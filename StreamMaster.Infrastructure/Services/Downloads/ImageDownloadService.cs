﻿using System.Net;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using StreamMaster.Domain.Common;
using StreamMaster.Domain.Configuration;
using StreamMaster.Domain.Dto;
using StreamMaster.Domain.Enums;
using StreamMaster.Domain.Extensions;
using StreamMaster.Domain.Helpers;
using StreamMaster.SchedulesDirect.Domain.Interfaces;
using StreamMaster.SchedulesDirect.Domain.JsonClasses;
using StreamMaster.SchedulesDirect.Domain.Models;
using StreamMaster.SchedulesDirect.Helpers;

namespace StreamMaster.Infrastructure.Services.Downloads
{
    public class ImageDownloadService : IHostedService, IImageDownloadService, IDisposable
    {
        private readonly ILogger<ImageDownloadService> logger;
        private readonly IDataRefreshService dataRefreshService;
        private readonly IOptionsMonitor<Setting> _settings;
        private readonly IOptionsMonitor<SDSettings> _sdSettings;
        private readonly ISchedulesDirectAPIService schedulesDirectAPI;
        private readonly IImageDownloadQueue imageDownloadQueue;
        private readonly ISchedulesDirectDataService schedulesDirectDataService;
        private readonly SemaphoreSlim downloadSemaphore;
        private readonly HttpClient httpClient; // Reused HttpClient via factory

        private const int BatchSize = 10;
        //private readonly object _lockObject = new();
        private static DateTime _lastRefreshTime = DateTime.MinValue;
        private static readonly Lock _refreshLock = new();
        private bool logged429 = false;

        public ImageDownloadServiceStatus ImageDownloadServiceStatus { get; } = new();

        public ImageDownloadService(
            ILogger<ImageDownloadService> logger,
            IHttpClientFactory httpClientFactory, // Use factory for HttpClient
            IDataRefreshService dataRefreshService,
            IOptionsMonitor<Setting> settings,
            IOptionsMonitor<SDSettings> sdSettings,
            ISchedulesDirectAPIService schedulesDirectAPI,
            IImageDownloadQueue imageDownloadQueue,
            ISchedulesDirectDataService schedulesDirectDataService)
        {
            this.logger = logger;
            this.dataRefreshService = dataRefreshService;
            _settings = settings;
            _sdSettings = sdSettings;
            this.schedulesDirectAPI = schedulesDirectAPI;
            this.imageDownloadQueue = imageDownloadQueue;
            this.schedulesDirectDataService = schedulesDirectDataService;
            downloadSemaphore = new SemaphoreSlim(_settings.CurrentValue.MaxConcurrentDownloads);
            httpClient = httpClientFactory.CreateClient(); // Use HttpClientFactory here
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Run(() => ExecuteAsync(cancellationToken), cancellationToken);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Stopping ImageDownloadService.");
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        private async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessQueuesAsync(stoppingToken).ConfigureAwait(false);
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred in ImageDownloadService.");
                }
            }
        }

        private async Task<bool> ProcessQueuesAsync(CancellationToken cancellationToken)
        {
            // Run both processing tasks concurrently
            bool[] results = await Task.WhenAll(
                ProcessProgramMetadataQueue(cancellationToken),
                ProcessNameLogoQueue(cancellationToken)
            ).ConfigureAwait(false);

            // If no items were processed, exit early
            bool isProcessed = results.Any(processed => processed);
            if (isProcessed)
            {
                await RefreshDownloadServiceAsync();
            }
            return isProcessed;
        }

        private async Task RefreshDownloadServiceAsync()
        {
            // Throttle the refresh logic to run at most once per second
            bool shouldRefresh = false;
            lock (_refreshLock)
            {
                if ((DateTime.UtcNow - _lastRefreshTime).TotalSeconds >= 1)
                {
                    _lastRefreshTime = DateTime.UtcNow;
                    shouldRefresh = true;
                }
            }

            // Check if both queues are empty
            bool areQueuesEmpty = imageDownloadQueue.IsProgramMetadataQueueEmpty() && imageDownloadQueue.IsNameLogoQueueEmpty();

            // Only refresh if items were processed and the throttle time has passed
            if (shouldRefresh || areQueuesEmpty || imageDownloadQueue.ProgramMetadataCount <= BatchSize || imageDownloadQueue.NameLogoCount <= BatchSize)
            {
                await dataRefreshService.RefreshDownloadServiceStatus();
            }
        }

        private async Task<bool> ProcessProgramMetadataQueue(CancellationToken cancellationToken)
        {
            if (!CanProceedWithDownload())
            {
                return false;
            }

            List<ProgramMetadata> metadataBatch = imageDownloadQueue.GetNextProgramMetadataBatch(BatchSize);
            ImageDownloadServiceStatus.TotalProgramMetadata = imageDownloadQueue.ProgramMetadataCount;

            if (metadataBatch.Count == 0)
            {
                return false;
            }

            logger.LogDebug("Processing batch of ProgramMetadata: {Count}", metadataBatch.Count);
            ImageDownloadServiceStatus.TotalProgramMetadataDownloadAttempts += metadataBatch.Count;

            List<string> successfullyDownloaded = [];

            foreach (ProgramMetadata metadata in metadataBatch)
            {
                if (!CanProceedWithDownload())
                {
                    break;
                }

                List<ProgramArtwork> artwork = GetArtwork(metadata);
                if (artwork.Count == 0)
                {
                    logger.LogDebug("No artwork to download for ProgramId: {ProgramId}", metadata.ProgramId);
                    ImageDownloadServiceStatus.TotalProgramMetadataNoArt++;
                    successfullyDownloaded.Add(metadata.ProgramId);
                    await RefreshDownloadServiceAsync();
                    continue;
                }

                bool success = await DownloadProgramMetadataArtworkAsync(artwork, metadata.ProgramId, cancellationToken);
                if (success)
                {
                    successfullyDownloaded.Add(metadata.ProgramId);
                }
                await RefreshDownloadServiceAsync();
            }

            imageDownloadQueue.TryDequeueProgramMetadataBatch(successfullyDownloaded);
            ImageDownloadServiceStatus.TotalProgramMetadata = imageDownloadQueue.ProgramMetadataCount;
            return successfullyDownloaded.Count != 0;
        }

        private List<ProgramArtwork> GetArtwork(ProgramMetadata metadata)
        {
            // Determine artwork size from settings, default to "Md"
            string artworkSize = string.IsNullOrEmpty(_sdSettings.CurrentValue.ArtworkSize) ? "Md" : _sdSettings.CurrentValue.ArtworkSize;
            List<ProgramArtwork> artwork = [];

            // Find the corresponding program using the ProgramId from metadata
            MxfProgram? program = schedulesDirectDataService.AllPrograms.Find(p => p.ProgramId == metadata.ProgramId);

            // If extras (artwork) exist in the program, fetch them
            if (program?.extras != null)
            {
                artwork = program.GetArtWork();
            }

            // If no artwork was found in the program, try fetching from metadata.Data
            if (artwork.Count == 0 && metadata.Data?.Count > 0)
            {
                // Use SDHelpers to get tiered images (series, sport, episode) from metadata
                artwork = SDHelpers.GetTieredImages(metadata.Data, ["series", "sport", "episode"], artworkSize);
            }

            return artwork;
        }

        private async Task<bool> DownloadProgramMetadataArtworkAsync(List<ProgramArtwork> artwork, string programId, CancellationToken cancellationToken)
        {
            List<string> successfullyDownloaded = [];

            foreach (ProgramArtwork art in artwork)
            {
                await downloadSemaphore.WaitAsync(cancellationToken);

                try
                {
                    string? logoPath = art.Uri.GetSDImageFullPath();
                    if (string.IsNullOrEmpty(logoPath))
                    {
                        ImageDownloadServiceStatus.TotalProgramMetadataNoArt++;
                        successfullyDownloaded.Add(programId);
                        continue;
                    }

                    if (File.Exists(logoPath))
                    {
                        ImageDownloadServiceStatus.TotalProgramMetadataAlreadyExists++;
                        successfullyDownloaded.Add(programId);
                        continue;
                    }

                    string url = art.Uri.StartsWith("http") ? art.Uri : $"image/{art.Uri}";

                    if (await DownloadImageAsync(url, logoPath, isSchedulesDirect: true, cancellationToken))
                    {
                        ImageDownloadServiceStatus.TotalProgramMetadataDownloaded++;
                        successfullyDownloaded.Add(programId);
                    }
                    else
                    {
                        ImageDownloadServiceStatus.TotalProgramMetadataErrors++;
                    }
                }
                finally
                {
                    downloadSemaphore.Release();
                }
            }

            if (successfullyDownloaded.Count > 0)
            {
                logger.LogDebug("All artwork for ProgramId: {ProgramId} downloaded", programId);
                imageDownloadQueue.TryDequeueProgramMetadataBatch(successfullyDownloaded);
            }

            return successfullyDownloaded.Count > 0;
        }

        private async Task<bool> ProcessNameLogoQueue(CancellationToken cancellationToken)
        {
            List<NameLogo> nameLogoBatch = imageDownloadQueue.GetNextNameLogoBatch(BatchSize);
            ImageDownloadServiceStatus.TotalNameLogo = imageDownloadQueue.NameLogoCount;

            if (nameLogoBatch.Count == 0)
            {
                return false;
            }

            logger.LogDebug("Processing batch of NameLogos: {Count}", nameLogoBatch.Count);
            ImageDownloadServiceStatus.TotalNameLogoDownloadAttempts += nameLogoBatch.Count;

            List<string> successfullyDownloaded = [];

            foreach (NameLogo nameLogo in nameLogoBatch)
            {
                if (!nameLogo.Logo.StartsWith("http"))
                {
                    successfullyDownloaded.Add(nameLogo.Name);
                    continue;
                }

                string? filePath = GetFilePath(nameLogo);
                if (filePath == null || File.Exists(filePath))
                {
                    ImageDownloadServiceStatus.TotalNameLogoAlreadyExists++;
                    successfullyDownloaded.Add(nameLogo.Name);
                    continue;
                }

                if (await DownloadImageAsync(nameLogo.Logo, filePath, isSchedulesDirect: false, cancellationToken))
                {
                    ImageDownloadServiceStatus.TotalNameLogoSuccessful++;
                    successfullyDownloaded.Add(nameLogo.Name);
                }
                else
                {
                    successfullyDownloaded.Add(nameLogo.Name);
                    ImageDownloadServiceStatus.TotalNameLogoErrors++;
                }
                await RefreshDownloadServiceAsync();
            }

            imageDownloadQueue.TryDequeueNameLogoBatch(successfullyDownloaded);
            ImageDownloadServiceStatus.TotalNameLogo = imageDownloadQueue.NameLogoCount;
            return successfullyDownloaded.Count != 0;
        }

        private static string? GetFilePath(NameLogo nameLogo)
        {
            // If the logo URL is empty or null, return null
            if (string.IsNullOrEmpty(nameLogo.Logo))
            {
                return null;
            }

            // Retrieve the file definition for the logo's file type
            FileDefinition? fd = FileDefinitions.GetFileDefinition(nameLogo.SMFileType);
            if (fd == null)
            {
                return null; // Return null if no file definition was found
            }

            // Get the file extension, default to the definition's extension if none is provided
            string ext = Path.GetExtension(nameLogo.Logo);
            if (string.IsNullOrEmpty(ext))
            {
                ext = fd.DefaultExtension;
            }

            // Generate a unique filename using an MD5 hash of the logo URL
            string fileName = FileUtil.EncodeToMD5(nameLogo.Logo) + ext;

            // Determine a subdirectory based on the first character of the filename for better organization
            string subDir = char.ToLowerInvariant(fileName[0]).ToString();

            // Build the full file path by combining the logo folder, directory location, subdirectory, and filename
            return Path.Combine(BuildInfo.LogoFolder, fd.DirectoryLocation, subDir, fileName);
        }

        private bool CanProceedWithDownload()
        {
            if (_sdSettings.CurrentValue.SDTooManyRequestsSuspend > DateTime.UtcNow)
            {
                if (!logged429)
                {
                    logger.LogWarning("Image downloads are temporarily suspended until {NoDownloadUntil}", _sdSettings.CurrentValue.SDTooManyRequestsSuspend);
                    logged429 = true;
                }
                return false;
            }
            logged429 = false;
            return true;
        }

        private async Task<bool> DownloadImageAsync(string url, string filePath, bool isSchedulesDirect, CancellationToken cancellationToken)
        {
            if (isSchedulesDirect && !CanProceedWithDownload())
            {
                return false;
            }

            try
            {
                HttpResponseMessage? response = isSchedulesDirect
                    ? await GetSdImage(url)
                    : await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (response != null)
                {
                    if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        _sdSettings.CurrentValue.SDTooManyRequestsSuspend = DateTime.UtcNow
                            .Date.AddDays(1)
                            .AddMinutes(10);

                        SettingsHelper.UpdateSetting(_sdSettings.CurrentValue);
                        logger.LogWarning("Max image download limit reached. No more downloads allowed until {NoDownloadUntil}", _sdSettings.CurrentValue.SDTooManyRequestsSuspend);
                        return false;
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                        await using FileStream fileStream = new(filePath, FileMode.Create);
                        await stream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
                        return true;
                    }
                    logger.LogDebug("Failed to download image from {Url} with status code {StatusCode}", url, response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to download image from {Url} {Message}", url, ex.InnerException?.Message);
            }
            return false;
        }

        private async Task<HttpResponseMessage?> GetSdImage(string uri)
        {
            return await schedulesDirectAPI.GetSdImage(uri);
        }

        public void Dispose()
        {
            downloadSemaphore.Dispose();
            httpClient.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}