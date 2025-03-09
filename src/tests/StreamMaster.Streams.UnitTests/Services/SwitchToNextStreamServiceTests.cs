using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using StreamMaster.Application.Interfaces;
using StreamMaster.Domain.Configuration;
using StreamMaster.Domain.Dto;
using StreamMaster.Domain.Enums;
using StreamMaster.Domain.Models;
using StreamMaster.Domain.Repository;
using StreamMaster.Domain.Services;
using StreamMaster.PlayList;
using StreamMaster.PlayList.Models;
using StreamMaster.Streams.Broadcasters;
using StreamMaster.Streams.Domain.Interfaces;
using StreamMaster.Streams.Services;

namespace StreamMaster.Streams.UnitTests.Services;

public class SwitchToNextStreamServiceTests
{
    private class TestFixture
    {
        public Mock<ILogger<SwitchToNextStreamService>> LoggerMock { get; } = new();
        public Mock<ICacheManager> CacheManagerMock { get; } = new();
        public Mock<IStreamLimitsService> StreamLimitsServiceMock { get; } = new();
        public Mock<IProfileService> ProfileServiceMock { get; } = new();
        public Mock<IServiceProvider> ServiceProviderMock { get; } = new();
        public Mock<IIntroPlayListBuilder> IntroPlayListBuilderMock { get; } = new();
        public Mock<ICustomPlayListBuilder> CustomPlayListBuilderMock { get; } = new();
        public Mock<IStreamConnectionService> StreamConnectionServiceMock { get; } = new();
        public Mock<IOptionsMonitor<Setting>> SettingsMonitorMock { get; } = new();
        public Mock<IServiceScope> ServiceScopeMock { get; } = new();
        public Mock<IServiceProvider> ScopeServiceProviderMock { get; } = new();
        public Mock<IRepositoryWrapper> RepositoryMock { get; } = new();
        public Mock<IServiceScopeFactory> ServiceScopeFactoryMock { get; } = new();
        public Mock<IStreamGroupService> StreamGroupServiceMock { get; } = new();
        public SwitchToNextStreamService Service { get; }

        public TestFixture()
        {
            // Setup default settings
            SettingsMonitorMock.Setup(x => x.CurrentValue).Returns(new Setting
            {
                ShowIntros = "None",
                ClientUserAgent = "TestUserAgent"
            });

            // Setup service scope factory
            ServiceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(ServiceScopeMock.Object);
            ServiceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
                .Returns(ServiceScopeFactoryMock.Object);

            // Setup service scope
            ServiceScopeMock.Setup(x => x.ServiceProvider).Returns(ScopeServiceProviderMock.Object);

            // Setup repository in scope
            RepositoryMock.Setup(x => x.SMStream).Returns(new Mock<ISMStreamRepository>().Object);
            ScopeServiceProviderMock.Setup(x => x.GetService(typeof(IRepositoryWrapper)))
                .Returns(RepositoryMock.Object);

            // Setup stream group service
            StreamGroupServiceMock = new Mock<IStreamGroupService>();
            ScopeServiceProviderMock.Setup(x => x.GetService(typeof(IStreamGroupService)))
                .Returns(StreamGroupServiceMock.Object);

            Service = new SwitchToNextStreamService(
                LoggerMock.Object,
                CacheManagerMock.Object,
                StreamLimitsServiceMock.Object,
                ProfileServiceMock.Object,
                ServiceProviderMock.Object,
                IntroPlayListBuilderMock.Object,
                CustomPlayListBuilderMock.Object,
                SettingsMonitorMock.Object
            );
        }

        public IStreamStatus CreateChannelStatus(bool isFirst = false, int currentRank = 0)
        {
            var loggerMock = new Mock<ILogger<IChannelBroadcaster>>();
            var channel = new SMChannelDto
            {
                Id = 1,
                Name = "TestChannel",
                CurrentRank = currentRank,
                CommandProfileName = "DefaultProfile",
                SMStreamDtos = new List<SMStreamDto>
                {
                    new()
                    {
                        Id = "stream1",
                        Name = "TestStream",
                        Url = "http://test.com/stream",
                        SMStreamType = SMStreamTypeEnum.Regular,
                        Rank = 0
                    }
                }
            };

            var broadcaster = new ChannelBroadcaster(
                loggerMock.Object,
                SettingsMonitorMock.Object,
                channel,
                0
            );

            broadcaster.IsFirst = isFirst;

            return broadcaster;
        }

        public void SetupCommandProfile(string profileName = "SMFFMPEGLocal")
        {
            ProfileServiceMock.Setup(x => x.GetCommandProfile(profileName))
                .Returns(new CommandProfileDto { ProfileName = profileName });
        }

        public void SetupStreamGroupProfile(CommandProfileDto profile)
        {
            StreamGroupServiceMock.Setup(x => x.GetProfileFromSGIdsCommandProfileNameAsync(
                It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(profile);
        }

        public void AddStreamToChannel(IStreamStatus status, string id, string name, SMStreamTypeEnum type, int rank)
        {
            status.SMChannel.SMStreamDtos.Add(new SMStreamDto
            {
                Id = id,
                Name = name,
                Url = $"http://test.com/{id}",
                SMStreamType = type,
                Rank = rank
            });
        }
    }

    [Fact]
    public async Task IntroEnabled_FirstTime_ShouldPlayIntro()
    {
        // Arrange
        var fixture = new TestFixture();
        var status = fixture.CreateChannelStatus(isFirst: true);

        fixture.SettingsMonitorMock.Setup(x => x.CurrentValue).Returns(new Setting
        {
            ShowIntros = "Once",
            ClientUserAgent = "TestUserAgent"
        });

        var introNfo = new CustomStreamNfo
        {
            Movie = new Movie { Title = "TestIntro" },
            VideoFileName = "test.mp4"
        };
        fixture.IntroPlayListBuilderMock.Setup(x => x.GetRandomIntro(It.IsAny<int?>()))
            .Returns(introNfo);

        fixture.SetupCommandProfile();

        // Act
        bool result = await fixture.Service.SetNextStreamAsync(status);

        // Assert
        result.ShouldBeTrue();
        status.IsFirst.ShouldBeFalse();
        status.PlayedIntro.ShouldBeTrue();
        status.SMStreamInfo.SMStreamType.ShouldBe(SMStreamTypeEnum.Intro);
    }

    [Fact]
    public async Task NoStreams_ShouldReturnFalse()
    {
        // Arrange
        var fixture = new TestFixture();
        var status = fixture.CreateChannelStatus();
        status.SMChannel.SMStreamDtos.Clear();

        // Act
        bool result = await fixture.Service.SetNextStreamAsync(status);

        // Assert
        result.ShouldBeFalse();
        status.SMStreamInfo.ShouldBeNull();
    }

    [Fact]
    public async Task LimitedStream_MessageVideosEnabled_ShouldShowMessage()
    {
        // Arrange
        var fixture = new TestFixture();
        var status = fixture.CreateChannelStatus();

        fixture.StreamLimitsServiceMock.Setup(x => x.IsLimited(It.IsAny<SMStreamDto>()))
            .Returns(true);

        fixture.SettingsMonitorMock.Setup(x => x.CurrentValue).Returns(new Setting
        {
            ShowMessageVideos = true,
            ClientUserAgent = "TestUserAgent"
        });

        fixture.CacheManagerMock.SetupGet(x => x.MessageNoStreamsLeft).Returns(new SMStreamInfo
        {
            Id = "message",
            Name = "No Streams Available",
            CommandProfile = new CommandProfileDto(),
            SMStreamType = SMStreamTypeEnum.Regular,
            Url = "https://example.com/1.ts"
        });

        // Act
        bool result = await fixture.Service.SetNextStreamAsync(status);

        // Assert
        result.ShouldBeTrue();
        status.SMStreamInfo.Id.ShouldBe("message");
    }

    [Fact]
    public async Task CustomPlayList_ShouldSetupCorrectly()
    {
        // Arrange
        var fixture = new TestFixture();
        var status = fixture.CreateChannelStatus();
        status.SMChannel.SMStreamDtos[0].SMStreamType = SMStreamTypeEnum.Movie;

        var customPlayList = new CustomPlayList
        {
            Name = "TestPlaylist",
            CustomStreamNfos = new List<CustomStreamNfo>
            {
                new() { Movie = new Movie { Title = "TestMovie" }, VideoFileName = "test.mp4" }
            }
        };

        fixture.CustomPlayListBuilderMock.Setup(x => x.GetCustomPlayList(It.IsAny<string>()))
            .Returns(customPlayList);

        fixture.CustomPlayListBuilderMock.Setup(x => x.GetCurrentVideoAndElapsedSeconds(It.IsAny<string>()))
            .Returns((new CustomStreamNfo
            {
                Movie = new Movie { Title = "TestMovie" },
                VideoFileName = "test.mp4"
            }, 0));

        fixture.SetupCommandProfile();

        // Act
        bool result = await fixture.Service.SetNextStreamAsync(status);

        // Assert
        result.ShouldBeTrue();
        status.SMStreamInfo.SMStreamType.ShouldBe(SMStreamTypeEnum.Movie);
        status.SMStreamInfo.Name.ShouldBe("TestMovie");
    }

    [Fact]
    public async Task OverrideStreamId_ShouldUseSpecifiedStream()
    {
        // Arrange
        var fixture = new TestFixture();
        var status = fixture.CreateChannelStatus();
        var overrideStreamId = "override-stream-id";
        var overrideStream = new SMStreamDto
        {
            Id = overrideStreamId,
            Name = "Override Stream",
            Url = "http://test.com/override",
            SMStreamType = SMStreamTypeEnum.Regular
        };

        // Setup repository to return the override stream
        var smStreamRepository = new Mock<ISMStreamRepository>();
        smStreamRepository.Setup(x => x.GetSMStreamAsync(overrideStreamId))
            .ReturnsAsync(overrideStream);
        fixture.RepositoryMock.Setup(x => x.SMStream).Returns(smStreamRepository.Object);

        // Setup stream group service
        fixture.StreamGroupServiceMock.Setup(x => x.GetProfileFromSGIdsCommandProfileNameAsync(
            It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(new CommandProfileDto { ProfileName = "TestProfile" });

        // Act
        bool result = await fixture.Service.SetNextStreamAsync(status, overrideStreamId);

        // Assert
        result.ShouldBeTrue();
        status.SMStreamInfo.Id.ShouldBe(overrideStreamId);
        status.SMStreamInfo.Name.ShouldBe("Override Stream");
    }

    [Fact]
    public async Task IntroStream_ShouldSetupIntroPlaylist()
    {
        // Arrange
        var fixture = new TestFixture();
        var status = fixture.CreateChannelStatus();
        status.SMChannel.SMStreamDtos[0].SMStreamType = SMStreamTypeEnum.Intro;

        var introPlayList = new CustomPlayList
        {
            Name = "IntroPlaylist",
            CustomStreamNfos = new List<CustomStreamNfo>
            {
                new() { Movie = new Movie { Title = "IntroVideo" }, VideoFileName = "intro.mp4" }
            }
        };

        fixture.IntroPlayListBuilderMock.Setup(x => x.GetIntroPlayList(It.IsAny<string>()))
            .Returns(introPlayList);

        fixture.SetupCommandProfile();

        // Act
        bool result = await fixture.Service.SetNextStreamAsync(status);

        // Assert
        result.ShouldBeTrue();
        status.SMStreamInfo.SMStreamType.ShouldBe(SMStreamTypeEnum.Movie);
        status.SMStreamInfo.Name.ShouldBe("IntroPlaylist");
    }

    [Fact]
    public async Task StandardStream_ShouldSetupCorrectly()
    {
        // Arrange
        var fixture = new TestFixture();
        var status = fixture.CreateChannelStatus();
        var commandProfile = new CommandProfileDto { ProfileName = "StandardProfile" };

        fixture.SetupStreamGroupProfile(commandProfile);

        // Act
        bool result = await fixture.Service.SetNextStreamAsync(status);

        // Assert
        result.ShouldBeTrue();
        status.SMStreamInfo.SMStreamType.ShouldBe(SMStreamTypeEnum.Regular);
        status.SMStreamInfo.CommandProfile.ShouldBe(commandProfile);
    }

    [Fact]
    public async Task AllStreamsLimited_NoMessageVideos_ShouldReturnFalse()
    {
        // Arrange
        var fixture = new TestFixture();
        var status = fixture.CreateChannelStatus();

        fixture.StreamLimitsServiceMock.Setup(x => x.IsLimited(It.IsAny<SMStreamDto>()))
            .Returns(true);

        fixture.SettingsMonitorMock.Setup(x => x.CurrentValue).Returns(new Setting
        {
            ShowMessageVideos = false,
            ClientUserAgent = "TestUserAgent"
        });

        // Act
        bool result = await fixture.Service.SetNextStreamAsync(status);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task MultipleStreams_ShouldRotateToNextStream()
    {
        // Arrange
        var fixture = new TestFixture();
        var status = fixture.CreateChannelStatus(currentRank: 0);

        // Add a second stream
        fixture.AddStreamToChannel(status, "stream2", "TestStream2", SMStreamTypeEnum.Regular, 1);

        var commandProfile = new CommandProfileDto { ProfileName = "StandardProfile" };
        fixture.SetupStreamGroupProfile(commandProfile);

        // Act
        bool result = await fixture.Service.SetNextStreamAsync(status);

        // Assert
        result.ShouldBeTrue();
        status.SMStreamInfo.Id.ShouldBe("stream2");
    }

    [Fact]
    public async Task RotationCompleted_ShouldWrapAroundToFirstStream()
    {
        // Arrange
        var fixture = new TestFixture();
        var status = fixture.CreateChannelStatus(currentRank: 2);

        // Add multiple streams
        fixture.AddStreamToChannel(status, "stream2", "TestStream2", SMStreamTypeEnum.Regular, 1);
        fixture.AddStreamToChannel(status, "stream3", "TestStream3", SMStreamTypeEnum.Regular, 2);

        var commandProfile = new CommandProfileDto { ProfileName = "StandardProfile" };
        fixture.SetupStreamGroupProfile(commandProfile);

        // Act
        bool result = await fixture.Service.SetNextStreamAsync(status);

        // Assert
        result.ShouldBeTrue();
        status.SMStreamInfo.Id.ShouldBe("stream1"); // Should wrap around to first stream
    }

    [Fact]
    public async Task SomeStreamsLimited_ShouldSkipLimitedStreams()
    {
        // Arrange
        var fixture = new TestFixture();
        var status = fixture.CreateChannelStatus(currentRank: 0);

        // Add multiple streams
        fixture.AddStreamToChannel(status, "stream2", "TestStream2", SMStreamTypeEnum.Regular, 1);
        fixture.AddStreamToChannel(status, "stream3", "TestStream3", SMStreamTypeEnum.Regular, 2);

        // Set M3UFileId for streams
        status.SMChannel.SMStreamDtos[0].M3UFileId = 1;
        status.SMChannel.SMStreamDtos[1].M3UFileId = 2;
        status.SMChannel.SMStreamDtos[2].M3UFileId = 3;

        // Setup stream limits to limit the second stream only
        fixture.StreamLimitsServiceMock.Setup(x => x.IsLimited(It.Is<SMStreamDto>(s => s.Id == "stream2")))
            .Returns(true);
        fixture.StreamLimitsServiceMock.Setup(x => x.IsLimited(It.Is<SMStreamDto>(s => s.Id != "stream2")))
            .Returns(false);

        var commandProfile = new CommandProfileDto { ProfileName = "StandardProfile" };
        fixture.SetupStreamGroupProfile(commandProfile);

        // Act
        bool result = await fixture.Service.SetNextStreamAsync(status);

        // Assert
        result.ShouldBeTrue();
        status.SMStreamInfo.Id.ShouldBe("stream3"); // Should skip stream2 and go to stream3
    }

    [Fact]
    public async Task StreamSpecificUserAgent_ShouldBeUsed()
    {
        // Arrange
        var fixture = new TestFixture();
        var status = fixture.CreateChannelStatus();
        status.SMChannel.SMStreamDtos[0].ClientUserAgent = "StreamSpecificUserAgent";

        var commandProfile = new CommandProfileDto { ProfileName = "StandardProfile" };
        fixture.SetupStreamGroupProfile(commandProfile);

        // Act
        bool result = await fixture.Service.SetNextStreamAsync(status);

        // Assert
        result.ShouldBeTrue();
        status.SMStreamInfo.ClientUserAgent.ShouldBe("StreamSpecificUserAgent");
    }
}