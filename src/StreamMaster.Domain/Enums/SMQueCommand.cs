﻿namespace StreamMaster.Domain.Enums;

public enum SMQueCommand
{
    CacheChannelLogos,
    CacheStreamLogos,
    CacheEPGChannelLogos,

    EPGRemovedExpiredKeys,
    ScanForTvLogos,

    ProcessEPGFile,
    ProcessM3UFile,
    ProcessM3UFiles,

    ScanDirectoryForEPGFiles,
    ScanDirectoryForM3UFiles,
    ScanForCustomPlayLists,

    EPGSync,
    SetIsSystemReady,
    SetTestTask,

    UpdateEntitiesFromIPTVChannels,
    CreateSTRMFiles
}