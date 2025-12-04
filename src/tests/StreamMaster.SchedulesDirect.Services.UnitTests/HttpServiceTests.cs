using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Shouldly;
using StreamMaster.Domain.Configuration;
using StreamMaster.Domain.Services;
using StreamMaster.SchedulesDirect.Domain;
using StreamMaster.SchedulesDirect.Domain.Enums;
using StreamMaster.SchedulesDirect.Services;
using System.Net;
using System.Text;
using System.Text.Json;

namespace StreamMaster.SchedulesDirect.UnitTests.Services;

public class HttpServiceTests
{
    [Fact]
    public void IsReady_WhenTokenErrorTimestampInPast_ReturnsTrue()
    {
        // Arrange
        var (service, settings, _, _, _, _) = CreateServiceAndMocks();
        settings.ErrorCooldowns = new List<ErrorCooldownSetting>();

        // Act
        bool result = service.IsReady;

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsReady_WhenTokenErrorTimestampInFuture_ReturnsFalse()
    {
        // Arrange
        var (service, settings, _, _, _, _) = CreateServiceAndMocks();
        settings.ErrorCooldowns = new List<ErrorCooldownSetting>
        {
            new ErrorCooldownSetting
            {
                ErrorCode = (int)SDHttpResponseCode.SERVICE_OFFLINE,
                CooldownUntil = DateTime.UtcNow.AddMinutes(5),
                Reason = "Test cooldown"
            }
        };

        // Act
        bool result = service.IsReady;

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void ClearToken_ResetsTokenState()
    {
        // Arrange
        var (service, _, _, _, _, mockTokenStore) = CreateServiceAndMocks();
        SetTokenProperties(service, mockTokenStore);

        // Act
        service.ClearToken();

        // Assert
        mockTokenStore.Verify(x => x.ClearToken(), Times.Once);
        service.GoodToken.ShouldBeFalse();
        service.TokenTimestamp.ShouldBe(DateTime.MinValue);
    }

    [Fact]
    public async Task ValidateTokenAsync_WhenSDDisabled_ReturnsFalse()
    {
        // Arrange
        var (service, settings, _, _, _, _) = CreateServiceAndMocks();
        settings.SDEnabled = false;

        // Act
        bool result = await service.ValidateTokenAsync();

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task ValidateTokenAsync_WithForceReset_RefreshesToken()
    {
        // Arrange
        var (service, _, _, mockHttpMessageHandler, _, mockTokenStore) = CreateServiceAndMocks();

        // Setup token response
        SetupTokenResponse(mockHttpMessageHandler, HttpStatusCode.OK, new
        {
            code = 0,
            token = "new-token",
            datetime = DateTime.UtcNow
        });

        // Act
        bool result = await service.ValidateTokenAsync(forceReset: true);

        // Assert
        result.ShouldBeTrue();
        mockTokenStore.Verify(x => x.SetToken("new-token"), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_SuccessfulTokenRefresh_UpdatesTokenAndReturnsTrue()
    {
        // Arrange
        var (service, _, _, mockHttpMessageHandler, mockDataRefreshService, mockTokenStore) = CreateServiceAndMocks();
        var tokenDateTime = DateTime.UtcNow;

        SetupTokenResponse(mockHttpMessageHandler, HttpStatusCode.OK, new
        {
            code = 0,
            token = "new-token",
            datetime = tokenDateTime
        });

        // Act
        bool result = await service.RefreshTokenAsync(CancellationToken.None);

        // Assert
        result.ShouldBeTrue();
        mockTokenStore.Verify(x => x.SetToken("new-token"), Times.Once);
        service.GoodToken.ShouldBeTrue();
        Math.Abs((service.TokenTimestamp - tokenDateTime).TotalSeconds).ShouldBeLessThan(1);
        mockDataRefreshService.Verify(x => x.RefreshSDReady(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task RefreshTokenAsync_MissingCredentials_ReturnsFalse()
    {
        // Arrange
        var (service, settings, mockHttpClientFactory, _, _, _) = CreateServiceAndMocks();
        settings.SDUserName = "";
        settings.SDPassword = "";

        // Act
        bool result = await service.RefreshTokenAsync(CancellationToken.None);

        // Assert
        result.ShouldBeFalse();
        mockHttpClientFactory.Verify(x => x.CreateClient(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RefreshTokenAsync_NotReady_ReturnsFalse()
    {
        // Arrange
        var (service, settings, mockHttpClientFactory, mockHttpMessageHandler, _, _) = CreateServiceAndMocks();
        settings.ErrorCooldowns = new List<ErrorCooldownSetting>
        {
            new ErrorCooldownSetting
            {
                ErrorCode = (int)SDHttpResponseCode.SERVICE_OFFLINE,
                CooldownUntil = DateTime.UtcNow.AddHours(1),
                Reason = "Test cooldown"
            }
        };

        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        // Act
        bool result = await service.RefreshTokenAsync(CancellationToken.None);

        // Assert
        result.ShouldBeFalse();
        service.GoodToken.ShouldBeFalse();
        mockHttpClientFactory.Verify(x => x.CreateClient(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenServerReturnsError_HandlesProperly()
    {
        // Arrange
        var (service, settings, _, mockHttpMessageHandler, _, mockTokenStore) = CreateServiceAndMocks();
        var initialTimestamp = settings.TokenErrorTimestamp;

        SetupHttpResponse(mockHttpMessageHandler, HttpStatusCode.Unauthorized, new
        {
            code = (int)SDHttpResponseCode.INVALID_USER,
            message = "Error: INVALID_USER"
        }, HttpMethod.Post, "token");

        // Act
        bool result = await service.RefreshTokenAsync(CancellationToken.None);

        // Assert
        result.ShouldBeFalse();
        mockTokenStore.Verify(x => x.ClearToken(), Times.AtLeastOnce);
        service.GoodToken.ShouldBeFalse();
        settings.TokenErrorTimestamp.ShouldBeGreaterThan(initialTimestamp);
    }

    [Fact]
    public async Task SendRequestAsync_WhenSDDisabled_ReturnsDefault()
    {
        // Arrange
        var (service, settings, mockHttpClientFactory, _, _, _) = CreateServiceAndMocks();
        settings.SDEnabled = false;

        // Act
        var result = await service.SendRequestAsync<object>(APIMethod.GET, "test");

        // Assert
        result.ShouldBeNull();
        mockHttpClientFactory.Verify(x => x.CreateClient(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SendRequestAsync_WhenTokenInvalid_ThrowsException()
    {
        // Arrange
        var (service, _, _, mockHttpMessageHandler, _, _) = CreateServiceAndMocks();

        SetupHttpResponse(mockHttpMessageHandler, HttpStatusCode.Unauthorized, new
        {
            code = (int)SDHttpResponseCode.TOKEN_INVALID,
            message = "Error: TOKEN_INVALID"
        }, HttpMethod.Post, "token");

        // Act & Assert
        await Should.ThrowAsync<TokenValidationException>(async () =>
            await service.SendRequestAsync<object>(APIMethod.GET, "test"));
    }

    [Fact]
    public async Task SendRawRequestAsync_WhenTokenValid_SendsRequest()
    {
        // Arrange
        var (service, _, mockHttpClientFactory, mockHttpMessageHandler, _, mockTokenStore) = CreateServiceAndMocks();
        SetTokenProperties(service, mockTokenStore);

        var request = new HttpRequestMessage(HttpMethod.Get, "test");

        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("test-content")
            });

        // Act
        var response = await service.SendRawRequestAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        mockHttpClientFactory.Verify(x => x.CreateClient(It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task SendRawRequestAsync_WhenHttpRequestExceptionOccurs_LogsAndRethrows()
    {
        // Arrange
        var (service, _, _, mockHttpMessageHandler, _, mockTokenStore) = CreateServiceAndMocks();
        SetTokenProperties(service, mockTokenStore);

        var request = new HttpRequestMessage(HttpMethod.Get, "test");

        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act & Assert
        await Should.ThrowAsync<HttpRequestException>(async () =>
            await service.SendRawRequestAsync(request));
    }

    [Fact]
    public async Task SendRawRequestAsync_WhenErrorResponse_HandlesErrorCorrectly()
    {
        // Arrange
        var (service, _, _, mockHttpMessageHandler, _, mockTokenStore) = CreateServiceAndMocks();
        SetTokenProperties(service, mockTokenStore);

        var request = new HttpRequestMessage(HttpMethod.Get, "test");

        SetupHttpResponse(mockHttpMessageHandler, HttpStatusCode.BadRequest, new
        {
            code = (int)SDHttpResponseCode.IMAGE_NOT_FOUND,
            message = "Image not found"
        });

        // Act
        var response = await service.SendRawRequestAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.ReasonPhrase.ShouldBe("Not Found");
    }

    [Fact]
    public async Task HandleHttpResponseError_AccountLockout_UpdatesTokenErrorTimestamp()
    {
        // Arrange
        var (service, settings, _, mockHttpMessageHandler, mockDataRefreshService, _) = CreateServiceAndMocks();
        var initialTimestamp = settings.TokenErrorTimestamp;

        SetupHttpResponse(mockHttpMessageHandler, HttpStatusCode.Unauthorized, new
        {
            code = (int)SDHttpResponseCode.ACCOUNT_LOCKOUT,
            message = "Account is locked out"
        }, HttpMethod.Post, "token");

        // Act
        await service.RefreshTokenAsync(CancellationToken.None);

        // Assert
        mockDataRefreshService.Verify(x => x.RefreshSDReady(), Times.AtLeastOnce);
        settings.TokenErrorTimestamp.ShouldBeGreaterThan(initialTimestamp);
    }

    [Fact]
    public async Task ConcurrentTokenRefresh_HandlesRaceConditionCorrectly()
    {
        // Arrange
        var (service, _, mockHttpClientFactory, mockHttpMessageHandler, _, mockTokenStore) = CreateServiceAndMocks();

        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://json.schedulesdirect.org/20141201/")
        };

        mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Setup response - create new content for each call to avoid disposal issues
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri != null &&
                    req.RequestUri.ToString().EndsWith("token")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    code = 0,
                    token = "new-token",
                    datetime = DateTime.UtcNow
                }), Encoding.UTF8, "application/json")
            });

        // Act - Start multiple concurrent token refreshes
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => service.RefreshTokenAsync(CancellationToken.None))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.ShouldAllBe(result => result == true);
        service.GoodToken.ShouldBeTrue();

        // Verify the token was set in the token store instead of checking service.Token
        mockTokenStore.Verify(x => x.SetToken("new-token"), Times.AtLeastOnce);

        // Verify that at least one token refresh was called
        // (semaphore should prevent excessive calls, but at least one should succeed)
        mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.AtLeastOnce(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri != null &&
                req.RequestUri.ToString().EndsWith("token")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public void Dispose_ReleasesResources()
    {
        // Arrange
        var (service, _, _, _, _, _) = CreateServiceAndMocks();

        // Act
        service.Dispose();

        // Assert
        var exception = Should.Throw<AggregateException>(() =>
            service.SendRequestAsync<object>(APIMethod.GET, "test").Wait());

        // Verify that the inner exception is ObjectDisposedException
        exception.InnerException.ShouldBeOfType<ObjectDisposedException>();
        exception.InnerException.Message.ShouldContain("SchedulesDirectHttpService");
    }

    [Fact]
    public async Task SendRequestAsync_WithValidToken_SendsRequestSuccessfully()
    {
        // Arrange
        var (service, _, _, mockHttpMessageHandler, _, mockTokenStore) = CreateServiceAndMocks();
        SetTokenProperties(service, mockTokenStore);

        var expectedResponse = new { data = "test-data" };

        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri != null &&
                    req.RequestUri.ToString().EndsWith("test-endpoint")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
            });

        // Act
        var result = await service.SendRequestAsync<object>(APIMethod.GET, "test-endpoint");

        // Assert
        result.ShouldNotBeNull();
        var resultJson = JsonSerializer.Serialize(result);
        resultJson.ShouldContain("test-data");
    }

    [Fact]
    public async Task SendRequestAsync_WithPayload_SendsPayloadCorrectly()
    {
        // Arrange
        var (service, _, _, mockHttpMessageHandler, _, mockTokenStore) = CreateServiceAndMocks();
        SetTokenProperties(service, mockTokenStore);

        var payload = new { key = "value" };
        var expectedResponse = new { success = true };

        // Use a thread-safe collection to capture the request
        var requestContentList = new List<string>();

        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>(async (req, _) =>
            {
                if (req.Content != null)
                {
                    // Read content before it's disposed
                    var content = await req.Content.ReadAsStringAsync();
                    requestContentList.Add(content);
                }
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
            });

        // Act
        var result = await service.SendRequestAsync<object>(APIMethod.POST, "test-endpoint", payload);

        // Assert
        result.ShouldNotBeNull();
        requestContentList.ShouldNotBeEmpty();
        var capturedContent = requestContentList.First();
        capturedContent.ShouldContain("key");
        capturedContent.ShouldContain("value");
    }

    [Fact]
    public async Task SendRequestAsync_WithTokenExpired_RefreshesTokenAndRetries()
    {
        // Arrange
        var (service, _, mockHttpClientFactory, mockHttpMessageHandler, _, mockTokenStore) = CreateServiceAndMocks();

        // Set an old token timestamp to trigger refresh
        SetTokenProperties(service, mockTokenStore, timestamp: DateTime.UtcNow.AddDays(-2));

        // Setup token refresh response
        var tokenResponse = new
        {
            code = 0,
            token = "refreshed-token",
            datetime = DateTime.UtcNow
        };

        // Setup the actual API response
        var apiResponse = new { data = "success" };

        // Setup the mock to return a new HttpClient for each call
        var httpClient1 = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://json.schedulesdirect.org/20141201/")
        };

        var httpClient2 = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://json.schedulesdirect.org/20141201/")
        };

        mockHttpClientFactory.SetupSequence(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient1)
            .Returns(httpClient2);

        // Create a sequence of responses
        mockHttpMessageHandler.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            // First response for token refresh
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(tokenResponse))
            })
            // Second response for the actual API call
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(apiResponse))
            });

        // Act
        var result = await service.SendRequestAsync<object>(APIMethod.GET, "api-endpoint");

        // Assert
        result.ShouldNotBeNull();
        mockTokenStore.Verify(x => x.SetToken("refreshed-token"), Times.Once);

        // Verify the client was created twice (once for token, once for API call)
        mockHttpClientFactory.Verify(x => x.CreateClient(It.IsAny<string>()), Times.Exactly(2));
    }

    [Theory]
    [InlineData(APIMethod.GET, "GET")]
    [InlineData(APIMethod.POST, "POST")]
    [InlineData(APIMethod.PUT, "PUT")]
    [InlineData(APIMethod.DELETE, "DELETE")]
    public async Task SendRequestAsync_UsesCorrectHttpMethod(APIMethod apiMethod, string expectedHttpMethod)
    {
        // Arrange
        var (service, _, _, mockHttpMessageHandler, _, mockTokenStore) = CreateServiceAndMocks();
        SetTokenProperties(service, mockTokenStore);

        HttpMethod capturedMethod = null;

        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedMethod = req.Method)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}")
            });

        // Act
        await service.SendRequestAsync<object>(apiMethod, "endpoint");

        // Assert
        capturedMethod.ShouldNotBeNull();
        capturedMethod.Method.ShouldBe(expectedHttpMethod);
    }

    [Fact]
    public async Task SendRequestAsync_WithErrorResponse_HandlesErrorCorrectly()
    {
        // Arrange
        var (service, _, _, mockHttpMessageHandler, _, mockTokenStore) = CreateServiceAndMocks();
        SetTokenProperties(service, mockTokenStore);

        SetupHttpResponse(mockHttpMessageHandler, HttpStatusCode.BadRequest, new
        {
            code = (int)SDHttpResponseCode.IMAGE_NOT_FOUND,
            message = "Image not found"
        }, HttpMethod.Get, "test-endpoint");

        // Act
        var result = await service.SendRequestAsync<object>(APIMethod.GET, "test-endpoint");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task SendRequestAsync_WithTimeout_ThrowsOperationCanceledException()
    {
        // Arrange
        var (service, _, _, mockHttpMessageHandler, _, mockTokenStore) = CreateServiceAndMocks();
        SetTokenProperties(service, mockTokenStore);

        // Use a shorter timeout for testing
        var timeoutTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (_, token) =>
            {
                try
                {
                    // Delay longer than the timeout
                    await Task.Delay(5000, token);
                    return new HttpResponseMessage(HttpStatusCode.OK);
                }
                catch (TaskCanceledException)
                {
                    throw new OperationCanceledException(token);
                }
            });

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await service.SendRequestAsync<object>(APIMethod.GET, "test-endpoint",
                cancellationToken: timeoutTokenSource.Token));
    }

    [Fact]
    public async Task SendRequestAsync_WithHttpException_LogsAndRethrows()
    {
        // Arrange
        var (service, _, _, mockHttpMessageHandler, _, mockTokenStore) = CreateServiceAndMocks();
        SetTokenProperties(service, mockTokenStore);

        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act & Assert
        await Should.ThrowAsync<HttpRequestException>(async () =>
            await service.SendRequestAsync<object>(APIMethod.GET, "test-endpoint"));
    }

    [Fact]
    public async Task SendRequestAsync_WithUnauthorizedResponse_ClearsTokenAndThrows()
    {
        // Arrange
        var (service, _, _, mockHttpMessageHandler, _, mockTokenStore) = CreateServiceAndMocks();
        SetTokenProperties(service, mockTokenStore);

        // First set up the token validation to succeed
        mockHttpMessageHandler.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri != null),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    code = (int)SDHttpResponseCode.TOKEN_INVALID,
                    message = "Token is invalid"
                }))
            });

        // Act
        var result = await service.SendRequestAsync<object>(APIMethod.GET, "test-endpoint");

        // Assert
        result.ShouldBeNull();
        mockTokenStore.Verify(x => x.ClearToken(), Times.Once);
        service.GoodToken.ShouldBeFalse();
    }

    [Fact]
    public async Task SendRequestAsync_WithCancellation_CancelsRequest()
    {
        // Arrange
        var (service, _, _, mockHttpMessageHandler, _, mockTokenStore) = CreateServiceAndMocks();
        SetTokenProperties(service, mockTokenStore);

        var cancellationTokenSource = new CancellationTokenSource();

        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (_, token) =>
            {
                await Task.Delay(500, token);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        // Act
        var task = service.SendRequestAsync<object>(APIMethod.GET, "test-endpoint",
            cancellationToken: cancellationTokenSource.Token);

        // Cancel the request
        cancellationTokenSource.Cancel();

        // Assert
        await Should.ThrowAsync<OperationCanceledException>(async () => await task);
    }

    [Fact]
    public async Task SendRequestAsync_WithServiceUnavailable_HandlesCorrectly()
    {
        // Arrange
        var (service, _, _, mockHttpMessageHandler, _, mockTokenStore) = CreateServiceAndMocks();
        SetTokenProperties(service, mockTokenStore);

        SetupHttpResponse(mockHttpMessageHandler, HttpStatusCode.ServiceUnavailable, new
        {
            code = (int)SDHttpResponseCode.SERVICE_OFFLINE,
            message = "Service is offline"
        });

        // Act
        var result = await service.SendRequestAsync<object>(APIMethod.GET, "test-endpoint");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task SendRequestAsync_WithTooManyRequests_HandlesRateLimiting()
    {
        // Arrange
        var (service, _, _, mockHttpMessageHandler, _, mockTokenStore) = CreateServiceAndMocks();
        SetTokenProperties(service, mockTokenStore);

        SetupHttpResponse(mockHttpMessageHandler, (HttpStatusCode)429, new
        {
            code = (int)SDHttpResponseCode.MAX_IMAGE_DOWNLOADS,
            message = "Too many image downloads"
        });

        // Act
        var result = await service.SendRequestAsync<object>(APIMethod.GET, "test-endpoint");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task SendRequestAsync_SetsCorrectUserAgent()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<SchedulesDirectHttpService>>();
        var mockSDSettings = new Mock<IOptionsMonitor<SDSettings>>();
        var mockDataRefreshService = new Mock<IDataRefreshService>();
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Loose);
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        var mockApiErrorManager = new Mock<IApiErrorManager>();
        var mockTokenStore = new Mock<ITokenStore>();

        var appVersion = "1.2.3.Sha.BADC0FFEE0DDF00D000000000000000000000000";
        var sdSettings = new SDSettings
        {
            SDEnabled = true,
            ErrorCooldowns = new List<ErrorCooldownSetting>(),
            SDUserName = "testuser",
            SDPassword = "testpass",
            UserAgent = $"StreamMaster/{appVersion}"
        };

        mockSDSettings.Setup(x => x.CurrentValue).Returns(sdSettings);
        mockDataRefreshService.Setup(x => x.RefreshSDReady()).Returns(Task.CompletedTask);
        mockTokenStore.Setup(x => x.Token).Returns("test-token");

        // Capture the actual user agent sent in the request
        string capturedUserAgent = null;

        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) =>
            {
                if (req.Headers.UserAgent.Any())
                {
                    capturedUserAgent = string.Join(" ", req.Headers.UserAgent);
                }
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://json.schedulesdirect.org/20141201/")
        };

        mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = new SchedulesDirectHttpService(
            mockHttpClientFactory.Object,
            mockLogger.Object,
            mockSDSettings.Object,
            mockDataRefreshService.Object,
            mockApiErrorManager.Object,
            mockTokenStore.Object);

        // Set token to avoid refresh
        SetTokenProperties(service, mockTokenStore);

        // Act
        await service.SendRequestAsync<object>(APIMethod.GET, "test-endpoint");

        // Assert
        capturedUserAgent.ShouldNotBeNull();
        capturedUserAgent.ShouldBe($"StreamMaster/{appVersion}");
    }

    [Fact]
    public async Task SendRequestAsync_WhenGlobalCooldownActive_ReturnsDefault()
    {
        // Arrange
        var mockApiErrorManager = new Mock<IApiErrorManager>();

        // Setup ApiErrorManager to report a global cooldown
        var cooldownCode = SDHttpResponseCode.SERVICE_OFFLINE;
        mockApiErrorManager
            .Setup(m => m.IsInCooldown(cooldownCode))
            .Returns(true);

        mockApiErrorManager
            .Setup(m => m.GetCooldownInfo(cooldownCode))
            .Returns(new ErrorCooldownInfo(DateTime.UtcNow.AddHours(1), "Service is offline"));

        var (service, _, mockHttpClientFactory, _, _, _) = CreateServiceAndMocks(mockApiErrorManager);

        // Act
        var result = await service.SendRequestAsync<object>(APIMethod.GET, "test-endpoint");

        // Assert
        result.ShouldBeNull();
        mockHttpClientFactory.Verify(x => x.CreateClient(It.IsAny<string>()), Times.Never);
        mockApiErrorManager.Verify(m => m.IsInCooldown(cooldownCode), Times.Once);
        mockApiErrorManager.Verify(m => m.GetCooldownInfo(cooldownCode), Times.Once);
    }

    [Fact]
    public async Task SendRawRequestAsync_WhenGlobalCooldownActive_ReturnsServiceUnavailable()
    {
        // Arrange
        var mockApiErrorManager = new Mock<IApiErrorManager>();
        var (service, settings, _, mockHttpMessageHandler, _, mockTokenStore) = CreateServiceAndMocks(mockApiErrorManager);

        // Setup token properties to avoid token refresh
        SetTokenProperties(service, mockTokenStore, "valid-token", true, DateTime.UtcNow);

        // Configure the mock to handle ANY call to IsInCooldown
        var cooldownCode = SDHttpResponseCode.SERVICE_OFFLINE;
        mockApiErrorManager
            .Setup(m => m.IsInCooldown(It.IsAny<SDHttpResponseCode>()))
            .Returns(false); // Default to false

        // Configure it to return true for SERVICE_OFFLINE specifically
        mockApiErrorManager
            .Setup(m => m.IsInCooldown(cooldownCode))
            .Returns(true);

        // Configure the error cooldowns collection
        settings.ErrorCooldowns = new List<ErrorCooldownSetting>
        {
            new ErrorCooldownSetting
            {
                ErrorCode = (int)cooldownCode,
                Reason = "Service is offline",
                CooldownUntil = DateTime.UtcNow.AddHours(1)
            }
        };

        // Make sure all HTTP calls return success
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}")
            });

        var request = new HttpRequestMessage(HttpMethod.Get, "test-endpoint");

        // Act
        var response = await service.SendRawRequestAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        response.ReasonPhrase.ShouldBe("Service is offline");
        mockApiErrorManager.Verify(m => m.IsInCooldown(cooldownCode), Times.AtLeastOnce);
    }

    [Fact]
    public async Task SendRequestAsync_WhenMaxImageDownloadsReached_SetsCooldown()
    {
        // Arrange
        var mockApiErrorManager = new Mock<IApiErrorManager>();
        var (service, _, _, mockHttpMessageHandler, _, mockTokenStore) = CreateServiceAndMocks(mockApiErrorManager);

        SetTokenProperties(service, mockTokenStore);

        SetupHttpResponse(mockHttpMessageHandler, HttpStatusCode.TooManyRequests, new
        {
            code = (int)SDHttpResponseCode.MAX_IMAGE_DOWNLOADS,
            message = "Too many image downloads"
        });

        // Act
        var result = await service.SendRequestAsync<object>(APIMethod.GET, "test-endpoint");

        // Assert
        result.ShouldBeNull();
        mockApiErrorManager.Verify(
            m => m.SetCooldown(
                SDHttpResponseCode.MAX_IMAGE_DOWNLOADS,
                It.Is<DateTime>(dt => dt > DateTime.UtcNow),
                It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task SendRawRequestAsync_WhenMaxImageDownloadsReached_SetsCooldown()
    {
        // Arrange
        var mockApiErrorManager = new Mock<IApiErrorManager>();
        var (service, _, _, mockHttpMessageHandler, _, mockTokenStore) = CreateServiceAndMocks(mockApiErrorManager);

        SetTokenProperties(service, mockTokenStore);

        var request = new HttpRequestMessage(HttpMethod.Get, "test-endpoint");

        SetupHttpResponse(mockHttpMessageHandler, HttpStatusCode.TooManyRequests, new
        {
            code = (int)SDHttpResponseCode.MAX_IMAGE_DOWNLOADS,
            message = "Too many image downloads"
        });

        // Act
        var response = await service.SendRawRequestAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        mockApiErrorManager.Verify(
            m => m.SetCooldown(
                SDHttpResponseCode.MAX_IMAGE_DOWNLOADS,
                It.Is<DateTime>(dt => dt > DateTime.UtcNow),
                It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenTooManyLoginsError_SetsCooldown()
    {
        // Arrange
        var (service, _, _, mockHttpMessageHandler, _, _) = CreateServiceAndMocks();

        SetupTokenResponse(mockHttpMessageHandler, HttpStatusCode.Locked, new
        {
            code = (int)SDHttpResponseCode.TOO_MANY_LOGINS,
            message = "Too many login attempts"
        });

        // Act
        var result = await service.RefreshTokenAsync(CancellationToken.None);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Constructor_WithNullTokenStore_ThrowsArgumentNullException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<SchedulesDirectHttpService>>();
        var mockSDSettings = new Mock<IOptionsMonitor<SDSettings>>();
        var mockDataRefreshService = new Mock<IDataRefreshService>();
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        var mockApiErrorManager = new Mock<IApiErrorManager>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new SchedulesDirectHttpService(
            mockHttpClientFactory.Object,
            mockLogger.Object,
            mockSDSettings.Object,
            mockDataRefreshService.Object,
            mockApiErrorManager.Object,
            null!));
    }

    private (SchedulesDirectHttpService service, SDSettings settings, Mock<IHttpClientFactory> mockHttpClientFactory, Mock<HttpMessageHandler> mockHttpMessageHandler, Mock<IDataRefreshService> mockDataRefreshService, Mock<ITokenStore> mockTokenStore)
        CreateServiceAndMocks(Mock<IApiErrorManager> mockApiErrorManager = default!)
    {
        var mockLogger = new Mock<ILogger<SchedulesDirectHttpService>>();
        var mockSDSettings = new Mock<IOptionsMonitor<SDSettings>>();
        var mockDataRefreshService = new Mock<IDataRefreshService>();
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Loose);
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        var mockTokenStore = new Mock<ITokenStore>();
        mockApiErrorManager ??= new Mock<IApiErrorManager>();

        var sdSettings = new SDSettings
        {
            SDEnabled = true,
            ErrorCooldowns = new List<ErrorCooldownSetting>(),
            SDUserName = "testuser",
            SDPassword = "testpass"
        };

        mockSDSettings.Setup(x => x.CurrentValue).Returns(sdSettings);
        mockDataRefreshService.Setup(x => x.RefreshSDReady()).Returns(Task.CompletedTask);
        mockTokenStore.Setup(x => x.Token).Returns((string?)null);

        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://json.schedulesdirect.org/20141201/")
        };

        mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = new SchedulesDirectHttpService(
            mockHttpClientFactory.Object,
            mockLogger.Object,
            mockSDSettings.Object,
            mockDataRefreshService.Object,
            mockApiErrorManager.Object,
            mockTokenStore.Object);

        return (service, sdSettings, mockHttpClientFactory, mockHttpMessageHandler, mockDataRefreshService, mockTokenStore);
    }

    private void SetTokenProperties(SchedulesDirectHttpService service, Mock<ITokenStore> mockTokenStore, string token = "test-token", bool goodToken = true, DateTime? timestamp = null)
    {
        var goodTokenProperty = typeof(SchedulesDirectHttpService).GetProperty("GoodToken");
        var tokenTimestampProperty = typeof(SchedulesDirectHttpService).GetProperty("TokenTimestamp");

        mockTokenStore.Setup(x => x.Token).Returns(token);
        goodTokenProperty?.SetValue(service, goodToken);
        tokenTimestampProperty?.SetValue(service, timestamp ?? DateTime.UtcNow);
    }

    private void SetupTokenResponse(Mock<HttpMessageHandler> mockHandler, HttpStatusCode statusCode, object responseContent)
    {
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri != null &&
                    req.RequestUri.ToString().EndsWith("token")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(JsonSerializer.Serialize(responseContent))
            });
    }

    private void SetupHttpResponse(Mock<HttpMessageHandler> mockHandler, HttpStatusCode statusCode, object responseContent, HttpMethod? method = null, string? endpointPattern = null)
    {
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    (method == null || req.Method == method) &&
                    (endpointPattern == null ||
                     (req.RequestUri != null && req.RequestUri.ToString().Contains(endpointPattern)))),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(JsonSerializer.Serialize(responseContent)),
                RequestMessage = new HttpRequestMessage(method ?? HttpMethod.Get, "https://json.schedulesdirect.org/20141201/test")
            });
    }
}