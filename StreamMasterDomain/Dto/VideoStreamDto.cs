﻿using StreamMasterDomain.Attributes;
using StreamMasterDomain.Mappings;

using System.ComponentModel.DataAnnotations;

namespace StreamMasterDomain.Dto;

[RequireAll]
public class ChildVideoStreamDto : BaseVideoStreamDto, IMapFrom<VideoStream>, IMapFrom<VideoStreamDto>
{
    [Required]
    public int MaxStreams { get; set; }

    [Required]
    public int Rank { get; set; }
}

[RequireAll]
public class VideoStreamDto : BaseVideoStreamDto, IMapFrom<VideoStream>, IMapFrom<ChildVideoStreamDto>
{
    public List<ChildVideoStreamDto> ChildVideoStreams { get; set; }
}

[RequireAll]
public class BaseVideoStreamDto : IMapFrom<VideoStream>
{
    /// <summary>
    /// Gets or sets the collection of video streams associated with the IPTV channel.
    /// </summary>
    /// <value>The collection of video streams associated with the IPTV channel.</value>

    /// <summary>
    /// Gets or sets the CUID (Content Unique Identifier) of the M3U stream.
    /// </summary>
    /// <value>The CUID of the M3U stream.</value>
    public string CUID { get; set; } = string.Empty;

    [Required]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the video stream is currently active.
    /// </summary>
    /// <value>True if the video stream is currently active; otherwise, false.</value>
    public bool IsActive { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether the video stream has been marked
    /// as deleted.
    /// </summary>
    /// <value>
    /// True if the video stream has been marked as deleted; otherwise, false.
    /// </value>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether the video stream should be hidden.
    /// </summary>
    /// <value>True if the video stream should be hidden; otherwise, false.</value>
    public bool IsHidden { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether the video stream is read-only.
    /// </summary>
    /// <value>True if the video stream is read-only; otherwise, false.</value>
    public bool IsReadOnly { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether the video stream was created by
    /// a user.
    /// </summary>
    /// <value>True if the video stream was created by a user; otherwise, false.</value>
    public bool IsUserCreated { get; set; } = false;

    /// <summary>
    /// Gets or sets the ID of the M3U file that the stream is associated with.
    /// </summary>
    /// <value>The ID of the M3U file.</value>
    public int M3UFileId { get; set; }

    /// <summary>
    /// Gets or sets the number of errors encountered during streaming.
    /// </summary>
    /// <value>The number of errors encountered during streaming.</value>
    public int StreamErrorCount { get; set; } = 0;

    /// <summary>
    /// Gets or sets the time of the last failed streaming attempt.
    /// </summary>
    /// <value>The time of the last failed streaming attempt.</value>
    public DateTime StreamLastFail { get; set; } = DateTime.MinValue;

    /// <summary>
    /// Gets or sets the time of the last successful streaming attempt.
    /// </summary>
    /// <value>The time of the last successful streaming attempt.</value>
    public DateTime StreamLastStream { get; set; } = DateTime.MinValue;

    /// <summary>
    /// Gets or sets the type of streaming proxy to use.
    /// </summary>
    /// <value>The type of streaming proxy to use.</value>
    public StreamingProxyTypes StreamProxyType { get; set; } = StreamingProxyTypes.SystemDefault;

    /// <summary>
    /// Gets or sets the channel number for the video stream.
    /// </summary>
    /// <value>The channel number for the video stream.</value>
    public int Tvg_chno { get; set; } = 0;

    /// <summary>
    /// Gets or sets the EPG group for the video stream.
    /// </summary>
    /// <value>The EPG group for the video stream.</value>
    public string Tvg_group { get; set; } = "All";

    /// <summary>
    /// Gets or sets the EPG ID for the video stream.
    /// </summary>
    /// <value>The EPG ID for the video stream.</value>
    public string Tvg_ID { get; set; } = "Dummy";

    /// <summary>
    /// Gets or sets the URL for the channel logo for the video stream.
    /// </summary>
    public string Tvg_logo { get; set; } = Constants.IconDefault;

    /// <summary>
    /// Gets or sets the callsign for the video stream.
    /// </summary>
    /// <value>The callsign for the video stream.</value>
    public string Tvg_name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL for the video stream.
    /// </summary>
    /// <value>
    /// A nullable string representing the URL for the video stream. If a URL is
    /// assigned, the value is a string. If no URL is assigned, the value is null.
    /// </value>
    public string Url { get; set; } = string.Empty;

    public int User_Tvg_chno { get; set; } = 0;
    public string User_Tvg_group { get; set; } = "All";
    public string User_Tvg_ID { get; set; } = "Dummy";
    public string User_Tvg_ID_DisplayName { get; set; } = "Dummy";
    public string User_Tvg_logo { get; set; } = Constants.IconDefault;

    public string User_Tvg_name { get; set; } = string.Empty;
    public string User_Url { get; set; } = string.Empty;
    public VideoStreamHandlers VideoStreamHandler { get; set; } = VideoStreamHandlers.SystemDefault;
}
