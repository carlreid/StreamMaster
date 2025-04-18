﻿using Microsoft.Extensions.Logging;

namespace StreamMaster.Infrastructure.Services;

public class BroadcastService(

    //IFileLoggingServiceFactory factory,

    ILogger<BroadcastService> logger) : IBroadcastService, IDisposable
{
    //private readonly IFileLoggingService debugLogger = factory.Create("FileLoggerDebug");
    private Timer? _broadcastTimer;

    //private void printDebug(string format, params object[] args)
    //{
    //    string formattedMessage = string.Format(format, args);
    //    Debug.WriteLine(formattedMessage);
    //    //debugLogger.EnqueueLogEntry(formattedMessage);
    //}
    //public void LogDebug()
    //{
    //    //if (schedulesDirectDataService.SchedulesDirectDatas.Any())
    //    //{
    //    //    printDebug("SchedulesDirectDatas: {0}", schedulesDirectDataService.SchedulesDirectDatas.Count);
    //    //    foreach (KeyValuePair<int, ISchedulesDirectData> sd in schedulesDirectDataService.SchedulesDirectDatas)
    //    //    {
    //    //        printDebug("SchedulesDirectData: {0} {1} {2}", sd.Key, sd.Value.Services.Count, sd.Value.ProgramService.Count);
    //    //    }
    //    //}

    //    //if (statisticsManager.GetAllClientIds().Any())
    //    //{
    //    //    printDebug("Stat ClientIds: {0}", statisticsManager.GetAllClientIds().Count);
    //    //}
    //    //if (channelService.GetGlobalStreamsCount() != 0)
    //    //{
    //    //    printDebug("Global: {0}", channelService.GetGlobalStreamsCount());
    //    //}

    //    //if (channelService.GetChannelBroadcasters().Any())
    //    //{
    //    //    printDebug("GetChannelBroadcasters: {0}", channelService.GetChannelBroadcasters().Count);
    //    //}

    //    //logger.LogInformation("GetStreamHandlers: {GetStreamHandlers}", streamManager.GetStreamHandlers().Count);
    //    //foreach (IClientConfiguration clientStreamerConfiguration in clientStreamer.GetAllClientStreamerConfigurations)
    //    //{
    //    //    printDebug("Client: {0} {1}", clientStreamerConfiguration.SMChannel.ProfileName, clientStreamerConfiguration.ClientStream?.Id ?? Guid.Empty);
    //    //}

    //    //foreach (IStreamHandler handler in streamManager.GetStreamHandlers())
    //    //{
    //    //    printDebug("Stream: {0} {2} {3}", handler.ChannelCount, handler.StreamName, handler.StreamUrl);
    //    //}
    //}

    public void StartBroadcasting()
    {
        _broadcastTimer ??= new Timer(BroadcastMessage, null, 1000, 1000);
    }

    public void StopBroadcasting()
    {
        _broadcastTimer?.Dispose();
    }

    private void BroadcastMessage(object? state)
    {
        try
        {
            //var statisticsResults = streamStatisticService.GetInputStatistics();
            //if (statisticsResults.Any())
            //{
            //    dataRefreshService.RefreshStatistics();
            //    //_ = hub.ClientChannels.All.ClientStreamingStatisticsUpdate(statisticsResults).ConfigureAwait(false);

            //}
            //else
            //{
            //    //if (!sentEmpty)
            //    //{
            //    //_ = hub.ClientChannels.All.ClientStreamingStatisticsUpdate(statisticsResults).ConfigureAwait(false);
            //    //}
            //    //sentEmpty = true;
            //}
        }
        catch (Exception ex)
        {
            logger.LogError("Error while broadcasting message: {Message}", ex.Message);
        }
    }

    public void Dispose()
    {
        StopBroadcasting();
        _broadcastTimer?.Dispose();
        GC.SuppressFinalize(this);
    }
}