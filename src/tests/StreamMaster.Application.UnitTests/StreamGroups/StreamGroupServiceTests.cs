using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MockQueryable;
using Moq;
using Shouldly;
using StreamMaster.Application.StreamGroups;
using StreamMaster.Domain.Configuration;
using StreamMaster.Domain.Models;
using StreamMaster.Domain.Repository;
using StreamMaster.Domain.Services;
using StreamMaster.Streams.Domain.Interfaces;

namespace StreamMaster.Application.UnitTests.StreamGroups;

public partial class StreamGroupServiceTests : IDisposable
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
    private readonly Mock<ILogoService> _logoService;
    private readonly Mock<IOptionsMonitor<Setting>> _settings;
    private readonly Mock<IOptionsMonitor<CommandProfileDict>> _commandProfileSettings;
    private readonly Mock<ICacheManager> _cacheManager;
    private readonly Mock<IRepositoryWrapper> _repositoryWrapper;
    private readonly Mock<IProfileService> _profileService;
    private readonly StreamGroupService _streamGroupService;
    private readonly Setting _settingsValue;
    private readonly CommandProfileDict _commandProfileDictValue;
    private readonly MemoryCache _memoryCache;

    public StreamGroupServiceTests()
    {
        _httpContextAccessor = new Mock<IHttpContextAccessor>();
        _logoService = new Mock<ILogoService>();
        _settings = new Mock<IOptionsMonitor<Setting>>();
        _commandProfileSettings = new Mock<IOptionsMonitor<CommandProfileDict>>();
        _cacheManager = new Mock<ICacheManager>();
        _repositoryWrapper = new Mock<IRepositoryWrapper>();
        _profileService = new Mock<IProfileService>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());

        _settingsValue = new Setting
        {
            ServerKey = "TestServerKey123", // Use a consistent key for encryption/decryption
            LogoCache = true,
            STRMBaseURL = "http://localhost",
            DefaultCommandProfileName = "DefaultCommandProfile"
        };

        _commandProfileDictValue = new CommandProfileDict
        {
            CommandProfiles = new Dictionary<string, CommandProfile>
            {
                { "Default", new CommandProfile { Command = "DefaultCommand" } },
                { "CustomProfile", new CommandProfile { Command = "CustomProfileCommand" } },
                { "DefaultCommandProfile", new CommandProfile { Command = "DefaultCommandProfileCommand" } }
            }
        };

        _settings.Setup(x => x.CurrentValue).Returns(_settingsValue);
        _commandProfileSettings.Setup(x => x.CurrentValue).Returns(_commandProfileDictValue);

        _streamGroupService = new StreamGroupService(
            _httpContextAccessor.Object,
            _logoService.Object,
            _settings.Object,
            _commandProfileSettings.Object,
            _cacheManager.Object,
            _repositoryWrapper.Object,
            _profileService.Object
        );
    }

    public void Dispose()
    {
        _memoryCache.Dispose();
    }

    internal StreamGroupService GetStreamGroupService(
        IHttpContextAccessor? httpContextAccessor = null,
        ILogoService? logoService = null,
        IOptionsMonitor<Setting>? settings = null,
        IOptionsMonitor<CommandProfileDict>? commandProfileSettings = null,
        ICacheManager? cacheManager = null,
        IRepositoryWrapper? repositoryWrapper = null,
        IProfileService? profileService = null)
    {
        // Provide default mock instances if any of the dependencies are not explicitly passed in.
        httpContextAccessor ??= Mock.Of<IHttpContextAccessor>();
        logoService ??= Mock.Of<ILogoService>();
        settings ??= Mock.Of<IOptionsMonitor<Setting>>();
        commandProfileSettings ??= Mock.Of<IOptionsMonitor<CommandProfileDict>>();
        cacheManager ??= Mock.Of<ICacheManager>();
        repositoryWrapper ??= Mock.Of<IRepositoryWrapper>();
        profileService ??= Mock.Of<IProfileService>();

        return new StreamGroupService(
            httpContextAccessor,
            logoService,
            settings,
            commandProfileSettings,
            cacheManager,
            repositoryWrapper,
            profileService
        );
    }

    [Fact]
    public async Task GetStreamGroupIdFromSGProfileIdAsync_ValidProfileId_ReturnsStreamGroupId()
    {
        // Arrange
        int streamGroupProfileId = 1;
        int streamGroupId = 2;
        var streamGroup = new StreamGroup { Id = streamGroupId, Name = "TestGroup" };
        var mockRepositoryWrapper = new Mock<IRepositoryWrapper>();

        mockRepositoryWrapper.Setup(x => x.StreamGroupProfile.GetQuery(It.IsAny<bool>()))
            .Returns(new List<StreamGroupProfile>
            {
                new StreamGroupProfile { Id = streamGroupProfileId, StreamGroupId = streamGroupId }
            }.AsQueryable());

        mockRepositoryWrapper.Setup(x => x.StreamGroup.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StreamGroup, bool>>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(streamGroup);

        var mockStreamGroupService = GetStreamGroupService(repositoryWrapper: mockRepositoryWrapper.Object);

        // Act
        int result = await mockStreamGroupService.GetStreamGroupIdFromSGProfileIdAsync(streamGroupProfileId);

        // Assert
        result.ShouldBe(streamGroupId);
    }

    [Fact]
    public async Task GetStreamGroupIdFromSGProfileIdAsync_InvalidProfileId_ReturnsDefaultStreamGroupId()
    {
        // Arrange
        int? streamGroupProfileId = null;
        int defaultStreamGroupId = 1;
        var defaultStreamGroup = new StreamGroup { Id = defaultStreamGroupId, Name = "DefaultStreamGroup" };

        var mockCacheManager = new Mock<ICacheManager>();
        mockCacheManager.Setup(x => x.DefaultSG).Returns(defaultStreamGroup);

        var mockStreamGroupService = GetStreamGroupService(cacheManager: mockCacheManager.Object);
        // Act
        int result = await mockStreamGroupService.GetStreamGroupIdFromSGProfileIdAsync(streamGroupProfileId);

        // Assert
        result.ShouldBe(defaultStreamGroupId);
    }

    [Fact]
    public async Task GetDefaultSGAsync_CachedDefaultSG_ReturnsCachedValue()
    {
        // Arrange
        var defaultStreamGroup = new StreamGroup { Id = 1, Name = "DefaultStreamGroup" };
        var mockCacheManager = new Mock<ICacheManager>();
        mockCacheManager.Setup(x => x.DefaultSG).Returns(defaultStreamGroup);

        var mockRepositoryWrapper = new Mock<IRepositoryWrapper>();

        var mockStreamGroupService = GetStreamGroupService(cacheManager: mockCacheManager.Object, repositoryWrapper: mockRepositoryWrapper.Object);
        // Act
        var result = await mockStreamGroupService.GetDefaultSGAsync();

        // Assert
        result.ShouldBe(defaultStreamGroup);
        mockRepositoryWrapper.Verify(x => x.StreamGroup.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StreamGroup, bool>>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetDefaultSGAsync_NoCachedDefaultSG_FetchesAndCachesDefaultSG()
    {
        // Arrange
        var defaultStreamGroup = new StreamGroup { Id = 1, Name = "DefaultStreamGroup" };

        var mockCacheManager = new Mock<ICacheManager>();
        mockCacheManager.Setup(x => x.DefaultSG).Returns((StreamGroup?)null);

        var mockRepositoryWrapper = new Mock<IRepositoryWrapper>();
        var mockStreamGroupRepository = new Mock<IStreamGroupRepository>();

        mockStreamGroupRepository
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<StreamGroup, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultStreamGroup);

        mockRepositoryWrapper.Setup(x => x.StreamGroup).Returns(mockStreamGroupRepository.Object);

        var streamGroupService = new StreamGroupService(
            _httpContextAccessor.Object,
            _logoService.Object,
            _settings.Object,
            _commandProfileSettings.Object,
            mockCacheManager.Object,
            mockRepositoryWrapper.Object,
            _profileService.Object
        );

        // Act
        var result = await streamGroupService.GetDefaultSGAsync();

        // Assert
        result.ShouldBe(defaultStreamGroup);
        mockCacheManager.VerifySet(x => x.DefaultSG = defaultStreamGroup, Times.Once);
    }

    [Fact]
    public async Task GetDefaultSGAsync_DefaultSGNotFound_ThrowsException()
    {
        // Arrange
        var mockCacheManager = new Mock<ICacheManager>();
        mockCacheManager.Setup(x => x.DefaultSG).Returns((StreamGroup?)null);
        var mockRepositoryWrapper = new Mock<IRepositoryWrapper>();
        mockRepositoryWrapper.Setup(x => x.StreamGroup.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StreamGroup, bool>>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StreamGroup?)null);

        var mockStreamGroupService = GetStreamGroupService(cacheManager: mockCacheManager.Object, repositoryWrapper: mockRepositoryWrapper.Object);

        // Act & Assert
        await Should.ThrowAsync<Exception>(async () => await mockStreamGroupService.GetDefaultSGAsync());
    }

    [Fact]
    public async Task StreamGroupExistsAsync_ValidProfileId_ReturnsTrue()
    {
        // Arrange
        int streamGroupProfileId = 1;
        int streamGroupId = 2;

        var streamGroupProfile = new StreamGroupProfile { Id = streamGroupProfileId, StreamGroupId = streamGroupId };
        var mockRepositoryWrapper = new Mock<IRepositoryWrapper>();

        mockRepositoryWrapper.Setup(x => x.StreamGroupProfile.FirstOrDefaultAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<StreamGroupProfile, bool>>>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(streamGroupProfile);

        var streamGroups = new List<StreamGroup> { new StreamGroup { Id = streamGroupId } };
        var mockStreamGroups = streamGroups.AsQueryable().BuildMock();

        mockRepositoryWrapper.Setup(x => x.StreamGroup.GetQuery(It.IsAny<bool>()))
            .Returns(mockStreamGroups);

        var mockStreamGroupService = GetStreamGroupService(repositoryWrapper: mockRepositoryWrapper.Object);

        // Act
        bool result = await mockStreamGroupService.StreamGroupExistsAsync(streamGroupProfileId);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task StreamGroupExistsAsync_InvalidProfileId_ReturnsFalse()
    {
        // Arrange
        int streamGroupProfileId = 1;
        var mockRepositoryWrapper = new Mock<IRepositoryWrapper>();

        mockRepositoryWrapper.Setup(x => x.StreamGroupProfile.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StreamGroupProfile, bool>>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StreamGroupProfile?)null);

        var mockStreamGroupService = GetStreamGroupService(repositoryWrapper: mockRepositoryWrapper.Object);

        // Act
        bool result = await mockStreamGroupService.StreamGroupExistsAsync(streamGroupProfileId);

        // Assert
        result.ShouldBeFalse();
    }
}