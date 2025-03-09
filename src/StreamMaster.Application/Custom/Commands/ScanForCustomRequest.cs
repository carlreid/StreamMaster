using StreamMaster.Domain.Crypto;

namespace StreamMaster.Application.Custom.Commands;

[SMAPI]
[TsInterface(AutoI = false, IncludeNamespace = false, FlattenHierarchy = true, AutoExportMethods = false)]
public record ScanForCustomRequest : IRequest<APIResponse>;

public class ScanForCustomPlayListsRequestHandler(
    ILogoService logoService,
    IOptionsMonitor<CommandProfileDict> optionsOutputProfiles,
    IDataRefreshService dataRefreshService,
    IOptionsMonitor<Setting> _settings,
    ICacheManager cacheManager,
    IIntroPlayListBuilder introPlayListBuilder,
    ICustomPlayListBuilder CustomPlayListBuilder,
    IRepositoryWrapper Repository)
    : IRequestHandler<ScanForCustomRequest, APIResponse>
{
    public async Task<APIResponse> Handle(ScanForCustomRequest command, CancellationToken cancellationToken)
    {
        List<CustomPlayList> customPlayLists = CustomPlayListBuilder.GetCustomPlayLists();
        List<string> smStreamIdsToChannels = [];

        foreach (CustomPlayList customPlayList in customPlayLists)
        {
            string id = customPlayList.Name;

            SMStream? currentStream = await Repository.SMStream.FirstOrDefaultAsync(s => s.Id == id, tracking: true, cancellationToken: cancellationToken);
            if (currentStream != null)
            {
                if (currentStream.Logo != customPlayList.Logo)
                {
                    currentStream.Logo = customPlayList.Logo;
                }

                string logoStreamSource = currentStream.Logo.ToUrlSafeBase64String() + Path.GetExtension(currentStream.Logo);
                logoService.AddLogoToCache(logoStreamSource, currentStream.Name, SMFileTypes.CustomPlayListLogo);
            }
            else if (customPlayList.CustomStreamNfos != null && customPlayList.CustomStreamNfos.Count != 0)
            {
                string logo = customPlayList.Logo;
                string logoSource = logo.ToUrlSafeBase64String() + Path.GetExtension(logo);
                logoService.AddLogoToCache(logoSource, customPlayList.Name, SMFileTypes.CustomPlayListLogo);

                M3UFile? priv = await Repository.M3UFile.FirstOrDefaultAsync(a => a.Name == "-1PRIVATESYSTEM", cancellationToken: cancellationToken);

                foreach (CustomStreamNfo nfo in customPlayList.CustomStreamNfos)
                {
                    string streamId = id + "|" + nfo.Movie.Title;
                    SMStream? nfoStream = await Repository.SMStream.FirstOrDefaultAsync(s => s.Id == streamId, tracking: true, cancellationToken: cancellationToken);

                    if (nfoStream == null)
                    {
                        string logo2 = nfo.Movie.Thumb?.Text ?? nfo.Movie.Fanart?.Thumb?.Text ?? customPlayList.Logo;
                        string url = nfo.VideoFileName.ToUrlSafeBase64String();
                        string? ext = Path.GetExtension(nfo.VideoFileName);
                        if (ext != null)
                            url += ext;

                        SMStream newStream = new()
                        {
                            Id = streamId,
                            EPGID = EPGHelper.MovieId + "-" + nfo.Movie.Title,
                            Name = nfo.Movie.Title,
                            M3UFileName = Path.GetFileName(nfo.VideoFileName),
                            M3UFileId = EPGHelper.MovieId,
                            Group = "CustomPlayList",
                            SMStreamType = SMStreamTypeEnum.Movie,
                            Url = "/v/c/" + url,
                            Logo = logo,
                            IsSystem = true
                        };

                        Repository.SMStream.Create(newStream);
                        smStreamIdsToChannels.Add(newStream.Id);
                    }
                }
            }
        }

        await Repository.SaveAsync();
        APIResponse apiResponse = await Repository.SMChannel.CreateSMChannelsFromStreams(smStreamIdsToChannels, null);

        List<CustomPlayList> introPlayLists = introPlayListBuilder.GetIntroPlayLists();
        foreach (CustomPlayList customPlayList in introPlayLists)
        {
            foreach (CustomStreamNfo nfo in customPlayList.CustomStreamNfos)
            {
                string streamId = "|intro|" + nfo.Movie.Title;
                SMStream? nfoStream = await Repository.SMStream.FirstOrDefaultAsync(s => s.Id == streamId, tracking: true, cancellationToken: cancellationToken);

                if (nfoStream != null)
                {
                    if (nfoStream.Logo != customPlayList.Logo)
                    {
                        nfoStream.Logo = customPlayList.Logo;
                    }
                }
                else
                {
                    SMStream newStream = new()
                    {
                        Id = streamId,
                        EPGID = EPGHelper.IntroId.ToString() + "-" + nfo.Movie.Title,
                        Name = nfo.Movie.Title,
                        M3UFileName = Path.GetFileName(nfo.VideoFileName),
                        M3UFileId = EPGHelper.IntroId,
                        Group = "Intros",
                        SMStreamType = SMStreamTypeEnum.Intro,
                        Url = nfo.VideoFileName,
                        IsSystem = true
                    };
                    Repository.SMStream.Create(newStream);
                }
            }
        }

        await Repository.SaveAsync();

        if (File.Exists(BuildInfo.MessageNoStreamsLeft))
        {
            SMStream? stream = await Repository.SMStream.FirstOrDefaultAsync(a => a.Id == "MessageNoStreamsLeft", cancellationToken: cancellationToken);
            if (stream == null)
            {
                stream = new()
                {
                    Id = "MessageNoStreamsLeft",
                    EPGID = EPGHelper.MessageId.ToString() + "-MessageNoStreamsLeft",
                    Name = "No Streams Left",
                    M3UFileName = Path.GetFileName(BuildInfo.MessageNoStreamsLeft),
                    M3UFileId = EPGHelper.MessageId,
                    Group = "SystemMessages",
                    SMStreamType = SMStreamTypeEnum.Message,
                    Url = BuildInfo.MessageNoStreamsLeft,
                    IsSystem = true
                };
                Repository.SMStream.Create(stream);
                await Repository.SaveAsync();
            }

            if (stream != null)
            {
                CommandProfileDto introCommandProfileDto = optionsOutputProfiles.CurrentValue.GetProfileDto("SMFFMPEG");
                SMStreamInfo smStreamInfo = new()
                {
                    Id = stream.Id,
                    Name = stream.Name,
                    Url = stream.Url,
                    ClientUserAgent = _settings.CurrentValue.ClientUserAgent,
                    CommandProfile = introCommandProfileDto,
                    SMStreamType = SMStreamTypeEnum.Message
                };
                cacheManager.MessageNoStreamsLeft = smStreamInfo;
            }
        }

        await dataRefreshService.RefreshSMStreams();
        await dataRefreshService.RefreshAllSMChannels();

        return APIResponse.Success;
    }
}