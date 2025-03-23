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
using System.Text.Json;

namespace StreamMaster.SchedulesDirect.UnitTests.Services;

public class HttpServiceTests
{
    [Fact]
    public void IsReady_WhenTokenErrorTimestampInPast_ReturnsTrue()
    {
        // Arrange
        var (service, settings, _, _, _) = CreateServiceAndMocks();
        settings.TokenErrorTimestamp = DateTime.UtcNow.AddMinutes(-5);

        // Act
        bool result = service.IsReady;

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsReady_WhenTokenErrorTimestampInFuture_ReturnsFalse()
    {
        // Arrange
        var (service, settings, _, _, _) = CreateServiceAndMocks();
        settings.TokenErrorTimestamp = DateTime.UtcNow.AddMinutes(5);

        // Act
        bool result = service.IsReady;

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void ClearToken_ResetsTokenState()
    {
        // Arrange
        var (service, _, _, _, _) = CreateServiceAndMocks();
        SetTokenProperties(service);

        // Act
        service.ClearToken();

        // Assert
        service.Token.ShouldBeNull();
        service.GoodToken.ShouldBeFalse();
        service.TokenTimestamp.ShouldBe(DateTime.MinValue);
    }

    [Fact]
    public async Task ValidateTokenAsync_WhenSDDisabled_ReturnsFalse()
    {
        // Arrange
        var (service, settings, _, _, _) = CreateServiceAndMocks();
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
        var (service, _, _, mockHttpMessageHandler, _) = CreateServiceAndMocks();

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
        service.Token.ShouldBe("new-token");
    }

    [Fact]
    public async Task RefreshTokenAsync_SuccessfulTokenRefresh_UpdatesTokenAndReturnsTrue()
    {
        // Arrange
        var (service, _, _, mockHttpMessageHandler, mockDataRefreshService) = CreateServiceAndMocks();
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
        service.Token.ShouldBe("new-token");
        service.GoodToken.ShouldBeTrue();
        Math.Abs((service.TokenTimestamp - tokenDateTime).TotalSeconds).ShouldBeLessThan(1);
        mockDataRefreshService.Verify(x => x.RefreshSDReady(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task RefreshTokenAsync_MissingCredentials_ReturnsFalse()
    {
        // Arrange
        var (service, settings, mockHttpClientFactory, _, _) = CreateServiceAndMocks();
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
        var (service, settings, mockHttpClientFactory, _, _) = CreateServiceAndMocks();
        settings.TokenErrorTimestamp = DateTime.UtcNow.AddHours(1); // Future timestamp means not ready

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
        var (service, settings, _, mockHttpMessageHandler, _) = CreateServiceAndMocks();
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
        service.Token.ShouldBeNull();
        service.GoodToken.ShouldBeFalse();
        settings.TokenErrorTimestamp.ShouldBeGreaterThan(initialTimestamp);
    }

    [Fact]
    public async Task SendRequestAsync_WhenSDDisabled_ReturnsDefault()
    {
        // Arrange
        var (service, settings, mockHttpClientFactory, _, _) = CreateServiceAndMocks();
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
        var (service, _, _, mockHttpMessageHandler, _) = CreateServiceAndMocks();

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
        var (service, _, mockHttpClientFactory, mockHttpMessageHandler, _) = CreateServiceAndMocks();
        SetTokenProperties(service);

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
        var (service, _, _, mockHttpMessageHandler, _) = CreateServiceAndMocks();
        SetTokenProperties(service);

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
        var (service, _, _, mockHttpMessageHandler, _) = CreateServiceAndMocks();
        SetTokenProperties(service);

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
        var (service, settings, _, mockHttpMessageHandler, mockDataRefreshService) = CreateServiceAndMocks();
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
        var (service, _, _, mockHttpMessageHandler, _) = CreateServiceAndMocks();

        SetupTokenResponse(mockHttpMessageHandler, HttpStatusCode.OK, new
        {
            code = 0,
            token = "new-token",
            datetime = DateTime.UtcNow
        });

        // Act - Start multiple concurrent token refreshes
        var task1 = service.RefreshTokenAsync(CancellationToken.None);
        var task2 = service.RefreshTokenAsync(CancellationToken.None);
        var task3 = service.RefreshTokenAsync(CancellationToken.None);

        await Task.WhenAll(task1, task2, task3);

        // Assert
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
        var (service, _, _, _, _) = CreateServiceAndMocks();

        // Act
        service.Dispose();

        // Assert
        var exception = Should.Throw<AggregateException>(() =>
            service.SendRequestAsync<object>(APIMethod.GET, "test").Wait());

        // Verify that the inner exception is ObjectDisposedException
        exception.InnerException.ShouldBeOfType<ObjectDisposedException>();
        exception.InnerException.Message.ShouldContain("HttpService");
    }

    [Fact]
    public async Task SendRequestAsync_WithValidToken_SendsRequestSuccessfully()
    {
        // Arrange
        var (service, _, _, mockHttpMessageHandler, _) = CreateServiceAndMocks();
        SetTokenProperties(service);

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
        var (service, _, _, mockHttpMessageHandler, _) = CreateServiceAndMocks();
        SetTokenProperties(service);

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
        var (service, _, mockHttpClientFactory, mockHttpMessageHandler, _) = CreateServiceAndMocks();

        // Set an old token timestamp to trigger refresh
        SetTokenProperties(service, timestamp: DateTime.UtcNow.AddDays(-2));

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
        service.Token.ShouldBe("refreshed-token");

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
        var (service, _, _, mockHttpMessageHandler, _) = CreateServiceAndMocks();
        SetTokenProperties(service);

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
        var (service, _, _, mockHttpMessageHandler, _) = CreateServiceAndMocks();
        SetTokenProperties(service);

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
        var (service, _, _, mockHttpMessageHandler, _) = CreateServiceAndMocks();
        SetTokenProperties(service);

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
        var (service, _, _, mockHttpMessageHandler, _) = CreateServiceAndMocks();
        SetTokenProperties(service);

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
        var (service, _, _, mockHttpMessageHandler, _) = CreateServiceAndMocks();
        SetTokenProperties(service);

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
        service.Token.ShouldBeNull();
        service.GoodToken.ShouldBeFalse();
    }

    [Fact]
    public async Task SendRequestAsync_WithCancellation_CancelsRequest()
    {
        // Arrange
        var (service, _, _, mockHttpMessageHandler, _) = CreateServiceAndMocks();
        SetTokenProperties(service);

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
        var (service, _, _, mockHttpMessageHandler, _) = CreateServiceAndMocks();
        SetTokenProperties(service);

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
        var (service, _, _, mockHttpMessageHandler, _) = CreateServiceAndMocks();
        SetTokenProperties(service);

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

    private (HttpService service, SDSettings settings, Mock<IHttpClientFactory> mockHttpClientFactory, Mock<HttpMessageHandler> mockHttpMessageHandler, Mock<IDataRefreshService> mockDataRefreshService) CreateServiceAndMocks()
    {
        var mockLogger = new Mock<ILogger<HttpService>>();
        var mockSDSettings = new Mock<IOptionsMonitor<SDSettings>>();
        var mockSettings = new Mock<IOptionsMonitor<Setting>>();
        var mockDataRefreshService = new Mock<IDataRefreshService>();
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Loose);
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();

        var sdSettings = new SDSettings
        {
            SDEnabled = true,
            TokenErrorTimestamp = DateTime.MinValue,
            SDUserName = "testuser",
            SDPassword = "testpass"
        };

        mockSDSettings.Setup(x => x.CurrentValue).Returns(sdSettings);
        mockSettings.Setup(x => x.CurrentValue).Returns(new Setting { ClientUserAgent = "TestUserAgent" });
        mockDataRefreshService.Setup(x => x.RefreshSDReady()).Returns(Task.CompletedTask);

        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://json.schedulesdirect.org/20141201/")
        };

        mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = new HttpService(
            mockHttpClientFactory.Object,
            mockLogger.Object,
            mockSDSettings.Object,
            mockSettings.Object,
            mockDataRefreshService.Object);

        return (service, sdSettings, mockHttpClientFactory, mockHttpMessageHandler, mockDataRefreshService);
    }

    private void SetTokenProperties(HttpService service, string token = "test-token", bool goodToken = true, DateTime? timestamp = null)
    {
        var tokenProperty = typeof(HttpService).GetProperty("Token");
        var goodTokenProperty = typeof(HttpService).GetProperty("GoodToken");
        var tokenTimestampProperty = typeof(HttpService).GetProperty("TokenTimestamp");

        if (tokenProperty != null) tokenProperty.SetValue(service, token);
        if (goodTokenProperty != null) goodTokenProperty.SetValue(service, goodToken);
        if (tokenTimestampProperty != null) tokenTimestampProperty.SetValue(service, timestamp ?? DateTime.UtcNow);
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