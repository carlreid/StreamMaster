﻿using AutoMapper;
using AutoMapper.QueryableExtensions;

using FluentValidation;

using MediatR;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

using StreamMasterApplication.Common.Extensions;

using StreamMasterDomain.Attributes;
using StreamMasterDomain.Dto;
using StreamMasterDomain.Entities.EPG;

using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Serialization;

using static StreamMasterDomain.Common.GetStreamGroupEPGHandler;

namespace StreamMasterApplication.StreamGroups.Queries;

[RequireAll]
public record GetStreamGroupEPG(int StreamGroupNumber) : IRequest<string>;

public class GetStreamGroupEPGValidator : AbstractValidator<GetStreamGroupEPG>
{
    public GetStreamGroupEPGValidator()
    {
        _ = RuleFor(v => v.StreamGroupNumber)
            .NotNull().GreaterThanOrEqualTo(0);
    }
}

public partial class GetStreamGroupEPGHandler : IRequestHandler<GetStreamGroupEPG, string>
{
    protected Setting _setting = FileUtil.GetSetting();
    private readonly IAppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _memoryCache;
    private readonly ISender _sender;
    private readonly object Lock = new();
    private int dummyCount = 0;

    public GetStreamGroupEPGHandler(
        IMapper mapper, IMemoryCache memoryCache,
        ISender sender,
        IHttpContextAccessor httpContextAccessor,
        IAppDbContext context)
    {
        _httpContextAccessor = httpContextAccessor;
        _memoryCache = memoryCache;
        _mapper = mapper;
        _context = context;
        _sender = sender;
    }

    public async Task<string> Handle(GetStreamGroupEPG command, CancellationToken cancellationToken)
    {
        List<VideoStreamDto> videoStreams = new();
        if (command.StreamGroupNumber > 0)
        {
            StreamGroupDto? sg = await _sender.Send(new GetStreamGroupByStreamNumber(command.StreamGroupNumber), cancellationToken).ConfigureAwait(false);
            if (sg == null)
            {
                return "";
            }
            videoStreams = sg.VideoStreams.Where(a => !a.IsHidden).ToList();
        }
        else
        {
            videoStreams = _context.VideoStreams
                .Where(a => !a.IsHidden)
                .AsNoTracking()
                .ProjectTo<VideoStreamDto>(_mapper.ConfigurationProvider)
                .ToList();
        }

        ParallelOptions po = new()
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = System.Environment.ProcessorCount
        };
        ConcurrentBag<TvChannel> retChannels = new();
        ConcurrentBag<Programme> retProgrammes = new();

        if (videoStreams.Any())
        {
            string url = _httpContextAccessor.GetUrl();

            List<string> epgids = videoStreams.Where(a => !a.IsHidden).SelectMany(r => new string[] { r.User_Tvg_ID.ToLower(), r.User_Tvg_ID_DisplayName.ToLower() }).Distinct().ToList();

            List<Programme> programmes = _memoryCache.Programmes()
                .Where(a =>
                a.Channel != null &&
                (
                    epgids.Contains(a.Channel.ToLower()) ||
                    epgids.Contains(a.DisplayName.ToLower())
                )
                ).ToList();

            SettingDto setting = await _sender.Send(new GetSettings(), cancellationToken).ConfigureAwait(false);

            List<IconFile> progIcons = _context.Icons.Where(a => a.SMFileType == SMFileTypes.ProgrammeIcon && a.FileExists).ToList();

            var icons = await _context.GetIcons(cancellationToken).ConfigureAwait(false);// _sender.Send(new GetIcons(), cancellationToken).ConfigureAwait(false);

            _ = Parallel.ForEach(videoStreams, po, videoStream =>
            {
                if (videoStream == null)
                {
                    return;
                }

                IconFileDto? icon = icons.SingleOrDefault(a => a.OriginalSource == videoStream.User_Tvg_logo);
                string Logo = icon != null ? url + icon.Source : url + "/" + setting.DefaultIcon;

                TvChannel t;

                int dummy = 0;

                if (IsVideoStreamADummy(videoStream) || IsNotInProgrammes(programmes, videoStream))
                {
                    videoStream.User_Tvg_ID = "dummy";
                }

                if (videoStream.User_Tvg_ID.ToLower() == "dummy")
                {
                    dummy = GetDummy();

                    t = new TvChannel
                    {
                        Id = videoStream.User_Tvg_name,
                        Icon = new TvIcon { Src = Logo },
                        Displayname = new()
                        {
                            videoStream.User_Tvg_name,
                            "dummy-" + dummy
                        }
                    };
                }
                else
                {
                    t = new TvChannel
                    {
                        Id = videoStream.User_Tvg_name,
                        Icon = new TvIcon { Src = Logo },
                        Displayname = new()
                        {
                            videoStream.User_Tvg_name
                        }
                    };
                }

                retChannels.Add(t);

                if (videoStream.User_Tvg_ID != null)
                {
                    if (videoStream.User_Tvg_ID.ToLower() == "dummy")
                    {
                        var prog = new Programme();

                        prog.Channel = videoStream.User_Tvg_name;

                        prog.Title = new TvTitle
                        {
                            Lang = "en",
                            Text = videoStream.User_Tvg_name,
                        };
                        prog.Desc = new TvDesc
                        {
                            Lang = "en",
                            Text = videoStream.User_Tvg_name,
                        };
                        DateTime now = DateTime.Now;
                        prog.Icon.Add(new TvIcon { Height = "10", Width = "10", Src = $"{url}/images/transparent.png" });
                        prog.Start = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0).ToString("yyyyMMddHHmmss zzz").Replace(":", ""); ;
                        now = now.AddDays(7);
                        prog.Stop = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0).ToString("yyyyMMddHHmmss zzz").Replace(":", ""); ;
                        prog.New = null;
                        prog.Previouslyshown = null;
                        retProgrammes.Add(prog);
                    }
                    else
                    {
                        if (programmes.Any())
                        {
                            IEnumerable<Programme>? progs = programmes.Where(a => a.DisplayName.ToLower() == videoStream.User_Tvg_ID.ToLower() || a.Channel.ToLower() == videoStream.User_Tvg_ID.ToLower()).DeepCopy();

                            if (progs != null)
                            {
                                foreach (Programme? p in progs)
                                {
                                    if (p.Icon.Any())
                                    {
                                        foreach (TvIcon progIcon in p.Icon)
                                        {
                                            if (progIcon != null && !string.IsNullOrEmpty(progIcon.Src))
                                            {
                                                IconFile? programmeIcon = progIcons.FirstOrDefault(a => a.SMFileType == SMFileTypes.ProgrammeIcon && a.Source == progIcon.Src);

                                                if (programmeIcon == null)
                                                {
                                                    continue;
                                                }
                                                string IconSource = $"{url}/api/files/{(int)SMFileTypes.ProgrammeIcon}/{HttpUtility.UrlEncode(programmeIcon.Source)}";
                                                progIcon.Src = IconSource;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        p.Icon.Add(new TvIcon { Height = "", Width = "", Src = "" });
                                    }

                                    p.Channel = videoStream.User_Tvg_name;

                                    if (videoStream.User_Tvg_ID.ToLower() == "dummy")
                                    {
                                        p.Channel = videoStream.User_Tvg_name;

                                        p.Title = new TvTitle
                                        {
                                            Lang = "en",
                                            Text = videoStream.User_Tvg_name,
                                        };
                                        p.Desc = new TvDesc
                                        {
                                            Lang = "en",
                                            Text = videoStream.User_Tvg_name,
                                        };
                                    }

                                    if (string.IsNullOrEmpty(p.New))
                                    {
                                        p.New = null;
                                    }

                                    if (string.IsNullOrEmpty(p.Live))
                                    {
                                        p.Live = null;
                                    }

                                    if (string.IsNullOrEmpty(p.Premiere))
                                    {
                                        p.Premiere = null;
                                    }

                                    if (p.Previouslyshown == null || string.IsNullOrEmpty(p.Previouslyshown.Start))
                                    {
                                        p.Previouslyshown = null;
                                    }

                                    if (videoStream.User_Tvg_ID.ToLower() == "dummy")
                                    {
                                        continue;
                                    }
                                    retProgrammes.Add(p);
                                }
                            }
                        }
                    }
                }
            });
        }

        Tv ret = new()
        {
            Channel = retChannels.ToList(),
            Programme = retProgrammes.ToList()
        };

        XmlSerializerNamespaces ns = new();
        ns.Add("", "");

        using Utf8StringWriter textWriter = new();
        XmlSerializer serializer = new(typeof(Tv));
        serializer.Serialize(textWriter, ret, ns);
        textWriter.Close();
        return textWriter.ToString();
    }

    private int GetDummy()
    {
        lock (Lock)
        {
            ++dummyCount;
            return dummyCount;
        }
    }

    private bool IsNotInProgrammes(IEnumerable<Programme> programmes, VideoStreamDto videoStream)
    {
        string userTvgId = videoStream.User_Tvg_ID.ToLower();
        string userTvgIdDisplayName = videoStream.User_Tvg_ID_DisplayName.ToLower();

        return !programmes.Any(p =>
            string.Equals(p.Channel, userTvgId, StringComparison.InvariantCultureIgnoreCase) ||
            string.Equals(p.Channel, userTvgIdDisplayName, StringComparison.InvariantCultureIgnoreCase));
    }

    private bool IsVideoStreamADummy(VideoStreamDto videoStream)
    {
        if (string.IsNullOrEmpty(videoStream.User_Tvg_ID))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(_setting.DummyRegex))
        {
            Regex regex = new(_setting.DummyRegex, RegexOptions.ECMAScript | RegexOptions.IgnoreCase);
            var test = regex.IsMatch(videoStream.User_Tvg_ID);
            return test;
        }

        return false;
    }
}
