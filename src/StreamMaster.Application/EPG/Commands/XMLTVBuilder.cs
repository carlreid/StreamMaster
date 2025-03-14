using Microsoft.AspNetCore.Http;
using StreamMaster.Domain.Crypto;
using StreamMaster.Domain.XML;
using StreamMaster.Domain.XmltvXml;
using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace StreamMaster.Application.EPG.Commands;

public class XMLTVBuilder(
    IOptionsMonitor<SDSettings> sdSettingsMonitor,
    IOptionsMonitor<Setting> settings,
    IEPGService EPGService,
    IFileUtilService fileUtilService,
    ICustomPlayListBuilder customPlayListBuilder,
    IHttpContextAccessor httpContextAccessor,
    ILogger<XMLTVBuilder> logger) : IXMLTVBuilder
{
    public async Task<XMLTV?> CreateXmlTv(List<VideoStreamConfig> videoStreamConfigs, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            XMLTV xmlTv = XMLUtil.NewXMLTV;
            await ProcessServicesAsync(xmlTv, videoStreamConfigs, cancellationToken);

            // Check cancellation before sorting
            cancellationToken.ThrowIfCancellationRequested();

            xmlTv.SortXmlTv();
            return xmlTv;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError("Failed to create the XMLTV file. Exception: {Message}", ex.Message);
            return null;
        }
    }

    public string GetUrlWithPath()
    {
        HttpRequest? request = httpContextAccessor.HttpContext?.Request;
        if (request == null)
        {
            return string.Empty;
        }

        var handler = new DefaultInterpolatedStringHandler(3, 2);
        handler.AppendFormatted(request.Scheme);
        handler.AppendLiteral("://");
        handler.AppendFormatted(request.Host);
        string url = handler.ToStringAndClear();

        if (url.StartsWith("wss"))
        {
            url = "https" + url[3..];
        }
        return url;
    }

    internal async Task ProcessServicesAsync(XMLTV xmlTv, List<VideoStreamConfig> videoStreamConfigs, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        if (videoStreamConfigs == null || videoStreamConfigs.Count == 0)
        {
            return;
        }

        // Process Schedules Direct Configurations
        List<VideoStreamConfig> sdVideoStreamConfigs = [.. videoStreamConfigs.Where(a => a.EPGNumber == EPGHelper.SchedulesDirectId)];
        if (sdVideoStreamConfigs.Count > 0)
        {
            await ProcessScheduleDirectConfigsAsync(xmlTv, sdVideoStreamConfigs);
        }

        cancellation.ThrowIfCancellationRequested();

        // Process Dummy Configurations
        List<VideoStreamConfig> dummyVideoStreamConfigs = [.. videoStreamConfigs.Where(a => a.EPGNumber == EPGHelper.UserId)];
        if (dummyVideoStreamConfigs.Count > 0)
        {
            ProcessDummyConfigs(xmlTv, dummyVideoStreamConfigs);
        }

        cancellation.ThrowIfCancellationRequested();

        // Process Custom PlayList Configurations
        List<VideoStreamConfig> customVideoStreamConfigs = [.. videoStreamConfigs.Where(a => a.EPGNumber == EPGHelper.MovieId)];
        if (customVideoStreamConfigs.Count > 0)
        {
            ProcessCustomPlaylists(xmlTv, customVideoStreamConfigs);
        }

        cancellation.ThrowIfCancellationRequested();

        // Process EPG Files
        List<EPGFile> epgFiles = await EPGService.GetEPGFilesAsync();
        if (epgFiles.Count > 0)
        {
            cancellation.ThrowIfCancellationRequested();
            await ProcessEPGFileConfigsAsync(xmlTv, videoStreamConfigs, epgFiles, cancellation);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    internal async Task ProcessScheduleDirectConfigsAsync(XMLTV xmlTv, List<VideoStreamConfig> SDVideoStreamConfigs)
    {
        XMLTV? xml = await fileUtilService.ReadXmlFileAsync(BuildInfo.SDXMLFile).ConfigureAwait(false);
        if (xml == null)
        {
            return;
        }

        (List<XmltvChannel> newChannels, List<XmltvProgramme> newProgrammes) = ProcessXML(xml, SDVideoStreamConfigs);
        xmlTv.Channels.AddRange(newChannels);
        xmlTv.Programs.AddRange(newProgrammes);
    }

    internal void ProcessDummyConfigs(XMLTV xmlTv, List<VideoStreamConfig> dummyConfigs)
    {
        ConcurrentBag<XmltvChannel> channels = [];
        ConcurrentBag<XmltvProgramme> programs = [];

        _ = Parallel.ForEach(dummyConfigs, config =>
        {
            if (config.OutputProfile is null)
            {
                return;
            }

            string logoSrc = settings.CurrentValue.LogoCache ? config.Logo : config.OGLogo;

            XmltvChannel channel = new()
            {
                Id = config.OutputProfile.Id,
                DisplayNames = [new XmltvText { Text = config.Name }],
                Icons = [new XmltvIcon { Src = logoSrc }]
            };
            channels.Add(channel);

            DateTime startTime = DateTime.UtcNow.Date;
            DateTime stopTime = startTime.AddDays(sdSettingsMonitor.CurrentValue.SDEPGDays);
            int fillerProgramLength = sdSettingsMonitor.CurrentValue.XmltvFillerProgramLength;

            while (startTime < stopTime)
            {
                programs.Add(new XmltvProgramme
                {
                    Start = FormatDateTime(startTime),
                    Stop = FormatDateTime(startTime.AddHours(fillerProgramLength)),
                    Channel = config.OutputProfile.Id,
                    Titles = [new XmltvText { Language = "en", Text = config.Name }]
                });

                startTime = startTime.AddHours(fillerProgramLength);
            }
        });

        xmlTv.Channels.AddRange(channels);
        xmlTv.Programs.AddRange(programs);
    }

    internal async Task ProcessEPGFileConfigsAsync(XMLTV xmlTv, List<VideoStreamConfig> configs, List<EPGFile> epgFiles, CancellationToken cancellationToken)
    {
        foreach (EPGFile epgFile in epgFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<VideoStreamConfig> matchingConfigs = [.. configs.Where(a => a.EPGNumber == epgFile.EPGNumber)];
            if (matchingConfigs.Count == 0)
            {
                continue;
            }

            cancellationToken.ThrowIfCancellationRequested();

            XMLTV? xml = await fileUtilService.ReadXmlFileAsync(epgFile).ConfigureAwait(false);
            if (xml == null)
            {
                continue;
            }

            cancellationToken.ThrowIfCancellationRequested();

            (List<XmltvChannel> newChannels, List<XmltvProgramme> newProgrammes) = ProcessXML(xml, matchingConfigs);

            lock (xmlTv.Channels)
            {
                xmlTv.Channels.AddRange(newChannels);
            }

            lock (xmlTv.Programs)
            {
                xmlTv.Programs.AddRange(newProgrammes);
            }
        }
    }

    internal void ProcessCustomPlaylists(XMLTV xmlTv, List<VideoStreamConfig> customConfigs)
    {
        ConcurrentBag<XmltvChannel> channels = [];
        ConcurrentBag<XmltvProgramme> programs = [];

        _ = Parallel.ForEach(customConfigs, config =>
        {
            if (config.OutputProfile is null)
            {
                return;
            }

            string logoSrc = settings.CurrentValue.LogoCache ? config.Logo : config.OGLogo;

            XmltvChannel channel = new()
            {
                Id = config.OutputProfile.Id,
                DisplayNames = [new XmltvText { Text = config.Name }]
            };

            CustomPlayList? nfo = customPlayListBuilder.GetCustomPlayList(config.Name);
            string? logoFile = customPlayListBuilder.GetCustomPlayListLogoFromFileName(config.Name);

            if (logoFile is not null && !string.IsNullOrEmpty(logoFile))
            {
                channel.Icons = [new XmltvIcon { Src = logoFile }];
            }
            else if (nfo?.FolderNfo?.Thumb != null && !string.IsNullOrEmpty(nfo.FolderNfo.Thumb.Text))
            {
                channel.Icons = [new XmltvIcon { Src = nfo.FolderNfo.Thumb.Text }];
            }

            channels.Add(channel);

            List<XmltvProgramme> newProgrammes = GetXmltvProgrammeForPeriod(config, SMDT.UtcNow, sdSettingsMonitor.CurrentValue.SDEPGDays, config.BaseUrl);
            foreach (XmltvProgramme programme in newProgrammes)
            {
                programme.Channel = config.OutputProfile.Id;
                programs.Add(programme);
            }
        });

        xmlTv.Channels.AddRange(channels);
        xmlTv.Programs.AddRange(programs);
    }

    public List<XmltvProgramme> GetXmltvProgrammeForPeriod(VideoStreamConfig videoStreamConfig, DateTime startDate, int days, string baseUrl)
    {
        List<(Movie Movie, DateTime StartTime, DateTime EndTime)> moviesForPeriod = customPlayListBuilder.GetMoviesForPeriod(videoStreamConfig.Name, startDate, days);
        List<XmltvProgramme> ret = [];
        foreach ((Movie Movie, DateTime StartTime, DateTime EndTime) x in moviesForPeriod)
        {
            var xmltvProgramme = ConvertMovieToXmltvProgramme(x.Movie, videoStreamConfig.EPGId, x.StartTime, x.EndTime);
            if (x.Movie.Thumb is not null && !string.IsNullOrEmpty(x.Movie.Thumb.Text))
            {
                string src = $"/api/files/smChannelLogo/{videoStreamConfig.ChannelNumber}";
                xmltvProgramme.Icons = [new XmltvIcon { Src = src }];
            }
            ret.Add(xmltvProgramme);
        }
        return ret;
    }

    internal (List<XmltvChannel> xmltvChannels, List<XmltvProgramme> programs) ProcessXML(XMLTV xml, List<VideoStreamConfig> videoStreamConfigs)
    {
        string baseUrl = GetUrlWithPath();
        Dictionary<string, List<XmltvChannel>> channelsById = xml.Channels
            .GroupBy(channel => channel.Id)
            .ToDictionary(group => group.Key, group => group.ToList());

        Dictionary<string, List<XmltvProgramme>> programsByChannel = xml.Programs
            .GroupBy(program => program.Channel)
            .ToDictionary(group => group.Key, group => group.ToList());

        List<XmltvChannel> newChannels = [];
        List<XmltvProgramme> newProgrammes = [];

        foreach (VideoStreamConfig videoStreamConfig in videoStreamConfigs)
        {
            if (channelsById.TryGetValue(videoStreamConfig.EPGId, out List<XmltvChannel>? matchingChannels))
            {
                XmltvChannel? firstChannel = matchingChannels[0];

                List<XmltvIcon>? icons = null;

                // Determine logo source based on config and settings
                if (!string.IsNullOrEmpty(videoStreamConfig.Logo) || !string.IsNullOrEmpty(videoStreamConfig.OGLogo))
                {
                    // Use Logo or OGLogo from VideoStreamConfig
                    var logoSrc = settings.CurrentValue.LogoCache ? videoStreamConfig.Logo : videoStreamConfig.OGLogo;
                    if (!string.IsNullOrEmpty(logoSrc))
                    {
                        icons = [new XmltvIcon { Src = logoSrc }];
                    }
                }
                else if (firstChannel.Icons != null && firstChannel.Icons.Count != 0)
                {
                    // If we dont have values on the config, copy the first channel icons
                    icons = [.. firstChannel.Icons.Select(a => new XmltvIcon() { Src = a.Src })];
                }
                else
                {
                    // Fallback to custom playlist logo if no other logo is available
                    var customPlaylistLogo = customPlayListBuilder.GetCustomPlayListLogoFromFileName(videoStreamConfig.Name);
                    if (!string.IsNullOrEmpty(customPlaylistLogo))
                    {
                        icons = [new XmltvIcon { Src = customPlaylistLogo }];
                    }
                }

                XmltvChannel updatedChannel = new()
                {
                    Id = videoStreamConfig.OutputProfile!.Id,
                    DisplayNames = firstChannel.DisplayNames?.Count > 0 ? [firstChannel.DisplayNames[0]] : [new XmltvText(videoStreamConfig.OutputProfile.Id)],
                    Icons = icons
                };
                newChannels.Add(updatedChannel);
            }

            if (programsByChannel.TryGetValue(videoStreamConfig.EPGId, out List<XmltvProgramme>? matchingPrograms))
            {
                foreach (XmltvProgramme program in matchingPrograms)
                {
                    var newProgram = program.DeepCopy();
                    newProgram.Channel = videoStreamConfig.OutputProfile!.Id;

                    if (videoStreamConfig.EPGNumber == EPGHelper.SchedulesDirectId)
                    {
                        newProgram.Icons = program.Icons?.Select(icon => new XmltvIcon
                        {
                            Src = $"{baseUrl}/api/files/pr/{icon.Src.GenerateFNV1aHash()}"
                        }).ToList();
                    }

                    if (settings.CurrentValue.UseChannelLogoForProgramLogo &&
                        (newProgram.Icons == null || newProgram.Icons.Count == 0))
                    {
                        string logoSrc = settings.CurrentValue.LogoCache ? videoStreamConfig.Logo : videoStreamConfig.OGLogo;
                        newProgram.Icons = [new XmltvIcon { Src = logoSrc }];
                    }

                    newProgrammes.Add(newProgram);
                }
            }
        }

        return (newChannels, newProgrammes);
    }

    public static XmltvProgramme ConvertMovieToXmltvProgramme(Movie movie, string channelId, DateTime StartTime, DateTime EndTime)
    {
        XmltvProgramme programme = new()
        {
            Titles = [new XmltvText { Text = movie.Title }],
            Descriptions = !string.IsNullOrEmpty(movie.Plot) ? [new XmltvText { Text = movie.Plot }] : null,
            Start = FormatDateTime(StartTime),
            Stop = FormatDateTime(EndTime),
            Channel = channelId,
            Categories = movie.Genres?.ConvertAll(g => new XmltvText { Text = g }),
            Countries = !string.IsNullOrEmpty(movie.Country) ? [new() { Text = movie.Country }] : null,
            Rating = movie.Ratings?.Rating?.ConvertAll(r => new XmltvRating { Value = r.Value, System = r.Name }),
            StarRating = movie.Ratings?.Rating?.ConvertAll(r => new XmltvRating { Value = r.Value, System = r.Name }),
            Credits = movie.Actors != null ? new XmltvCredit
            {
                Actors = movie.Actors.ConvertAll(a => new XmltvActor { Role = a.Role, Actor = a.Name })
            } : null,
            EpisodeNums = !string.IsNullOrEmpty(movie.Id) ?
                [new XmltvEpisodeNum { System = "default", Text = movie.Id }] : null,
            Language = !string.IsNullOrEmpty(movie.Country) ?
                new XmltvText { Text = movie.Country } : null,
            Length = movie.Runtime > 0 ?
                new XmltvLength { Units = "minutes", Text = movie.Runtime.ToString() } : null,
            Video = new XmltvVideo
            {
                Aspect = movie.Fileinfo?.Streamdetails?.Video?.Aspect ?? "",
                Quality = movie.Rating
            }
        };

        return programme;
    }

    private static string FormatDateTime(DateTime dateTime)
    {
        return dateTime.ToString("yyyyMMddHHmmss 0000", CultureInfo.InvariantCulture);
    }
}