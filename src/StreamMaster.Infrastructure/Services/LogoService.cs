using System.Collections.Concurrent;
using System.Diagnostics;
using System.Web;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using StreamMaster.Application.Common;
using StreamMaster.Application.Common.Extensions;
using StreamMaster.Domain.API;
using StreamMaster.Domain.Configuration;
using StreamMaster.Domain.Crypto;
using StreamMaster.Domain.Dto;
using StreamMaster.Domain.Enums;
using StreamMaster.Domain.Extensions;
using StreamMaster.Domain.Helpers;
using StreamMaster.Domain.Repository;
using StreamMaster.Domain.XmltvXml;
using StreamMaster.SchedulesDirect.Domain.Interfaces;

namespace StreamMaster.Infrastructure.Services;

public class LogoService(IHttpContextAccessor httpContextAccessor,
                         IOptionsMonitor<Setting> settings,
                         IOptionsMonitor<CustomLogoDict> customLogos,
                         IImageDownloadService imageDownloadService,
                         IContentTypeProvider mimeTypeProvider,
                         IMemoryCache memoryCache,
                         IImageDownloadQueue imageDownloadQueue,
                         IServiceProvider serviceProvider,
                         IFileUtilService fileUtilService,
                         IDataRefreshService dataRefreshService,
                         ILogger<LogoService> logger)
    : ILogoService
{
    private ConcurrentDictionary<string, CustomLogoDto> Logos { get; } = [];
    private static readonly SemaphoreSlim scantvLogoSemaphore = new(1, 1);

    #region Custom Logo

    public string AddCustomLogo(string Name, string Source)
    {
        Source = ImageConverter.ConvertDataToPNG(Name, Source);

        customLogos.CurrentValue.AddCustomLogo(Source.ToUrlSafeBase64String(), Name);

        AddLogoToCache(Source, Source, Name, SMFileTypes.CustomLogo, false);

        SettingsHelper.UpdateSetting(customLogos.CurrentValue);

        return Source;
    }

    public void RemoveCustomLogo(string Source)
    {
        if (!ImageConverter.IsCustomSource(Source))
        {
            customLogos.CurrentValue.RemoveProfile(Source);
            SettingsHelper.UpdateSetting(customLogos.CurrentValue);
        }

        string toTest = Source.ToUrlSafeBase64String();
        CustomLogo? test = customLogos.CurrentValue.GetCustomLogo(toTest);
        if (test?.IsReadOnly != false)
        {
            return;
        }
        customLogos.CurrentValue.RemoveProfile(toTest);

        SettingsHelper.UpdateSetting(customLogos.CurrentValue);

        Source = Source.Remove(0, 14);

        ImagePath? imagePath = GetValidImagePath(Source, SMFileTypes.CustomLogo);
        if (imagePath is null)
        {
            return;
        }
        if (File.Exists(imagePath.FullPath))
        {
            File.Delete(imagePath.FullPath);
        }
    }

    #endregion Custom Logo

    public string GetLogoUrl(SMChannel smChannel, string baseUrl)
    {
        return settings.CurrentValue.LogoCache || !smChannel.Logo.StartsWithIgnoreCase("http")
            ? $"{baseUrl}{BuildInfo.PATH_BASE}/api/files/sm/{smChannel.Id}"
            : smChannel.Logo;
    }

    public string GetLogoUrl(SMChannel smChannel)
    {
        string baseUrl = httpContextAccessor.GetUrl();

        return GetLogoUrl(smChannel, baseUrl);
    }

    public string GetLogoUrl(XmltvChannel xmltvChannel)
    {
        string baseUrl = httpContextAccessor.GetUrl();
        return xmltvChannel.Icons is null || xmltvChannel.Icons.Count == 0
            ? "/" + settings.CurrentValue.DefaultLogo
            : GetLogoUrl(xmltvChannel.Id, xmltvChannel.Icons[0].Src, baseUrl);
    }

    private string GetLogoUrl(string Id, string Logo, string baseUrl)
    {
        return settings.CurrentValue.LogoCache || !Logo.StartsWithIgnoreCase("http")
            ? $"{baseUrl}/api/files/sm/{Id}"
            : Logo;
    }

    public async Task<DataResponse<bool>> CacheSMChannelLogosAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        ISMChannelService channelService = scope.ServiceProvider.GetRequiredService<ISMChannelService>();

        IQueryable<SMChannel> channelsQuery = channelService.GetSMStreamLogos(true);

        if (!await channelsQuery.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return DataResponse.False;
        }

        try
        {
            int degreeOfParallelism = Environment.ProcessorCount;
            await channelsQuery.AsAsyncEnumerable().ForEachAsync(degreeOfParallelism, channel =>
            {
                if (string.IsNullOrEmpty(channel.Logo) || !channel.Logo.IsValidUrl())
                    return Task.CompletedTask;

                AddLogoToCache(channel.Logo, channel.Logo, channel.Name, SMFileTypes.Logo, true);
                imageDownloadQueue.EnqueueLogo(new LogoInfo(channel.Name, channel.Logo));
                return Task.CompletedTask;
            }, cancellationToken).ConfigureAwait(false);

            await dataRefreshService.RefreshLogos().ConfigureAwait(false);
        }
        catch (Exception ex) when (!(ex is OperationCanceledException))
        {
            logger.LogError(ex, "An error occurred while building logos cache from SM streams.");
            return DataResponse.False;
        }

        return DataResponse.True;
    }

    public async Task<DataResponse<bool>> CacheSMStreamLogosAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        ISMStreamService smStreamService = scope.ServiceProvider.GetRequiredService<ISMStreamService>();

        IQueryable<SMStream> streamsQuery = smStreamService.GetSMStreamLogos(true);

        if (!await streamsQuery.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return DataResponse.False;
        }

        try
        {
            int degreeOfParallelism = Environment.ProcessorCount;

            await streamsQuery.AsAsyncEnumerable()
                .ForEachAsync(degreeOfParallelism, stream =>
                {
                    // Ensure thread safety in AddLogoToCache
                    AddLogoToCache(stream.Logo, stream.Logo, stream.Name, SMFileTypes.Logo, true);
                    return Task.CompletedTask;
                }, cancellationToken)
                .ConfigureAwait(false);

            await dataRefreshService.RefreshLogos().ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "An error occurred while building logos cache from SM streams.");
            return DataResponse.False;
        }

        return DataResponse.True;
    }

    public async Task<(FileStream? fileStream, string? FileName, string? ContentType)> GetLogoAsync(string fileName, CancellationToken cancellationToken)
    {
        if (fileName.IsRedirect())
        {
            return (null, fileName, null);
        }

        string? imagePath = fileName.GetLogoImageFullPath();

        if (imagePath == null || !File.Exists(imagePath))
        {
            return (null, null, null);
        }

        try
        {
            (FileStream? fileStream, string? FileName, string? ContentType) result = await GetLogoStreamAsync(imagePath, fileName, cancellationToken);
            return result;
        }
        catch
        {
            return (null, null, null);
        }
    }

    public async Task<(FileStream? fileStream, string? FileName, string? ContentType)> GetProgramLogoAsync(string fileName, CancellationToken cancellationToken)
    {
        try
        {
            if (fileName.IsRedirect())
            {
                return (null, fileName, null);
            }

            string? imagePath = fileName.GetProgramLogoFullPath();

            return imagePath == null || !File.Exists(imagePath)
                ? ((FileStream? fileStream, string? FileName, string? ContentType))(null, null, null)
                : await ThingAsync(fileName, imagePath, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            return (null, null, null);
        }
    }

    public async Task<(FileStream? fileStream, string? FileName, string? ContentType)> GetLogoForChannelAsync(int SMChannelId, CancellationToken cancellationToken)
    {
        IServiceScope scope = serviceProvider.CreateScope();
        IRepositoryWrapper repositoryWrapper = scope.ServiceProvider.GetRequiredService<IRepositoryWrapper>();

        SMChannel? channel = repositoryWrapper.SMChannel.GetSMChannel(SMChannelId);
        if (channel == null || string.IsNullOrEmpty(channel.Logo))
        {
            return (null, null, null);
        }

        if (channel.Logo.IsRedirect())
        {
            return (null, channel.Logo, null);
        }

        string fileName = channel.Logo.Remove(0, 14);
        if (channel.Logo.StartsWithIgnoreCase("/api/files/cu/"))
        {
            return await GetCustomLogoAsync(fileName, cancellationToken);
        }

        if (channel.Logo.StartsWithIgnoreCase("/api/files/lc/"))
        {
            return await GetLogoAsync(fileName, cancellationToken);
        }

        if (channel.Logo.StartsWithIgnoreCase("/api/files/tv/"))
        {
            return await GetTVLogoAsync(fileName, cancellationToken);
        }

        string cleanedPath = LogoInfo.Cleanup(channel.Logo);
        fileName = cleanedPath.StartsWithIgnoreCase("http") ? cleanedPath.GenerateFNV1aHash() : cleanedPath;

        (FileStream? fileStream, string? FileName, string? ContentType) ret = await GetLogoAsync(fileName, cancellationToken);

        return ret;
    }

    public async Task<(FileStream? fileStream, string? FileName, string? ContentType)> GetCustomLogoAsync(string Source, CancellationToken cancellationToken)
    {
        string toTest = $"/api/files/cu/{Source}".ToUrlSafeBase64String();

        CustomLogo? test = customLogos.CurrentValue.GetCustomLogo(toTest);
        if (test is null)
        {
            return (null, null, null);
        }

        ImagePath? imagePath = GetValidImagePath(Source, SMFileTypes.CustomLogo);

        if (imagePath == null || !File.Exists(imagePath.FullPath))
        {
            return (null, null, null);
        }

        try
        {
            (FileStream? fileStream, string? FileName, string? ContentType) result = await GetLogoStreamAsync(imagePath.FullPath, Source, cancellationToken);
            return result;
        }
        catch
        {
            return (null, null, null);
        }
    }

    private static async Task<FileStream?> GetFileStreamAsync(string imagePath)
    {
        try
        {
            FileStream fileStream = new(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
            await Task.CompletedTask.ConfigureAwait(false);

            return fileStream;
        }
        catch
        {
            return null;
        }
    }

    private async Task<(FileStream? fileStream, string? FileName, string? ContentType)> GetLogoStreamAsync(string imagePath, string fileName, CancellationToken cancellationToken)
    {
        try
        {
            return fileName.IsRedirect() ? ((FileStream? fileStream, string? FileName, string? ContentType))(null, null, null) : await ThingAsync(fileName, imagePath, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            return (null, null, null);
        }
    }

    private async Task<(FileStream? fileStream, string? FileName, string? ContentType)> ThingAsync(string fileName, string imagePath, CancellationToken cancellationToken)
    {
        try
        {
            if (fileName.IsRedirect())
            {
                return (null, null, null);
            }

            imagePath = imagePath.GetPNGPath();
            fileName = fileName.GetPNGPath();

            if (imagePath == null || !File.Exists(imagePath))
            {
                return (null, null, null);
            }

            FileStream? fileStream = null;

            if (File.Exists(imagePath))
            {
                fileStream = await GetFileStreamAsync(imagePath).ConfigureAwait(false);
            }

            if (fileStream == null)
            {
                LogoInfo logoInfo = new(fileName)
                {
                    IsSchedulesDirect = true
                };
                if (!string.IsNullOrEmpty(logoInfo.FullPath))
                {
                    if (await imageDownloadService.DownloadImageAsync(logoInfo, cancellationToken).ConfigureAwait(false))
                    {
                        fileStream = await GetFileStreamAsync(imagePath).ConfigureAwait(false);
                    }
                }
                if (fileStream == null)
                {
                    return (null, null, null);
                }
            }

            string contentType = GetContentType(fileName);

            // Ensure the file is ready to be read asynchronously
            await Task.CompletedTask.ConfigureAwait(false);

            return (fileStream, fileName, contentType);
        }
        catch
        {
            return (null, null, null);
        }
    }

    public static readonly MemoryCacheEntryOptions NeverRemoveCacheEntryOptions = new MemoryCacheEntryOptions().SetPriority(CacheItemPriority.NeverRemove);

    internal string GetContentType(string fileName)
    {
        string cacheKey = $"ContentType-{fileName}";

        if (!memoryCache.TryGetValue(cacheKey, out string? contentType))
        {
            if (!mimeTypeProvider.TryGetContentType(fileName, out contentType))
            {
                contentType = "application/octet-stream";
            }
            contentType ??= "application/octet-stream";

            _ = memoryCache.Set(cacheKey, contentType, NeverRemoveCacheEntryOptions);
        }

        return contentType ?? "application/octet-stream";
    }

    private static string? GetCachedFile(string source, SMFileTypes smFileType)
    {
        string? fullPath = source.GetImageFullPath(smFileType);

        return string.IsNullOrEmpty(fullPath) ? null : !File.Exists(fullPath) ? null : fullPath;
    }

    public void AddLogoToCache(string source, string title, SMFileTypes sMFileType)
    {
        AddLogoToCache(source, source, title, sMFileType, false);
    }

    public void AddLogoToCache(string source, string value, string title, SMFileTypes sMFileType, bool HashSource = false)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(value) || string.IsNullOrEmpty(title))
            return;

        string key = value;
        string logoPath = sMFileType switch
        {
            SMFileTypes.TvLogo => $"/api/files/tv/{value}",
            SMFileTypes.CustomLogo => $"/api/files/cu/{value}",
            SMFileTypes.CustomPlayListLogo => $"/api/files/lc/{value}",
            SMFileTypes.ProgramLogo => $"/api/files/pr/{value}",
            _ => $"/api/files/{value}"
        };

        value = logoPath;
        if (!source.StartsWith("http"))
            source = value;

        CustomLogoDto logoDto = new()
        {
            Source = source,
            Value = value,
            Name = title,
            FileId = (int)sMFileType,
            IsReadOnly = true
        };

        Logos.TryAdd(key, logoDto);
    }

    public void ClearLogos()
    {
        Logos.Clear();
    }

    public CustomLogoDto? GetLogoBySource(string source)
    {
        return Logos.TryGetValue(source.GenerateFNV1aHash(), out CustomLogoDto? logo) ? logo : null;
    }

    public ImagePath? GetValidImagePath(string URL, SMFileTypes fileType, bool? checkExists = true)
    {
        string url = HttpUtility.UrlDecode(URL);

        switch (fileType)
        {
            case SMFileTypes.Logo:
                {
                    string logoReturnName = Path.GetFileName(url);
                    string? cachedFile = GetCachedFile(url, fileType);
                    return cachedFile != null
                        ? new ImagePath
                        {
                            ReturnName = logoReturnName,
                            FullPath = cachedFile,
                            SMFileType = SMFileTypes.Logo
                        }
                        : null;
                }

            case SMFileTypes.CustomLogo:
                {
                    string logoReturnName = Path.GetFileName(url);
                    string? cachedFile = GetCachedFile(url, fileType);
                    return cachedFile != null
                        ? new ImagePath
                        {
                            ReturnName = logoReturnName,
                            FullPath = cachedFile,
                            SMFileType = SMFileTypes.CustomLogo
                        }
                        : null;
                }

            case SMFileTypes.CustomPlayList:
            case SMFileTypes.CustomPlayListLogo:
                {
                    string fullPath = BuildInfo.CustomPlayListFolder + URL;
                    return File.Exists(fullPath)
                        ? new ImagePath
                        {
                            ReturnName = Path.GetFileName(fullPath),
                            FullPath = fullPath,
                            SMFileType = SMFileTypes.CustomPlayListLogo
                        }
                        : null;
                }

            case SMFileTypes.ProgramLogo:
                {
                    string logoReturnName = Path.GetFileName(url);
                    string? cachedFile = GetCachedFile(url, fileType);
                    return cachedFile != null
                        ? new ImagePath
                        {
                            ReturnName = logoReturnName,
                            FullPath = cachedFile,
                            SMFileType = SMFileTypes.Logo
                        }
                        : null;
                }

            case SMFileTypes.TvLogo:
                {
                    string fullPath = BuildInfo.TVLogoFolder + "/" + URL;
                    return File.Exists(fullPath)
                        ? new ImagePath
                        {
                            ReturnName = Path.GetFileName(fullPath),
                            FullPath = fullPath,
                            SMFileType = SMFileTypes.TvLogo
                        }
                        : null;
                }
        }

        // Handle logo cache lookup
        if (Logos.TryGetValue(url, out CustomLogoDto? cache))
        {
            string path = cache.Value;
            string tvLogosFileName = Path.Combine(BuildInfo.TVLogoFolder, path);
            return new ImagePath
            {
                ReturnName = path,
                FullPath = tvLogosFileName,
                SMFileType = SMFileTypes.TvLogo
            };
        }

        // Try to get logo by source
        Stopwatch stopwatch = Stopwatch.StartNew();
        CustomLogoDto? logoBySource = GetLogoBySource(url);
        if (logoBySource == null)
        {
            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds > 10)
            {
                logger.LogInformation("GetValidImagePath GetIcBySource took {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);
            }
            return null;
        }

        FileDefinition logo = FileDefinitions.Logo;
        string returnName = logoBySource.Name ?? "";
        string fileName = Path.Combine(logo.DirectoryLocation, returnName);

        return File.Exists(fileName)
            ? new ImagePath
            {
                ReturnName = returnName,
                SMFileType = SMFileTypes.Logo,
                FullPath = fileName
            }
            : null;
    }

    public List<CustomLogoDto> GetLogos()
    {
        IEnumerable<CustomLogoDto> masterLogos = customLogos.CurrentValue.GetCustomLogosDto().Concat(Logos.Values);
        IOrderedEnumerable<CustomLogoDto> ret = masterLogos.OrderBy(a => a.Name);
        return [.. ret];
    }

    public async Task<bool> ScanForTvLogosAsync(CancellationToken cancellationToken = default)
    {
        await scantvLogoSemaphore.WaitAsync(cancellationToken);
        try
        {
            FileDefinition fd = FileDefinitions.TVLogo;
            if (!Directory.Exists(fd.DirectoryLocation))
            {
                return false;
            }

            DirectoryInfo dirInfo = new(BuildInfo.TVLogoFolder);

            await UpdateTVLogosFromDirectoryAsync(dirInfo, dirInfo.FullName, cancellationToken).ConfigureAwait(false);

            return true;
        }
        finally
        {
            scantvLogoSemaphore.Release();
        }
    }

    public async Task<(FileStream? fileStream, string? FileName, string? ContentType)> GetTVLogoAsync(string Source, CancellationToken cancellationToken)
    {
        string ext = Path.GetExtension(Source);
        string name = Path.GetFileNameWithoutExtension(Source);
        string toTest = $"/api/files/tv/{Source}";
        CustomLogoDto? test = Logos.Values.FirstOrDefault(a => a.Source == toTest);
        if (test is null)
        {
            return (null, null, null);
        }

        string fileName = name.FromUrlSafeBase64String();

        ImagePath? imagePath = GetValidImagePath(fileName, SMFileTypes.TvLogo);

        if (imagePath == null || !File.Exists(imagePath.FullPath))
        {
            return (null, null, null);
        }

        try
        {
            (FileStream? fileStream, string? FileName, string? ContentType) result = await GetLogoStreamAsync(imagePath.FullPath, Source, cancellationToken);
            return result;
        }
        catch
        {
            return (null, null, null);
        }
    }

    public async Task UpdateTVLogosFromDirectoryAsync(DirectoryInfo dirInfo, string tvLogosLocation, CancellationToken cancellationToken = default)
    {
        foreach (FileInfo file in dirInfo.GetFiles("*png"))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            string basePath = dirInfo.FullName.Replace(tvLogosLocation, "");
            if (basePath.StartsWith(Path.DirectorySeparatorChar))
            {
                basePath = basePath.Remove(0, 1);
            }

            string? name = Path.GetFileNameWithoutExtension(file.Name);
            if (name is null)
            {
                continue;
            }
            string basename = basePath.Replace(Path.DirectorySeparatorChar, ' ');
            string source = $"{basename}-{name}";
            string title = basename + " " + name.Replace('-', ' ');
            string url = Path.Combine(basePath, file.Name).ToUrlSafeBase64String();
            url += Path.GetExtension(file.Name);

            AddLogoToCache(url, title, SMFileTypes.TvLogo);
        }

        foreach (DirectoryInfo newDir in dirInfo.GetDirectories())
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            await UpdateTVLogosFromDirectoryAsync(newDir, tvLogosLocation, cancellationToken).ConfigureAwait(false);
        }

        return;
    }

    public void RemoveLogosByM3UFileId(int id)
    {
        foreach (KeyValuePair<string, CustomLogoDto> logo in Logos.Where(a => a.Value.FileId == id))
        {
            _ = Logos.TryRemove(logo.Key, out _);
        }
    }

    public async Task<DataResponse<bool>> CacheEPGChannelLogosAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        IEPGService epgService = scope.ServiceProvider.GetRequiredService<IEPGService>();

        List<XmltvChannel> channels = new();
        foreach (EPGFile epgFile in await epgService.GetEPGFilesAsync())
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return DataResponse.True;
            }

            string epgPath = Path.Combine(FileDefinitions.EPG.DirectoryLocation, epgFile.Source);
            List<XmltvChannel> channelsFromXml = await fileUtilService.GetChannelsFromXmlAsync(epgPath, cancellationToken);
            channels.AddRange(channelsFromXml);
        }

        return DataResponse.True;
    }
}