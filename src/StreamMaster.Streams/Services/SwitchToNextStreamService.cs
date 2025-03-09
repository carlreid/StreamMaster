using Microsoft.Extensions.DependencyInjection;

using StreamMaster.Domain.Enums;
using StreamMaster.PlayList.Models;

namespace StreamMaster.Streams.Services;

/// <summary>
/// Service to handle switching to the next stream for a given channel status.
/// </summary>
public sealed class SwitchToNextStreamService(
    ILogger<SwitchToNextStreamService> logger,
    ICacheManager cacheManager,
    IStreamLimitsService streamLimitsService,
    IProfileService profileService,
    IServiceProvider serviceProvider,
    IIntroPlayListBuilder introPlayListBuilder,
    ICustomPlayListBuilder customPlayListBuilder,
    IOptionsMonitor<Setting> settingsMonitor) : ISwitchToNextStreamService
{
    /// <inheritdoc/>
    public async Task<bool> SetNextStreamAsync(IStreamStatus channelStatus, string? overrideSMStreamId = null)
    {
        channelStatus.FailoverInProgress = true;
        Setting settings = settingsMonitor.CurrentValue;

        if (HandleIntroLogic(channelStatus, settings))
        {
            return true;
        }

        using IServiceScope scope = serviceProvider.CreateScope();
        channelStatus.PlayedIntro = false;

        SMChannelStreamLink? OldsmStream = channelStatus.SMChannel.CurrentRank > -1 ? channelStatus.SMChannel.SMStreams.FirstOrDefault(channelStreamLink => channelStreamLink.Rank == channelStatus.SMChannel.CurrentRank) : null;

        SMStreamDto? smStream = await ResolveSMStreamAsync(scope, channelStatus, overrideSMStreamId).ConfigureAwait(false);
        if (smStream == null)
        {
            HandleStreamNotFound(channelStatus);
            return false;
        }

        int? smStreamM3UfileId = OldsmStream?.SMStreamM3UFileId;
        int m3UfileId = smStream.M3UFileId;

        if (!(smStreamM3UfileId.GetValueOrDefault() == m3UfileId & smStreamM3UfileId.HasValue) && streamLimitsService.IsLimited(smStream))
        {
            return HandleStreamLimits(channelStatus, settings);
        }

        return streamLimitsService.IsLimited(smStream)
            ? HandleStreamLimits(channelStatus, settings)
            : smStream.SMStreamType switch
            {
                SMStreamTypeEnum.Movie => HandleCustomPlayListStream(channelStatus, smStream, settings),
                SMStreamTypeEnum.Intro => HandleIntroStream(channelStatus, smStream, settings),
                _ => await HandleStandardStreamAsync(scope, channelStatus, smStream, settings).ConfigureAwait(false)
            };
    }

    private static string GetClientUserAgent(SMChannelDto smChannel, SMStreamDto? smStream, Setting settings)
    {
        return !string.IsNullOrEmpty(smStream?.ClientUserAgent) ? smStream.ClientUserAgent
            : !string.IsNullOrEmpty(smChannel.ClientUserAgent) ? smChannel.ClientUserAgent
            : settings.ClientUserAgent;
    }

    private bool HandleIntroLogic(IStreamStatus channelStatus, Setting settings)
    {
        if (settings.ShowIntros != "None" &&
            ((settings.ShowIntros == "Once" && channelStatus.IsFirst) ||
             (settings.ShowIntros == "Always" && !channelStatus.PlayedIntro)))
        {
            CustomStreamNfo? intro = introPlayListBuilder.GetRandomIntro(channelStatus.IsFirst ? null : channelStatus.IntroIndex);
            if (intro != null)
            {
                SMStreamInfo introStreamInfo = CreateIntroStreamInfo(intro, settings);
                channelStatus.SetSMStreamInfo(introStreamInfo);

                channelStatus.IsFirst = false;
                channelStatus.PlayedIntro = true;

                logger.LogDebug("Set Next for Channel {SourceName}, switched to Intro {Id} {Name}",
                    channelStatus.SourceName, introStreamInfo.Id, introStreamInfo.Name);

                return true;
            }
        }

        return false;
    }

    private bool HandleIntroStream(IStreamStatus channelStatus, SMStreamDto smStream, Setting settings)
    {
        CustomPlayList? introPlayList = introPlayListBuilder.GetIntroPlayList(smStream.Name);
        if (introPlayList == null)
        {
            return false;
        }

        CommandProfileDto customPlayListProfile = profileService.GetCommandProfile("SMFFMPEGLocal");

        SMStreamInfo streamInfo = new()
        {
            Id = introPlayList.Name,
            Name = introPlayList.Name,
            Url = introPlayList.CustomStreamNfos[0].VideoFileName,
            SMStreamType = SMStreamTypeEnum.Movie,
            ClientUserAgent = GetClientUserAgent(channelStatus.SMChannel, smStream, settings),
            CommandProfile = customPlayListProfile
        };

        channelStatus.SetSMStreamInfo(streamInfo);

        logger.LogDebug("Set Next for Channel {SourceName}, switched to Intro {Id} {Name}",
            channelStatus.SourceName, streamInfo.Id, streamInfo.Name);

        return true;
    }

    private SMStreamInfo CreateIntroStreamInfo(CustomStreamNfo intro, Setting settings)
    {
        CommandProfileDto introCommandProfile = profileService.GetCommandProfile("SMFFMPEGLocal");
        return new SMStreamInfo
        {
            Id = $"{IntroPlayListBuilder.IntroIDPrefix}{intro.Movie.Title}",
            Name = intro.Movie.Title,
            Url = intro.VideoFileName,
            ClientUserAgent = settings.ClientUserAgent,
            CommandProfile = introCommandProfile,
            SMStreamType = SMStreamTypeEnum.Intro
        };
    }

    private async Task<SMStreamDto?> ResolveSMStreamAsync(IServiceScope scope, IStreamStatus channelStatus, string? overrideSMStreamId)
    {
        // If a Stream ID is provided, return with that as the stream
        if (!string.IsNullOrEmpty(overrideSMStreamId))
        {
            IRepositoryWrapper repository = scope.ServiceProvider.GetRequiredService<IRepositoryWrapper>();
            return await repository.SMStream.GetSMStreamAsync(overrideSMStreamId).ConfigureAwait(false);
        }

        // Otherwise, locate the next stream in the rotation
        List<SMStreamDto> smStreams = [.. channelStatus.SMChannel.SMStreamDtos.OrderBy(stream => stream.Rank)];
        if (smStreams.Count == 0)
        {
            return null;
        }

        // Get the current stream if one exists
        SMStreamDto? currentStream = null;
        if (channelStatus.SMChannel.CurrentRank >= 0 && channelStatus.SMChannel.CurrentRank < smStreams.Count)
        {
            currentStream = smStreams[channelStatus.SMChannel.CurrentRank];
        }

        // Try each stream in rotation order, starting from the next one after current
        int startingRank = (channelStatus.SMChannel.CurrentRank + 1) % smStreams.Count;
        int currentRankToCheck = startingRank;

        do
        {
            SMStreamDto candidateStream = smStreams[currentRankToCheck];

            // Check if the candidate stream is valid.
            bool sameM3UFile = currentStream?.M3UFileId == candidateStream.M3UFileId;
            bool notLimited = !streamLimitsService.IsLimited(candidateStream);

            if (sameM3UFile || notLimited)
            {
                // Update the CurrentRank before returning the stream
                channelStatus.SMChannel.CurrentRank = currentRankToCheck;
                return candidateStream;
            }

            // Move to the next stream in rotation
            currentRankToCheck = (currentRankToCheck + 1) % smStreams.Count;
        } while (currentRankToCheck != startingRank); // Stop when we've checked all streams

        // No valid stream found
        return null;
    }

    private void HandleStreamNotFound(IStreamStatus channelStatus)
    {
        logger.LogError("Set Next for Channel {SourceName}, no streams available.", channelStatus.SourceName);
        channelStatus.SetSMStreamInfo(null);
    }

    private bool HandleStreamLimits(IStreamStatus channelStatus, Setting settings)
    {
        if (settings.ShowMessageVideos && cacheManager.MessageNoStreamsLeft != null)
        {
            channelStatus.SetSMStreamInfo(cacheManager.MessageNoStreamsLeft);

            logger.LogDebug("No streams found for {SourceName}, sending message: {Id} {Name}",
                channelStatus.SourceName, cacheManager.MessageNoStreamsLeft.Id, cacheManager.MessageNoStreamsLeft.Name);

            return true;
        }

        logger.LogInformation("Set Next for Channel {SourceName}, no streams found within limits.", channelStatus.SourceName);
        return false;
    }

    private bool HandleCustomPlayListStream(IStreamStatus channelStatus, SMStreamDto smStream, Setting settings)
    {
        CustomPlayList? customPlayList = customPlayListBuilder.GetCustomPlayList(smStream.Name);
        if (customPlayList == null)
        {
            return false;
        }

        (CustomStreamNfo StreamNfo, int SecondsIn) streamNfo = customPlayListBuilder.GetCurrentVideoAndElapsedSeconds(customPlayList.Name);
        SMStreamInfo customStreamInfo = CreateStreamInfoFromCustomPlayList(channelStatus, smStream, settings, streamNfo);

        channelStatus.SetSMStreamInfo(customStreamInfo);

        logger.LogDebug("Set Next for Channel {SourceName}, switched to Custom Playlist {Id} {Name}",
            channelStatus.SourceName, customStreamInfo.Id, customStreamInfo.Name);

        return true;
    }

    private SMStreamInfo CreateStreamInfoFromCustomPlayList(IStreamStatus channelStatus, SMStreamDto smStream, Setting settings, (CustomStreamNfo StreamNfo, int SecondsIn) streamNfo)
    {
        CommandProfileDto customPlayListProfile = profileService.GetCommandProfile("SMFFMPEGLocal");

        return new SMStreamInfo
        {
            Id = streamNfo.StreamNfo.Movie.Title,
            Name = streamNfo.StreamNfo.Movie.Title,
            Url = streamNfo.StreamNfo.VideoFileName,
            SMStreamType = SMStreamTypeEnum.Movie,
            SecondsIn = streamNfo.SecondsIn,
            ClientUserAgent = GetClientUserAgent(channelStatus.SMChannel, smStream, settings),
            CommandProfile = customPlayListProfile
        };
    }

    private async Task<bool> HandleStandardStreamAsync(IServiceScope scope, IStreamStatus channelStatus, SMStreamDto smStream, Setting settings)
    {
        IStreamGroupService streamGroupService = scope.ServiceProvider.GetRequiredService<IStreamGroupService>();
        CommandProfileDto commandProfile = await streamGroupService.GetProfileFromSGIdsCommandProfileNameAsync(
            null, channelStatus.StreamGroupProfileId, smStream.CommandProfileName ?? channelStatus.SMChannel.CommandProfileName);

        SMStreamInfo standardStreamInfo = new()
        {
            Id = smStream.Id,
            Name = smStream.Name,
            Url = smStream.Url,
            SMStreamType = smStream.SMStreamType,
            ClientUserAgent = GetClientUserAgent(channelStatus.SMChannel, smStream, settings),
            CommandProfile = commandProfile
        };

        channelStatus.SetSMStreamInfo(standardStreamInfo);

        logger.LogDebug("Set Next for Channel {SourceName}, switched to Standard {Id} {Name}",
            channelStatus.SourceName, standardStreamInfo.Id, standardStreamInfo.Name);

        return true;
    }
}