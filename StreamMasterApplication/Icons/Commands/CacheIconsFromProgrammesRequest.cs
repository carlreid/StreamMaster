using AutoMapper;

using FluentValidation;

using MediatR;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using StreamMasterDomain.Dto;
using StreamMasterDomain.Extensions;
using System.Text;
using System.Text.Json;
using System.Web;

namespace StreamMasterApplication.Icons.Commands;

public class CacheIconsFromEPGsRequest : IRequest<bool>
{
}

public class CacheIconsFromEPGsRequestHandler : IRequestHandler<CacheIconsFromEPGsRequest, bool>
{
    private readonly IAppDbContext _context;

    private readonly ILogger<CacheIconsFromEPGsRequestHandler> _logger;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _memoryCache;
    private readonly ISender _sender;
    private readonly HttpClient _httpClient;

    public CacheIconsFromEPGsRequestHandler(
        ILogger<CacheIconsFromEPGsRequestHandler> logger,
        IMemoryCache memoryCache,
        IMapper mapper,
        IAppDbContext context,
        ISender sender,
        HttpClient httpClient)
    {
        _memoryCache = memoryCache;
        _logger = logger;
        _mapper = mapper;
        _context = context;
        _sender = sender;
        _httpClient = httpClient;
    }

    public async Task<bool> Handle(CacheIconsFromEPGsRequest command, CancellationToken cancellationToken)
    {
        SettingDto _setting = await _sender.Send(new GetSettings(), cancellationToken).ConfigureAwait(false);
        if (!_setting.CacheIcons)
        {
            return false;
        }

        await Task.WhenAll(_context.EPGFiles.Select(epgFile => ProcessEPGFile(epgFile, _setting, cancellationToken)));

        var icons = _memoryCache.Programmes()
            .Where(p => p.Icon is not null && p.Icon.Any(i => i.Src is not null))
            .SelectMany(p => p.Icon!.Where(i => i.Src is not null).Select(i => i.Src!))
            .Distinct();

        if (!icons.Any())
        {
            return false;
        }

        await ProcessIcons(FileDefinitions.ProgrammeIcon, icons, _setting, cancellationToken);

        return true;
    }

    private async Task ProcessEPGFile(EPGFile epg, SettingDto setting, CancellationToken cancellationToken)
    {
        var tv = await epg.GetTV().ConfigureAwait(false);
        if (tv == null)
        {
            return;
        }

        var icons = tv.Channel
            .Where(tvChannel => tvChannel is not null && tvChannel.Icon is not null && !string.IsNullOrEmpty(tvChannel.Icon.Src))
            .Select(a => a.Icon!.Src!)
            .ToList();

        if (!icons.Any())
        {
            return;
        }

        await ProcessIcons(FileDefinitions.Icon, icons, setting, cancellationToken).ConfigureAwait(false);
    }

    private async Task ProcessIcons(FileDefinition fd, IEnumerable<string> icons, SettingDto setting, CancellationToken cancellationToken)
    {
        const int BatchSize = 100;
        var isNew = false;
        string token = "";
        var batchCount = 0;
        var batches = icons
            .Where(icon => icon is not null)
            .Batch(BatchSize);

        foreach (var batch in batches)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var tasks = batch
                .Select(icon => ProcessIcon(icon!, token, setting, fd, cancellationToken));

            await Task.WhenAll(tasks).ConfigureAwait(false);

            isNew |= tasks.Any(t => t.Result);

            batchCount++;
            _logger.LogDebug($"CacheIconsFromEPGs Batch {batchCount} of {batches.Count()}");
        }
    }

    private async Task<bool> ProcessIcon(string icon, string token, SettingDto setting, FileDefinition fd, CancellationToken cancellationToken)
    {
        string decodedIconUrl = HttpUtility.UrlDecode(icon);
        string? queryString = null;

        if (decodedIconUrl.ToLower().StartsWith("https://json.schedulesdirect.org/20141201/image/"))
        {
            if (string.IsNullOrEmpty(token))
            {
                var request = new SDGetTokenRequest
                {
                    username = setting.SDUserName,
                    password = setting.SDPassword
                };

                using var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

                using var getTokenResponse = await _httpClient.PostAsync("https://json.schedulesdirect.org/20141201/token", content, cancellationToken).ConfigureAwait(false);
                getTokenResponse.EnsureSuccessStatusCode();

                var getTokenResponseContentStream = await getTokenResponse.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                var getTokenResponseContent = JsonSerializer.Deserialize<SDGetToken>(getTokenResponseContentStream);

                if (getTokenResponseContent == null || string.IsNullOrEmpty(getTokenResponseContent.token))
                {
                    return false;
                }

                token = getTokenResponseContent.token;
            }

            queryString = "?token=" + token;
        }

        string name = Path.GetFileNameWithoutExtension(decodedIconUrl);
        var (_, isNew) = await IconHelper.AddIcon(decodedIconUrl, queryString, name, _context, _mapper, setting, fd, cancellationToken).ConfigureAwait(false);

        return isNew;
    }

    private class SDGetToken
    {
        public int code { get; set; }
        public DateTime datetime { get; set; }
        public string? message { get; set; }
        public string? serverID { get; set; }
        public string? token { get; set; }
    }

    private class SDGetTokenRequest
    {
        public string? password { get; set; }
        public string? username { get; set; }
    }
}