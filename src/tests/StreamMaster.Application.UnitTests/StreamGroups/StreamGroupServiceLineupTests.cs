using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MockQueryable;
using Moq;
using Shouldly;
using StreamMaster.Application.StreamGroups;
using StreamMaster.Domain.Common;
using StreamMaster.Domain.Configuration;
using StreamMaster.Domain.Models;
using StreamMaster.Domain.Repository;
using StreamMaster.Domain.Services;
using StreamMaster.Domain.XmltvXml;
using StreamMaster.Streams.Domain.Interfaces;
using System.Collections.Concurrent;
using System.Text.Json;

namespace StreamMaster.Application.UnitTests.StreamGroups;

public partial class StreamGroupServiceTests
{
    [Fact]
    public async Task GetStreamGroupLineupAsync_ValidInput_ReturnsLineupJson()
    {
        // Arrange
        int streamGroupProfileId = 1;
        var streamGroup = new StreamGroup { Id = 2, Name = "TestGroup", GroupKey = "testkey" };
        var streamGroupProfile = new StreamGroupProfile { Id = streamGroupProfileId, StreamGroupId = streamGroup.Id, CommandProfileName = "Default", OutputProfileName = "Default" };
        var defaultStreamGroup = new StreamGroup { Id = 1, Name = BuildInfo.DefaultStreamGroupName, GroupKey = "defaultkey" };
        var defaultStreamGroupProfile = new StreamGroupProfile { Id = 3, StreamGroupId = defaultStreamGroup.Id, ProfileName = "Default", CommandProfileName = "Default", OutputProfileName = "Default" };

        // Mock repository wrapper
        var mockRepositoryWrapper = new Mock<IRepositoryWrapper>();

        // Setup StreamGroupProfile using MockQueryable
        var streamGroupProfiles = new List<StreamGroupProfile> { streamGroupProfile, defaultStreamGroupProfile };
        var mockStreamGroupProfileQueryable = streamGroupProfiles.AsQueryable().BuildMock();

        mockRepositoryWrapper.Setup(x => x.StreamGroupProfile.GetQuery(It.IsAny<bool>()))
            .Returns(mockStreamGroupProfileQueryable);

        mockRepositoryWrapper.Setup(x => x.StreamGroupProfile.FirstOrDefaultAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<StreamGroupProfile, bool>>>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<StreamGroupProfile, bool>> predicate, bool tracking, CancellationToken token) =>
                streamGroupProfiles.FirstOrDefault(predicate.Compile()));

        // Setup StreamGroup using MockQueryable
        var streamGroups = new List<StreamGroup> { streamGroup, defaultStreamGroup };
        var mockStreamGroupQueryable = streamGroups.AsQueryable().BuildMock();

        mockRepositoryWrapper.Setup(x => x.StreamGroup.GetQuery(It.IsAny<bool>()))
            .Returns(mockStreamGroupQueryable);

        mockRepositoryWrapper.Setup(x => x.StreamGroup.FirstOrDefaultAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<StreamGroup, bool>>>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<StreamGroup, bool>> predicate, bool tracking, CancellationToken token) =>
                streamGroups.FirstOrDefault(predicate.Compile()));

        // Setup SMChannel
        var smChannels = new List<SMChannel>
    {
        new SMChannel { Id = 3, Name = "Test Channel", ChannelNumber = 1, EPGId = "test-epg", IsHidden = false }
    };
        var mockSmChannelQueryable = smChannels.AsQueryable().BuildMock();

        mockRepositoryWrapper.Setup(x => x.SMChannel.GetQuery(It.IsAny<bool>()))
            .Returns(mockSmChannelQueryable);

        mockRepositoryWrapper.Setup(x => x.SMChannel.GetSMChannelsFromStreamGroup(streamGroup.Id))
            .ReturnsAsync(smChannels);

        // Mock HttpContextAccessor
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockHttpContext = new Mock<HttpContext>();
        var mockHttpRequest = new Mock<HttpRequest>();

        mockHttpRequest.Setup(x => x.Scheme).Returns("http");
        mockHttpRequest.Setup(x => x.Host).Returns(new HostString("localhost"));
        mockHttpRequest.Setup(x => x.PathBase).Returns(new PathString(""));

        mockHttpContext.Setup(x => x.Request).Returns(mockHttpRequest.Object);
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        // Mock other required services
        var mockLogoService = new Mock<ILogoService>();
        mockLogoService.Setup(x => x.GetLogoUrl(It.IsAny<SMChannel>(), It.IsAny<string>()))
            .Returns("http://localhost/logo");

        var mockSettings = new Mock<IOptionsMonitor<Setting>>();
        mockSettings.Setup(x => x.CurrentValue).Returns(new Setting { ServerKey = "testserverkey" });

        var mockCommandProfileSettings = new Mock<IOptionsMonitor<CommandProfileDict>>();
        mockCommandProfileSettings.Setup(x => x.CurrentValue).Returns(new CommandProfileDict());

        var mockCacheManager = new Mock<ICacheManager>();
        mockCacheManager.Setup(x => x.StreamGroupKeyCache).Returns(new ConcurrentDictionary<int, string?>());
        mockCacheManager.Setup(x => x.StationChannelNames).Returns(new ConcurrentDictionary<int, List<StationChannelName>>());

        // Setup DefaultSG in cache manager
        mockCacheManager.Setup(x => x.DefaultSG).Returns(defaultStreamGroup);

        var mockProfileService = new Mock<IProfileService>();
        mockProfileService.Setup(x => x.GetCommandProfile(It.IsAny<string>()))
            .Returns(new CommandProfileDto());
        mockProfileService.Setup(x => x.GetOutputProfile(It.IsAny<string>()))
            .Returns(new OutputProfileDto());

        // Create service with all dependencies
        var streamGroupService = new StreamGroupService(
            mockHttpContextAccessor.Object,
            mockLogoService.Object,
            mockSettings.Object,
            mockCommandProfileSettings.Object,
            mockCacheManager.Object,
            mockRepositoryWrapper.Object,
            mockProfileService.Object);

        // Act
        string result = await streamGroupService.GetStreamGroupLineupAsync(streamGroupProfileId, true);

        // Assert
        result.ShouldNotBeNull();

        // Assert the content of the JSON
        var lineup = JsonSerializer.Deserialize<List<SGLineup>>(result);
        lineup.ShouldNotBeNull();
        lineup.Count.ShouldBe(1); // Assert that a single entry is in the lineup
        lineup[0].GuideNumber.ShouldBe("1");
        lineup[0].GuideName.ShouldBe("Test Channel");
        lineup[0].URL.ShouldContain("/v/1/3"); // Short URL format with streamGroupProfileId/channelId
    }

    [Fact]
    public async Task GetStreamGroupLineupAsync_EmptyChannels_ReturnsEmptyJson()
    {
        // Arrange
        int streamGroupProfileId = 1;
        var streamGroup = new StreamGroup { Id = 2, Name = "TestGroup", GroupKey = "testkey" };
        var streamGroupProfile = new StreamGroupProfile { Id = streamGroupProfileId, StreamGroupId = streamGroup.Id, CommandProfileName = "Default", OutputProfileName = "Default" };
        var defaultStreamGroup = new StreamGroup { Id = 1, Name = BuildInfo.DefaultStreamGroupName, GroupKey = "defaultkey" };
        var defaultStreamGroupProfile = new StreamGroupProfile { Id = 3, StreamGroupId = defaultStreamGroup.Id, ProfileName = "Default", CommandProfileName = "Default", OutputProfileName = "Default" };

        // Mock repository wrapper
        var mockRepositoryWrapper = new Mock<IRepositoryWrapper>();

        // Setup StreamGroupProfile using MockQueryable
        var streamGroupProfiles = new List<StreamGroupProfile> { streamGroupProfile, defaultStreamGroupProfile };
        var mockStreamGroupProfileQueryable = streamGroupProfiles.AsQueryable().BuildMock();

        mockRepositoryWrapper.Setup(x => x.StreamGroupProfile.GetQuery(It.IsAny<bool>()))
            .Returns(mockStreamGroupProfileQueryable);

        mockRepositoryWrapper.Setup(x => x.StreamGroupProfile.FirstOrDefaultAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<StreamGroupProfile, bool>>>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<StreamGroupProfile, bool>> predicate, bool tracking, CancellationToken token) =>
                streamGroupProfiles.FirstOrDefault(predicate.Compile()));

        // Setup StreamGroup using MockQueryable
        var streamGroups = new List<StreamGroup> { streamGroup, defaultStreamGroup };
        var mockStreamGroupQueryable = streamGroups.AsQueryable().BuildMock();

        mockRepositoryWrapper.Setup(x => x.StreamGroup.GetQuery(It.IsAny<bool>()))
            .Returns(mockStreamGroupQueryable);

        mockRepositoryWrapper.Setup(x => x.StreamGroup.FirstOrDefaultAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<StreamGroup, bool>>>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<StreamGroup, bool>> predicate, bool tracking, CancellationToken token) =>
                streamGroups.FirstOrDefault(predicate.Compile()));

        // Setup empty SMChannel list
        mockRepositoryWrapper.Setup(x => x.SMChannel.GetSMChannelsFromStreamGroup(streamGroup.Id))
            .ReturnsAsync(new List<SMChannel>());

        // Setup empty SMChannel queryable
        var emptySmChannels = new List<SMChannel>();
        var mockSmChannelQueryable = emptySmChannels.AsQueryable().BuildMock();

        mockRepositoryWrapper.Setup(x => x.SMChannel.GetQuery(It.IsAny<bool>()))
            .Returns(mockSmChannelQueryable);

        // Mock HttpContextAccessor
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockHttpContext = new Mock<HttpContext>();
        var mockHttpRequest = new Mock<HttpRequest>();

        mockHttpRequest.Setup(x => x.Scheme).Returns("http");
        mockHttpRequest.Setup(x => x.Host).Returns(new HostString("localhost"));
        mockHttpRequest.Setup(x => x.PathBase).Returns(new PathString(""));

        mockHttpContext.Setup(x => x.Request).Returns(mockHttpRequest.Object);
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        // Mock other required services
        var mockLogoService = new Mock<ILogoService>();
        var mockSettings = new Mock<IOptionsMonitor<Setting>>();
        mockSettings.Setup(x => x.CurrentValue).Returns(new Setting { ServerKey = "testserverkey" });

        var mockCommandProfileSettings = new Mock<IOptionsMonitor<CommandProfileDict>>();
        mockCommandProfileSettings.Setup(x => x.CurrentValue).Returns(new CommandProfileDict());

        var mockCacheManager = new Mock<ICacheManager>();
        mockCacheManager.Setup(x => x.StreamGroupKeyCache).Returns(new ConcurrentDictionary<int, string?>());
        mockCacheManager.Setup(x => x.StationChannelNames).Returns(new ConcurrentDictionary<int, List<StationChannelName>>());

        // Setup DefaultSG in cache manager
        mockCacheManager.Setup(x => x.DefaultSG).Returns(defaultStreamGroup);

        var mockProfileService = new Mock<IProfileService>();
        mockProfileService.Setup(x => x.GetCommandProfile(It.IsAny<string>()))
            .Returns(new CommandProfileDto());
        mockProfileService.Setup(x => x.GetOutputProfile(It.IsAny<string>()))
            .Returns(new OutputProfileDto());

        // Create service with all dependencies
        var streamGroupService = new StreamGroupService(
            mockHttpContextAccessor.Object,
            mockLogoService.Object,
            mockSettings.Object,
            mockCommandProfileSettings.Object,
            mockCacheManager.Object,
            mockRepositoryWrapper.Object,
            mockProfileService.Object);

        // Act
        string result = await streamGroupService.GetStreamGroupLineupAsync(streamGroupProfileId, true);

        // Assert
        result.ShouldBe("[]");
    }
}