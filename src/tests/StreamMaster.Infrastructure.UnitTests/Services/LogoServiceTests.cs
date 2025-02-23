using Shouldly;
using Moq;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using StreamMaster.Domain.Configuration;
using Microsoft.AspNetCore.Http;
using StreamMaster.Domain.Services;
using StreamMaster.SchedulesDirect.Domain.Interfaces;
using StreamMaster.Domain.Models;
using StreamMaster.Domain.Enums;

namespace StreamMaster.Infrastructure.Services.Tests;

public class LogoServiceTests : IDisposable
{
    private readonly LogoService _logoService;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
    private readonly Mock<IOptionsMonitor<Setting>> _settings;
    private readonly Mock<IOptionsMonitor<CustomLogoDict>> _customLogos;
    private readonly Mock<IImageDownloadService> _imageDownloadService;
    private readonly Mock<IContentTypeProvider> _mimeTypeProvider;
    private readonly IMemoryCache _memoryCache;
    private readonly Mock<IImageDownloadQueue> _imageDownloadQueue;
    private readonly Mock<IServiceProvider> _serviceProvider;
    private readonly Mock<IFileUtilService> _fileUtilService;
    private readonly Mock<IDataRefreshService> _dataRefreshService;
    private readonly Mock<ILogger<LogoService>> _logger;
    private readonly Setting _settings_value;
    private readonly CustomLogoDict _customLogos_value;

    public LogoServiceTests()
    {
        _httpContextAccessor = new Mock<IHttpContextAccessor>();
        _settings = new Mock<IOptionsMonitor<Setting>>();
        _customLogos = new Mock<IOptionsMonitor<CustomLogoDict>>();
        _imageDownloadService = new Mock<IImageDownloadService>();
        _mimeTypeProvider = new Mock<IContentTypeProvider>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _imageDownloadQueue = new Mock<IImageDownloadQueue>();
        _serviceProvider = new Mock<IServiceProvider>();
        _fileUtilService = new Mock<IFileUtilService>();
        _dataRefreshService = new Mock<IDataRefreshService>();
        _logger = new Mock<ILogger<LogoService>>();

        _settings_value = new Setting { LogoCache = true };
        _customLogos_value = new CustomLogoDict();

        _settings.Setup(x => x.CurrentValue).Returns(_settings_value);
        _customLogos.Setup(x => x.CurrentValue).Returns(_customLogos_value);

        _logoService = new LogoService(
            _httpContextAccessor.Object,
            _settings.Object,
            _customLogos.Object,
            _imageDownloadService.Object,
            _mimeTypeProvider.Object,
            _memoryCache,
            _imageDownloadQueue.Object,
            _serviceProvider.Object,
            _fileUtilService.Object,
            _dataRefreshService.Object,
            _logger.Object
        );
    }

    public void Dispose()
    {
        _memoryCache.Dispose();
    }

    [Fact]
    public void AddCustomLogo_ValidInput_AddsLogoSuccessfully()
    {
        string name = "TestLogo";
        string source = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=";

        string result = _logoService.AddCustomLogo(name, source);

        result.ShouldNotBeNullOrEmpty();
        var logos = _logoService.GetLogos();
        logos.ShouldNotBeEmpty();
        var addedLogo = logos.FirstOrDefault(l => l.Name == name);
        addedLogo.ShouldNotBeNull();
        addedLogo.Source.ShouldNotBeNull();
    }

    [Fact]
    public void GetLogoUrl_SMChannel_ReturnsCorrectUrl()
    {
        var channel = new SMChannel { Id = 1, Logo = "http://example.com/logo.png" };
        string baseUrl = "http://localhost";

        string result = _logoService.GetLogoUrl(channel, baseUrl);

        result.ShouldStartWith(baseUrl);
        result.ShouldContain(channel.Id.ToString());
    }

    [Fact]
    public void GetContentType_CachedType_ReturnsCachedValue()
    {
        string fileName = "test.png";
        string expectedContentType = "image/png";

        _mimeTypeProvider.Setup(x => x.TryGetContentType(fileName, out expectedContentType))
            .Returns(true);

        string result1 = _logoService.GetContentType(fileName);
        string result2 = _logoService.GetContentType(fileName);

        result1.ShouldBe(expectedContentType);
        result2.ShouldBe(expectedContentType);
        _mimeTypeProvider.Verify(x => x.TryGetContentType(It.IsAny<string>(), out It.Ref<string>.IsAny), Times.Once);
    }

    [Fact]
    public void AddLogoToCache_ValidInput_AddsToCache()
    {
        string source = "http://example.com/logo.png";
        string title = "Test Logo";
        SMFileTypes fileType = SMFileTypes.Logo;

        _logoService.AddLogoToCache(source, title, fileType);

        var logos = _logoService.GetLogos();
        logos.ShouldContain(x => x.Source == source && x.Name == title);
    }

    [Fact]
    public void ClearLogos_RemovesAllLogos()
    {
        _logoService.AddLogoToCache("test1", "Test 1", SMFileTypes.Logo);
        _logoService.AddLogoToCache("test2", "Test 2", SMFileTypes.Logo);

        _logoService.ClearLogos();

        _logoService.GetLogos().Count.ShouldBe(0);
    }
}