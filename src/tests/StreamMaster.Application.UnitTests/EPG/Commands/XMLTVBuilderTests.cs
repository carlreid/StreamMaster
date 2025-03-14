using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using StreamMaster.Application.EPG.Commands;
using StreamMaster.Domain.Configuration;
using StreamMaster.Domain.Helpers;
using StreamMaster.Domain.Models;
using StreamMaster.Domain.Services;
using StreamMaster.Domain.XML;
using StreamMaster.Domain.XmltvXml;
using StreamMaster.PlayList;
using StreamMaster.PlayList.Models;
using System.Globalization;

namespace StreamMaster.Application.UnitTests.EPG.Commands;

public class XMLTVBuilderTests : IDisposable
{
    private readonly Mock<IOptionsMonitor<SDSettings>> _sdSettingsMock = new();
    private readonly Mock<IOptionsMonitor<Setting>> _settingsMock = new();
    private readonly Mock<IEPGService> _epgServiceMock = new();
    private readonly Mock<IFileUtilService> _fileUtilServiceMock = new();
    private readonly Mock<ICustomPlayListBuilder> _customPlayListBuilderMock = new();
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
    private readonly Mock<ILogger<XMLTVBuilder>> _loggerMock = new();

    private readonly XMLTVBuilder _builder;
    private readonly SDSettings _sdSettings = new() { SDEPGDays = 7, XmltvFillerProgramLength = 4 };
    private readonly Setting _appSettings = new() { LogoCache = true, UseChannelLogoForProgramLogo = true };

    public XMLTVBuilderTests()
    {
        _sdSettingsMock.Setup(x => x.CurrentValue).Returns(_sdSettings);
        _settingsMock.Setup(x => x.CurrentValue).Returns(_appSettings);
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());

        _builder = new XMLTVBuilder(
            _sdSettingsMock.Object,
            _settingsMock.Object,
            _epgServiceMock.Object,
            _fileUtilServiceMock.Object,
            _customPlayListBuilderMock.Object,
            _httpContextAccessorMock.Object,
            _loggerMock.Object);
    }

    public void Dispose() => GC.SuppressFinalize(this);

    [Fact]
    public async Task CreateXmlTv_WithEmptyConfigs_ReturnsEmptyXmlTv()
    {
        var result = await _builder.CreateXmlTv([], CancellationToken.None);

        result.ShouldNotBeNull();
        result.Channels.ShouldBeEmpty();
        result.Programs.ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateXmlTv_WithProcessingException_ReturnsNullAndLogsError()
    {
        // Arrange
        _fileUtilServiceMock.Setup(x => x.ReadXmlFileAsync(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        var config = new VideoStreamConfig
        {
            EPGNumber = EPGHelper.SchedulesDirectId,
            OutputProfile = new OutputProfileDto { Id = "test" }
        };

        // Act
        var result = await _builder.CreateXmlTv([config], CancellationToken.None);

        // Assert
        result.ShouldBeNull();
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Test exception")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ProcessDummyConfigs_CreatesValidChannelsAndPrograms()
    {
        var config = new VideoStreamConfig
        {
            Name = "Test Channel",
            OutputProfile = new OutputProfileDto { Id = "dummy1" },
            Logo = "test-logo.png"
        };

        var xmlTv = XMLUtil.NewXMLTV;
        _builder.ProcessDummyConfigs(xmlTv, [config]);

        xmlTv.Channels.Count.ShouldBe(1);
        var channel = xmlTv.Channels[0];
        channel.Id.ShouldBe("dummy1");
        channel.DisplayNames[0].Text.ShouldBe("Test Channel");
        channel.Icons[0].Src.ShouldBe("test-logo.png");

        xmlTv.Programs.ShouldNotBeEmpty();
        var program = xmlTv.Programs[0];
        program.Channel.ShouldBe("dummy1");
        program.Titles[0].Text.ShouldBe("Test Channel");
    }

    [Fact]
    public async Task ProcessEPGFileConfigs_HandlesMultipleFiles()
    {
        var epgFile = new EPGFile { EPGNumber = 123 };
        var testXml = XMLUtil.NewXMLTV;
        testXml.Channels.Add(new XmltvChannel { Id = "epg1" });
        testXml.Programs.Add(new XmltvProgramme { Channel = "epg1" });

        _epgServiceMock.Setup(x => x.GetEPGFilesAsync()).ReturnsAsync([epgFile]);
        _fileUtilServiceMock.Setup(x => x.ReadXmlFileAsync(epgFile)).ReturnsAsync(testXml);

        var config = new VideoStreamConfig
        {
            EPGNumber = 123,
            EPGId = "epg1",
            OutputProfile = new OutputProfileDto { Id = "mapped1" }
        };

        var xmlTv = XMLUtil.NewXMLTV;
        await _builder.ProcessEPGFileConfigsAsync(xmlTv, [config], [epgFile], CancellationToken.None);

        xmlTv.Channels.Count.ShouldBe(1);
        xmlTv.Programs.Count.ShouldBe(1);
        xmlTv.Channels[0].Id.ShouldBe("mapped1");
        xmlTv.Programs[0].Channel.ShouldBe("mapped1");
    }

    [Fact]
    public void ProcessXML_HandlesChannelMappingAndLogos()
    {
        var inputXml = XMLUtil.NewXMLTV;
        inputXml.Channels.Add(new XmltvChannel
        {
            Id = "source1",
            DisplayNames = [new XmltvText { Text = "Source Channel" }],
            Icons = [new XmltvIcon { Src = "original-logo.png" }]
        });
        inputXml.Programs.Add(new XmltvProgramme
        {
            Channel = "source1",
            Icons = [new XmltvIcon { Src = "program-logo.png" }]
        });

        var config = new VideoStreamConfig
        {
            EPGId = "source1",
            OutputProfile = new OutputProfileDto { Id = "target1" },
            Logo = "new-logo.png"
        };

        var (channels, programs) = _builder.ProcessXML(inputXml, [config]);

        channels.Count.ShouldBe(1);
        channels[0].Id.ShouldBe("target1");
        channels[0].Icons[0].Src.ShouldBe("new-logo.png");

        programs.Count.ShouldBe(1);
        programs[0].Channel.ShouldBe("target1");
    }

    [Fact]
    public void ProcessCustomPlaylists_CreatesValidChannelsAndPrograms()
    {
        // Arrange
        var config = new VideoStreamConfig
        {
            Name = "Custom List",
            OutputProfile = new OutputProfileDto { Id = "custom1" },
            Logo = "custom-logo.png",
            BaseUrl = "http://test.com"
        };

        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddHours(2);

        _customPlayListBuilderMock
            .Setup(x => x.GetCustomPlayListLogoFromFileName("Custom List"))
            .Returns("custom-logo.png");

        _customPlayListBuilderMock
            .Setup(x => x.GetMoviesForPeriod(
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<int>()))
            .Returns(new List<(Movie Movie, DateTime StartTime, DateTime EndTime)>
            {
            (
                new Movie
                {
                    Title = "Test Movie",
                    Plot = "Test Plot",
                    Thumb = new Thumb { Text = "movie-thumb.jpg" }
                },
                startTime,
                endTime
            )
            });

        var xmlTv = XMLUtil.NewXMLTV;

        // Act
        _builder.ProcessCustomPlaylists(xmlTv, [config]);

        // Assert
        xmlTv.Channels.Count.ShouldBe(1);
        var channel = xmlTv.Channels[0];
        channel.Id.ShouldBe("custom1");
        channel.DisplayNames[0].Text.ShouldBe("Custom List");
        channel.Icons[0].Src.ShouldBe("custom-logo.png");

        xmlTv.Programs.Count.ShouldBe(1);
        var program = xmlTv.Programs[0];
        program.Channel.ShouldBe("custom1");
        program.Titles.ShouldNotBeNull();
        program.Titles[0].Text.ShouldBe("Test Movie");
        program.Descriptions.ShouldNotBeNull();
        program.Descriptions[0].Text.ShouldBe("Test Plot");
        program.Start.ShouldBe(startTime.ToString("yyyyMMddHHmmss 0000", CultureInfo.InvariantCulture));
        program.Stop.ShouldBe(endTime.ToString("yyyyMMddHHmmss 0000", CultureInfo.InvariantCulture));
    }

    [Fact]
    public void GetUrlWithPath_WithHttpScheme_ReturnsOriginalUrl()
    {
        var context = new DefaultHttpContext
        {
            Request = { Scheme = "http", Host = new HostString("testhost:8080") }
        };
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);

        var result = _builder.GetUrlWithPath();

        result.ShouldBe("http://testhost:8080");
    }

    [Fact]
    public void GetUrlWithPath_WithNullContext_ReturnsEmptyString()
    {
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext)null);

        var result = _builder.GetUrlWithPath();

        result.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task CreateXmlTv_WithCancellation_ThrowsOperationCanceled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var config = new VideoStreamConfig
        {
            EPGNumber = EPGHelper.SchedulesDirectId,
            OutputProfile = new OutputProfileDto { Id = "test" }
        };

        _fileUtilServiceMock
            .Setup(x => x.ReadXmlFileAsync(It.IsAny<string>()))
            .Returns<string>(async (path) =>
            {
                // Ensure cancellation is checked during execution
                cts.Token.ThrowIfCancellationRequested();
                return XMLUtil.NewXMLTV;
            });

        // Cancel immediately
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await _builder.CreateXmlTv([config], cts.Token));
    }

    [Fact]
    public async Task ProcessScheduleDirectConfigsAsync_WithNullXml_ReturnsWithoutChanges()
    {
        // Arrange
        var xmlTv = XMLUtil.NewXMLTV;
        _fileUtilServiceMock.Setup(x => x.ReadXmlFileAsync(It.IsAny<string>())).ReturnsAsync((XMLTV)null);

        // Act
        await _builder.ProcessScheduleDirectConfigsAsync(xmlTv, [new VideoStreamConfig()]);

        // Assert
        xmlTv.Channels.ShouldBeEmpty();
        xmlTv.Programs.ShouldBeEmpty();
    }

    [Fact]
    public void ConvertMovieToXmltvProgramme_WithMinimalProperties_CreatesValidProgramme()
    {
        // Arrange
        var movie = new Movie { Title = "Test" };
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddHours(2);

        // Act
        var result = XMLTVBuilder.ConvertMovieToXmltvProgramme(movie, "channel1", startTime, endTime);

        // Assert
        result.ShouldNotBeNull();
        result.Titles.ShouldHaveSingleItem().Text.ShouldBe("Test");
        result.Channel.ShouldBe("channel1");
        result.Start.ShouldBe(startTime.ToString("yyyyMMddHHmmss 0000", CultureInfo.InvariantCulture));
        result.Stop.ShouldBe(endTime.ToString("yyyyMMddHHmmss 0000", CultureInfo.InvariantCulture));

        // These should all be null when not provided
        result.Categories.ShouldBeNull();
        result.Countries.ShouldBeNull();
        result.Rating.ShouldBeNull();
        result.StarRating.ShouldBeNull();
        result.Credits.ShouldBeNull();
        result.Video.ShouldNotBeNull(); // Video is always created
        result.Length.ShouldBeNull();
    }

    [Fact]
    public void ConvertMovieToXmltvProgramme_WithFullProperties_IncludesAllData()
    {
        // Arrange
        var movie = new Movie
        {
            Title = "Test Movie",
            Plot = "Test Plot",
            Genres = ["Action", "Drama"],
            Country = "USA",
            Runtime = 120,
            Ratings = new Ratings
            {
                Rating = [new Rating { Name = "IMDB", Value = "8.5" }]
            },
            Actors = [new Actor { Name = "John Doe", Role = "Main Character" }],
            Id = "12345",
            Fileinfo = new Fileinfo
            {
                Streamdetails = new Streamdetails
                {
                    Video = new Video { Aspect = "16:9" }
                }
            }
        };

        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddHours(2);

        // Act
        var result = XMLTVBuilder.ConvertMovieToXmltvProgramme(movie, "channel1", startTime, endTime);

        // Assert
        result.ShouldNotBeNull();
        result.Titles.ShouldHaveSingleItem().Text.ShouldBe("Test Movie");
        result.Descriptions.ShouldHaveSingleItem().Text.ShouldBe("Test Plot");
        result.Categories.Count.ShouldBe(2);
        result.Categories[0].Text.ShouldBe("Action");
        result.Categories[1].Text.ShouldBe("Drama");
        result.Countries.ShouldHaveSingleItem().Text.ShouldBe("USA");
        result.Rating.ShouldHaveSingleItem().Value.ShouldBe("8.5");
        result.StarRating.ShouldHaveSingleItem().Value.ShouldBe("8.5");
        result.Credits.Actors.ShouldHaveSingleItem().Actor.ShouldBe("John Doe");
        result.EpisodeNums.ShouldHaveSingleItem().Text.ShouldBe("12345");
        result.Length.Text.ShouldBe("120");
        result.Video.Aspect.ShouldBe("16:9");
    }

    [Fact]
    public void ConvertMovieToXmltvProgramme_WithNullCollections_HandlesGracefully()
    {
        // Arrange
        var movie = new Movie
        {
            Title = "Test Movie",
            Genres = null,
            Ratings = null,
            Actors = null
        };

        // Act
        var result = XMLTVBuilder.ConvertMovieToXmltvProgramme(
            movie,
            "channel1",
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(2)
        );

        // Assert
        result.ShouldNotBeNull();
        result.Categories.ShouldBeNull();
        result.Rating.ShouldBeNull();
        result.StarRating.ShouldBeNull();
        result.Credits.ShouldBeNull();
    }

    [Fact]
    public void ProcessDummyConfigs_WithNullOutputProfile_SkipsConfig()
    {
        // Arrange
        var config = new VideoStreamConfig { Name = "Test", OutputProfile = null };
        var xmlTv = XMLUtil.NewXMLTV;

        // Act
        _builder.ProcessDummyConfigs(xmlTv, [config]);

        // Assert
        xmlTv.Channels.ShouldBeEmpty();
        xmlTv.Programs.ShouldBeEmpty();
    }

    [Fact]
    public void GetUrlWithPath_WithWssScheme_ConvertsToHttps()
    {
        // Arrange
        var context = new DefaultHttpContext
        {
            Request = { Scheme = "wss", Host = new HostString("testhost:8080") }
        };
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);

        // Act
        var result = _builder.GetUrlWithPath();

        // Assert
        result.ShouldBe("https://testhost:8080");
    }

    [Fact]
    public void ProcessXML_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var inputXml = XMLUtil.NewXMLTV;
        inputXml.Channels.Add(new XmltvChannel
        {
            Id = "source&1",
            DisplayNames = [new XmltvText { Text = "Source & Channel" }]
        });

        var config = new VideoStreamConfig
        {
            EPGId = "source&1",
            OutputProfile = new OutputProfileDto { Id = "target&1" }
        };

        // Act
        var (channels, programs) = _builder.ProcessXML(inputXml, [config]);

        // Assert
        channels[0].Id.ShouldBe("target&1");
        channels[0].DisplayNames[0].Text.ShouldBe("Source & Channel");
    }

    [Fact]
    public void ProcessCustomPlaylists_WithNullOutputProfile_SkipsProcessing()
    {
        // Arrange
        var config = new VideoStreamConfig
        {
            Name = "Test",
            OutputProfile = null,
            Logo = "test.png",
            OGLogo = "test.png"
        };
        var xmlTv = XMLUtil.NewXMLTV;

        _customPlayListBuilderMock
            .Setup(x => x.GetMoviesForPeriod(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<int>()))
            .Returns(new List<(Movie, DateTime, DateTime)>());

        // Act
        _builder.ProcessCustomPlaylists(xmlTv, [config]);

        // Assert
        xmlTv.Channels.ShouldBeEmpty();
        xmlTv.Programs.ShouldBeEmpty();
    }

    [Fact]
    public void ProcessCustomPlaylists_WithNullNfoAndLogo_UsesDefaultValues()
    {
        // Arrange
        var config = new VideoStreamConfig
        {
            Name = "Test",
            OutputProfile = new OutputProfileDto { Id = "test1" },
            Logo = "test.png",
            OGLogo = "test.png"
        };

        _customPlayListBuilderMock
            .Setup(x => x.GetCustomPlayList(It.IsAny<string>()))
            .Returns((CustomPlayList)null);

        _customPlayListBuilderMock
            .Setup(x => x.GetCustomPlayListLogoFromFileName(It.IsAny<string>()))
            .Returns((string)null);

        _customPlayListBuilderMock
            .Setup(x => x.GetMoviesForPeriod(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<int>()))
            .Returns(new List<(Movie, DateTime, DateTime)>());

        var xmlTv = XMLUtil.NewXMLTV;

        // Act
        _builder.ProcessCustomPlaylists(xmlTv, [config]);

        // Assert
        xmlTv.Channels.Count.ShouldBe(1);
        xmlTv.Channels[0].Icons.ShouldBeNull();
    }

    [Fact]
    public void ProcessXML_WithNullDisplayNames_UsesOutputProfileId()
    {
        var inputXml = XMLUtil.NewXMLTV;
        inputXml.Channels.Add(new XmltvChannel
        {
            Id = "source1",
            DisplayNames = null
        });

        var config = new VideoStreamConfig
        {
            EPGId = "source1",
            OutputProfile = new OutputProfileDto { Id = "target1" }
        };

        var (channels, _) = _builder.ProcessXML(inputXml, [config]);

        channels[0].DisplayNames[0].Text.ShouldBe("target1");
    }

    [Fact]
    public async Task ProcessEPGFileConfigsAsync_WithNullXml_SkipsProcessing()
    {
        var epgFile = new EPGFile { EPGNumber = 123 };
        var config = new VideoStreamConfig
        {
            EPGNumber = 123,
            EPGId = "epg1",
            OutputProfile = new OutputProfileDto { Id = "mapped1" }
        };

        _fileUtilServiceMock.Setup(x => x.ReadXmlFileAsync(epgFile)).ReturnsAsync((XMLTV)null);

        var xmlTv = XMLUtil.NewXMLTV;
        await _builder.ProcessEPGFileConfigsAsync(xmlTv, [config], [epgFile], CancellationToken.None);

        xmlTv.Channels.ShouldBeEmpty();
        xmlTv.Programs.ShouldBeEmpty();
    }

    [Fact]
    public void ProcessDummyConfigs_WithEmptyConfigs_DoesNotAddChannelsOrPrograms()
    {
        var xmlTv = XMLUtil.NewXMLTV;
        _builder.ProcessDummyConfigs(xmlTv, []);

        xmlTv.Channels.ShouldBeEmpty();
        xmlTv.Programs.ShouldBeEmpty();
    }

    [Fact]
    public async Task ProcessServicesAsync_WithNullVideoStreamConfigs_ReturnsEarly()
    {
        var xmlTv = XMLUtil.NewXMLTV;
        await _builder.ProcessServicesAsync(xmlTv, null!, CancellationToken.None);

        xmlTv.Channels.ShouldBeEmpty();
        xmlTv.Programs.ShouldBeEmpty();

        // Verify no calls were made to services
        _epgServiceMock.Verify(x => x.GetEPGFilesAsync(), Times.Never);
        _fileUtilServiceMock.Verify(x => x.ReadXmlFileAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void GetXmltvProgrammeForPeriod_WithEmptyMovieList_ReturnsEmptyList()
    {
        var config = new VideoStreamConfig
        {
            Name = "Test",
            EPGId = "test1",
            ChannelNumber = 1
        };

        _customPlayListBuilderMock
            .Setup(x => x.GetMoviesForPeriod(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<int>()))
            .Returns(new List<(Movie, DateTime, DateTime)>());

        var result = _builder.GetXmltvProgrammeForPeriod(config, DateTime.UtcNow, 7, "http://test.com");

        result.ShouldBeEmpty();
    }

    [Fact]
    public void ProcessXML_WithNoMatchingChannels_SkipsConfig()
    {
        var inputXml = XMLUtil.NewXMLTV;
        inputXml.Channels.Add(new XmltvChannel { Id = "source1" });

        var config = new VideoStreamConfig
        {
            EPGId = "source2", // Non-matching EPGId
            OutputProfile = new OutputProfileDto { Id = "target1" }
        };

        var (channels, programs) = _builder.ProcessXML(inputXml, [config]);

        channels.ShouldBeEmpty();
        programs.ShouldBeEmpty();
    }

    [Fact]
    public void SortXmlTv_OrdersChannelsAndProgramsCorrectly()
    {
        // Arrange
        var xmlTv = XMLUtil.NewXMLTV;

        // Add channels in random order
        xmlTv.Channels.AddRange([
            new XmltvChannel { Id = "channel3" },
        new XmltvChannel { Id = "channel1" },
        new XmltvChannel { Id = "channel2" }
        ]);

        // Add programs in random order with different start times
        xmlTv.Programs.AddRange([
            new XmltvProgramme {
            Channel = "channel1",
            Start = "20250223140000 0000",  // Later program
            StartDateTime = DateTime.Parse("2025-02-23T14:00:00Z")
        },
        new XmltvProgramme {
            Channel = "channel1",
            Start = "20250223120000 0000",  // Earlier program
            StartDateTime = DateTime.Parse("2025-02-23T12:00:00Z")
        },
        new XmltvProgramme {
            Channel = "channel2",
            Start = "20250223130000 0000",  // Middle program
            StartDateTime = DateTime.Parse("2025-02-23T13:00:00Z")
        }
        ]);

        // Act
        xmlTv.SortXmlTv();

        // Assert
        // Verify channels are sorted by Id
        xmlTv.Channels.Select(c => c.Id).ToList()
            .ShouldBe(["channel1", "channel2", "channel3"]);

        // Get all channel1 programs
        var channel1Programs = xmlTv.Programs
            .Where(p => p.Channel == "channel1")
            .OrderBy(p => p.StartDateTime)  // Ensure we're comparing in time order
            .ToList();

        // Verify the ordering
        channel1Programs.Count.ShouldBe(2);
        channel1Programs[0].Start.ShouldBe("20250223120000 0000");  // Earlier time should be first
        channel1Programs[1].Start.ShouldBe("20250223140000 0000");  // Later time should be second
    }

    [Fact]
    public void ProcessXML_WithUseChannelLogoFalse_DoesNotOverrideProgramIcons()
    {
        // Arrange
        _settingsMock.Setup(x => x.CurrentValue).Returns(new Setting
        {
            UseChannelLogoForProgramLogo = false,
            LogoCache = true
        });

        var inputXml = XMLUtil.NewXMLTV;
        inputXml.Channels.Add(new XmltvChannel
        {
            Id = "source1",
            Icons = [new XmltvIcon { Src = "channel-logo.png" }]
        });

        inputXml.Programs.Add(new XmltvProgramme
        {
            Channel = "source1",
            Icons = [new XmltvIcon { Src = "program-logo.png" }]
        });

        var config = new VideoStreamConfig
        {
            EPGId = "source1",
            OutputProfile = new OutputProfileDto { Id = "target1" },
            Logo = "new-logo.png"
        };

        // Act
        var (_, programs) = _builder.ProcessXML(inputXml, [config]);

        // Assert
        programs.Count.ShouldBe(1);
        programs[0].Icons.ShouldHaveSingleItem();
        programs[0].Icons[0].Src.ShouldBe("program-logo.png");
    }

    [Fact]
    public async Task ProcessScheduleDirectConfigsAsync_WithOrphanedPrograms_HandlesGracefully()
    {
        // Arrange
        var xmlTv = XMLUtil.NewXMLTV;
        var sdXml = XMLUtil.NewXMLTV;

        sdXml.Programs.Add(new XmltvProgramme
        {
            Channel = "orphaned-channel",
            Titles = [new XmltvText { Text = "Orphaned Program" }]
        });

        _fileUtilServiceMock.Setup(x => x.ReadXmlFileAsync(BuildInfo.SDXMLFile))
            .ReturnsAsync(sdXml);

        var config = new VideoStreamConfig
        {
            EPGNumber = EPGHelper.SchedulesDirectId,
            EPGId = "orphaned-channel",
            OutputProfile = new OutputProfileDto { Id = "mapped-channel" }
        };

        // Act
        await _builder.ProcessScheduleDirectConfigsAsync(xmlTv, [config]);

        // Assert
        xmlTv.Channels.ShouldBeEmpty();
        xmlTv.Programs.Count.ShouldBe(1);
        xmlTv.Programs[0].Channel.ShouldBe("mapped-channel");
    }

    [Fact]
    public void ConvertMovieToXmltvProgramme_WithZeroRuntime_OmitsLength()
    {
        // Arrange
        var movie = new Movie
        {
            Title = "Test Movie",
            Runtime = 0
        };
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddHours(2);

        // Act
        var result = XMLTVBuilder.ConvertMovieToXmltvProgramme(
            movie,
            "channel1",
            startTime,
            endTime
        );

        // Assert
        result.Length.ShouldBeNull();
    }

    [Fact]
    public void ProcessXML_WithLogoFallback_UsesCorrectLogoSource()
    {
        // Arrange
        _settingsMock.Setup(x => x.CurrentValue).Returns(new Setting
        {
            LogoCache = false
        });

        var inputXml = XMLUtil.NewXMLTV;
        inputXml.Channels.Add(new XmltvChannel
        {
            Id = "source1",
            DisplayNames = [new XmltvText { Text = "Source Channel" }],
            Icons = [new XmltvIcon { Src = "existing-logo.png" }]
        });

        var config = new VideoStreamConfig
        {
            EPGId = "source1",
            OutputProfile = new OutputProfileDto { Id = "target1" },
            Logo = "cached-logo.png",
            OGLogo = "original-logo.png"
        };

        // Act
        var (channels, _) = _builder.ProcessXML(inputXml, [config]);

        // Assert
        channels.ShouldHaveSingleItem();
        channels[0].Icons.ShouldNotBeNull();
        channels[0].Icons.ShouldHaveSingleItem();
        channels[0].Icons[0].Src.ShouldBe("original-logo.png");
    }

    [Fact]
    public void ProcessXML_WithConcurrentAccess_MaintainsDataIntegrity()
    {
        // Arrange
        var inputXml = XMLUtil.NewXMLTV;
        var configs = new List<VideoStreamConfig>();

        // Create multiple configs to trigger parallel processing
        for (int i = 0; i < 100; i++)
        {
            inputXml.Channels.Add(new XmltvChannel
            {
                Id = $"source{i}",
                DisplayNames = [new XmltvText { Text = $"Channel {i}" }]
            });

            configs.Add(new VideoStreamConfig
            {
                EPGId = $"source{i}",
                OutputProfile = new OutputProfileDto { Id = $"target{i}" },
                Logo = $"logo{i}.png"
            });
        }

        // Act
        var (channels, _) = _builder.ProcessXML(inputXml, configs);

        // Assert
        channels.Count.ShouldBe(100);
        channels.Select(c => c.Id).Distinct().Count().ShouldBe(100);
        channels.All(c => c.DisplayNames?.Count == 1).ShouldBeTrue();
    }

    [Fact]
    public void ProcessXML_ValidatesCompleteXMLStructure()
    {
        // Arrange
        var inputXml = XMLUtil.NewXMLTV;
        var startTime = DateTime.UtcNow;
        var stopTime = startTime.AddHours(1);

        inputXml.Channels.Add(new XmltvChannel
        {
            Id = "source1",
            DisplayNames = [new XmltvText { Text = "Channel Name", Language = "en" }],
            Icons = [new XmltvIcon { Src = "logo.png" }]
        });

        inputXml.Programs.Add(new XmltvProgramme
        {
            Channel = "source1",
            Start = startTime.ToString("yyyyMMddHHmmss 0000"),
            Stop = stopTime.ToString("yyyyMMddHHmmss 0000"),
            Titles = [new XmltvText { Text = "Program Title", Language = "en" }],
            SubTitles = [new XmltvText { Text = "Subtitle" }],
            Descriptions = [new XmltvText { Text = "Description" }],
            Credits = new XmltvCredit
            {
                Directors = ["Director Name"],
                Actors = [new XmltvActor { Actor = "Actor Name", Role = "Character" }]
            },
            Date = "2025",
            Categories = [new XmltvText { Text = "Category" }],
            Keywords = [new XmltvText { Text = "Keyword" }],
            Language = new XmltvText { Text = "en" },
            Length = new XmltvLength { Units = "minutes", Text = "60" },
            Icons = [new XmltvIcon { Src = "program-logo.png" }],
            Countries = [new XmltvText { Text = "USA" }],
            EpisodeNums = [new XmltvEpisodeNum { System = "onscreen", Text = "S01E01" }],
            Video = new XmltvVideo { Present = "yes" },
            Audio = new XmltvAudio { Present = "no" },
            PreviouslyShown = new XmltvPreviouslyShown { Start = startTime.AddDays(-1).ToString("yyyyMMddHHmmss 0000") },
            Premiere = new XmltvText { Text = "Series Premiere" },
            LastChance = new XmltvText { Text = "Season Finale" },
            New = "yes",
            Rating = [new XmltvRating { Value = "TV-14", System = "MPAA" }],
            StarRating = [new XmltvRating { Value = "4/5" }]
        });

        var config = new VideoStreamConfig
        {
            EPGId = "source1",
            OutputProfile = new OutputProfileDto { Id = "target1" }
        };

        // Act
        var (channels, programs) = _builder.ProcessXML(inputXml, [config]);

        // Assert
        channels.Count.ShouldBe(1);
        programs.Count.ShouldBe(1);

        var program = programs[0];
        program.Channel.ShouldBe("target1");
        program.Start.ShouldBe(startTime.ToString("yyyyMMddHHmmss 0000"));
        program.Stop.ShouldBe(stopTime.ToString("yyyyMMddHHmmss 0000"));
        program.Titles.ShouldHaveSingleItem().Text.ShouldBe("Program Title");
        program.SubTitles.ShouldHaveSingleItem().Text.ShouldBe("Subtitle");
        program.Descriptions.ShouldHaveSingleItem().Text.ShouldBe("Description");
        program.Credits.ShouldNotBeNull();
        program.Credits.Directors.ShouldHaveSingleItem().ShouldBe("Director Name");
        program.Credits.Actors.ShouldHaveSingleItem().Actor.ShouldBe("Actor Name");
        program.Date.ShouldBe("2025");
        program.Categories.ShouldHaveSingleItem().Text.ShouldBe("Category");
        program.Keywords.ShouldHaveSingleItem().Text.ShouldBe("Keyword");
        program.Language.Text.ShouldBe("en");
        program.Length.Text.ShouldBe("60");
        program.Icons.ShouldHaveSingleItem().Src.ShouldBe("program-logo.png");
        program.Countries.ShouldHaveSingleItem().Text.ShouldBe("USA");
        program.EpisodeNums.ShouldHaveSingleItem().Text.ShouldBe("S01E01");
        program.Video.Present.ShouldBe("yes");
        program.Audio.Present.ShouldBe("no");
        program.PreviouslyShown.Start.ShouldBe(startTime.AddDays(-1).ToString("yyyyMMddHHmmss 0000"));
        program.Premiere.Text.ShouldBe("Series Premiere");
        program.LastChance.Text.ShouldBe("Season Finale");
        program.New.ShouldBe("yes");
        program.Rating.ShouldHaveSingleItem().Value.ShouldBe("TV-14");
        program.StarRating.ShouldHaveSingleItem().Value.ShouldBe("4/5");
    }

    [Fact]
    public void ProcessXML_WithEPGFallback_UsesEPGThumbWhenNoLogoSet()
    {
        // Arrange
        var inputXml = XMLUtil.NewXMLTV;

        //Simulate that nfo is populated with a value
        string epgLogo = "epg-logo.png";
        _customPlayListBuilderMock
               .Setup(x => x.GetCustomPlayListLogoFromFileName(It.IsAny<string>()))
               .Returns(epgLogo);

        inputXml.Channels.Add(new XmltvChannel
        {
            Id = "source1",
            DisplayNames = [new XmltvText { Text = "Source Channel" }]
            // No Icons set
        });

        var config = new VideoStreamConfig
        {
            EPGId = "source1",
            OutputProfile = new OutputProfileDto { Id = "target1" },
            // No Logo or OGLogo set
        };

        // Act
        var (channels, _) = _builder.ProcessXML(inputXml, [config]);

        // Assert
        channels.ShouldHaveSingleItem();
        channels[0].Icons.ShouldNotBeNull();
        channels[0].Icons.ShouldHaveSingleItem();
        channels[0].Icons[0].Src.ShouldBe(epgLogo);
    }

    [Fact]
    public void ProcessXML_WithEPGSourceUpdated_UpdatesChannelLogo()
    {
        // Arrange
        var inputXml = XMLUtil.NewXMLTV;
        inputXml.Channels.Add(new XmltvChannel
        {
            Id = "source1",
            DisplayNames = [new XmltvText { Text = "Source Channel" }],
        });
        string initialLogo = "epg-logo-v1.png";
        _customPlayListBuilderMock
               .Setup(x => x.GetCustomPlayListLogoFromFileName(It.IsAny<string>()))
               .Returns(initialLogo);
        var config = new VideoStreamConfig
        {
            EPGId = "source1",
            OutputProfile = new OutputProfileDto { Id = "target1" },
            // No Logo or OGLogo set initially
        };
        // Act
        var (channels, _) = _builder.ProcessXML(inputXml, [config]);
        // Assert
        channels.ShouldHaveSingleItem();
        channels[0].Icons.ShouldNotBeNull();
        channels[0].Icons.ShouldHaveSingleItem();
        channels[0].Icons[0].Src.ShouldBe(initialLogo);
        // Simulate EPG source update
        string updatedLogo = "epg-logo-v2.png";
        _customPlayListBuilderMock
              .Setup(x => x.GetCustomPlayListLogoFromFileName(It.IsAny<string>()))
              .Returns(updatedLogo);
        // Act again after the "update"
        var (updatedChannels, _) = _builder.ProcessXML(inputXml, [config]);
        // Assert that the logo has been updated
        updatedChannels.ShouldHaveSingleItem();
        updatedChannels[0].Icons.ShouldNotBeNull();
        updatedChannels[0].Icons.ShouldHaveSingleItem();
        updatedChannels[0].Icons[0].Src.ShouldBe(updatedLogo);
    }
}