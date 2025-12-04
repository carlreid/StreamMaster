using StreamMaster.Domain.Configuration;
using StreamMaster.Domain.Extensions;
using StreamMaster.Domain.Helpers;
using StreamMaster.Domain.Services;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace StreamMaster.SchedulesDirect.Services;

/// <summary>
/// This HttpService is designed to work with the SchedulesDirect API.
/// Documentation for the API can be found at: https://github.com/SchedulesDirect/JSON-Service/wiki/API-20141201
/// </summary>
public class SchedulesDirectHttpService : ISchedulesDirectHttpService, IDisposable
{
    private readonly ITokenStore _tokenStore;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SchedulesDirectHttpService> _logger;
    private readonly IOptionsMonitor<SDSettings> _sdSettings;
    private readonly IDataRefreshService _dataRefreshService;
    private readonly IApiErrorManager _apiErrorManager;
    private readonly SemaphoreSlim _tokenSemaphore = new(1, 1);
    private bool _disposed;
    private static readonly HttpRequestOptionsKey<object?> SkipTokenAuthKey = new("SkipTokenAuth");

    public string? Token => _tokenStore.Token; // ✅ Fixed: Use token store
    public DateTime TokenTimestamp { get; private set; }
    public bool GoodToken { get; private set; }
    public bool IsReady => !_disposed && _sdSettings.CurrentValue.ErrorCooldowns.All(errorCooldown => errorCooldown.CooldownUntil <= SMDT.UtcNow); // ✅ Fixed: <= not >

    public SchedulesDirectHttpService(
        IHttpClientFactory httpClientFactory,
        ILogger<SchedulesDirectHttpService> logger,
        IOptionsMonitor<SDSettings> sdSettings,
        IDataRefreshService dataRefreshService,
        IApiErrorManager apiErrorManager,
        ITokenStore tokenStore)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sdSettings = sdSettings ?? throw new ArgumentNullException(nameof(sdSettings));
        _dataRefreshService = dataRefreshService ?? throw new ArgumentNullException(nameof(dataRefreshService));
        _apiErrorManager = apiErrorManager ?? throw new ArgumentNullException(nameof(apiErrorManager));
        _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
    }

    public async Task<T?> SendRequestAsync<T>(APIMethod method, string endpoint, object? payload = null, CancellationToken cancellationToken = default, bool authenticationRequired = false)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SchedulesDirectHttpService));
        }

        if (!_sdSettings.CurrentValue.SDEnabled)
        {
            return default;
        }

        foreach (var code in SDHttpResponseCodeExtensions.GlobalCooldowns)
        {
            if (_apiErrorManager.IsInCooldown(code))
            {
                var cooldownInfo = _apiErrorManager.GetCooldownInfo(code);
                _logger.LogWarning(
                    "Request to {Endpoint} blocked due to active cooldown: {Reason}. Retry after {Time}",
                    endpoint,
                    cooldownInfo!.Reason,
                    cooldownInfo.CooldownUntil.ToString("g"));
                return default;
            }
        }

        if (!await ValidateTokenAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            throw new TokenValidationException("Token validation failed. Cannot proceed with the request.");
        }

        try
        {
            using var request = new HttpRequestMessage(new HttpMethod(method.ToString()), endpoint);

            if (payload != null)
            {
                request.Content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");
            }

            var httpClient = GetHttpClient(authenticationRequired);
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

            using var response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                linkedCts.Token).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                HandleHttpResponseError(response, content);
                return default;
            }

            return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Request to {Endpoint} was cancelled or timed out", endpoint);
            throw;
        }
        catch (Exception ex) when (ex is not TokenValidationException)
        {
            _logger.LogError(ex, "Error occurred during HTTP request to {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<HttpResponseMessage> SendRawRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken = default, bool authenticationRequired = false)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SchedulesDirectHttpService));
        }

        foreach (var code in SDHttpResponseCodeExtensions.GlobalCooldowns)
        {
            if (_apiErrorManager.IsInCooldown(code))
            {
                var cooldown = _sdSettings.CurrentValue.ErrorCooldowns
                    .First(c => c.ErrorCode == (int)code);

                _logger.LogWarning(
                    "Request to {Uri} blocked due to active cooldown: {Reason}. Retry after {Time}",
                    request.RequestUri,
                    cooldown.Reason,
                    cooldown.CooldownUntil.ToString("g"));

                var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                {
                    ReasonPhrase = cooldown.Reason
                };

                return response;
            }
        }

        await ValidateTokenAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        try
        {
            var httpClient = GetHttpClient(authenticationRequired);
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

            var clonedRequest = await CloneHttpRequestAsync(request);

            HttpResponseMessage response = await httpClient
                .SendAsync(clonedRequest, HttpCompletionOption.ResponseHeadersRead, linkedCts.Token)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return HandleHttpResponseError(response, content);
            }

            return response;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error sending raw HTTP request to {Uri}", request.RequestUri);
            throw;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Request to {Uri} was cancelled or timed out", request.RequestUri);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending raw HTTP request to {Uri}", request.RequestUri);
            throw;
        }
    }

    private HttpResponseMessage HandleHttpResponseError(HttpResponseMessage response, string? content)
    {
        string? tokenUsed = null;
        if (response.RequestMessage?.Headers.Contains("token") == true)
        {
            tokenUsed = response.RequestMessage.Headers.GetValues("token")?.FirstOrDefault();
        }

        if (!string.IsNullOrEmpty(content))
        {
            BaseResponse? err = JsonSerializer.Deserialize<BaseResponse>(content);
            if (err != null)
            {
                SDHttpResponseCode sdCode = (SDHttpResponseCode)err.Code;

                if (sdCode == SDHttpResponseCode.TOKEN_INVALID)
                {
                    _logger.LogError("SDToken is invalid {Token} {Length}",
                        tokenUsed?[..Math.Min(5, tokenUsed.Length)],
                        tokenUsed?.Length ?? 0);
                }

                switch (sdCode)
                {
                    case SDHttpResponseCode.SERVICE_OFFLINE:
                        response.StatusCode = HttpStatusCode.ServiceUnavailable;
                        response.ReasonPhrase = "Service Unavailable";
                        break;

                    case SDHttpResponseCode.JSON_ACCOUNT_ACCESS_DISABLED:
                    case SDHttpResponseCode.ACCOUNT_EXPIRED:
                    case SDHttpResponseCode.APPLICATION_DISABLED:
                        response.StatusCode = HttpStatusCode.Forbidden;
                        response.ReasonPhrase = "Forbidden";
                        break;

                    case SDHttpResponseCode.ACCOUNT_LOCKOUT:
                        response.StatusCode = HttpStatusCode.Locked;
                        response.ReasonPhrase = "Locked";
                        break;

                    case SDHttpResponseCode.IMAGE_NOT_FOUND:
                    case SDHttpResponseCode.IMAGE_QUEUED:
                        response.StatusCode = HttpStatusCode.NotFound;
                        response.ReasonPhrase = "Not Found";
                        break;

                    case SDHttpResponseCode.MAX_IMAGE_DOWNLOADS:
                    case SDHttpResponseCode.MAX_IMAGE_DOWNLOADS_TRIAL:
                        // Next day at UTC midnight
                        _apiErrorManager.SetCooldown(sdCode, SMDT.UtcNow.Date.AddDays(1), "Image download limit reached");

                        response.StatusCode = HttpStatusCode.TooManyRequests;
                        response.ReasonPhrase = "Too Many Requests";
                        break;

                    case SDHttpResponseCode.MAX_IMAGE_INVALID_URI_ERRORS:
                        // Resets ever 24 hours
                        _apiErrorManager.SetCooldown(sdCode, SMDT.UtcNow.AddDays(1), "Application is requesting URIs which do not exist. ");

                        response.StatusCode = HttpStatusCode.TooManyRequests;
                        response.ReasonPhrase = "Too Many Requests";
                        break;

                    case SDHttpResponseCode.TOKEN_MISSING:
                        // In case of a request being made where no token is added, ensure we don't block ourselves.
                        _apiErrorManager.SetCooldown(sdCode, SMDT.UtcNow.AddMinutes(10), "A request was made to SchedulesDirect without a token.");
                        response.StatusCode = HttpStatusCode.Unauthorized;
                        response.ReasonPhrase = "Unauthorized";
                        ClearToken();
                        break;

                    case SDHttpResponseCode.TOKEN_INVALID:
                    case SDHttpResponseCode.INVALID_USER:
                    case SDHttpResponseCode.TOKEN_EXPIRED:
                    case SDHttpResponseCode.ACCOUNT_INACTIVE:
                    case SDHttpResponseCode.UNKNOWN_CLIENT:
                        response.StatusCode = HttpStatusCode.Unauthorized;
                        response.ReasonPhrase = "Unauthorized";
                        ClearToken();
                        break;

                    case SDHttpResponseCode.TOO_MANY_LOGINS:
                        response.StatusCode = HttpStatusCode.Locked;
                        response.ReasonPhrase = "Locked";
                        break;
                }
            }
        }

        if (response.StatusCode == HttpStatusCode.OK)
        {
            response.StatusCode = (HttpStatusCode)418;
            response.ReasonPhrase = "I'm a teapot";
        }

        if (response.StatusCode != HttpStatusCode.NotModified)
        {
            string tokenUsedShort = tokenUsed?.Length >= 5 ? tokenUsed[..5] : tokenUsed ?? string.Empty;
            _logger.LogError(
                "{RequestPath}: {StatusCode} {ReasonPhrase} : Token={TokenUsed}...{Content}",
                response.RequestMessage?.RequestUri?.AbsolutePath.Replace("https://json.schedulesdirect.org/20141201/", "/"),
                (int)response.StatusCode,
                response.ReasonPhrase,
                tokenUsedShort,
                !string.IsNullOrEmpty(content) ? $"\n{content}" : ""
            );
        }

        return response;
    }

    public async Task<bool> RefreshTokenAsync(CancellationToken cancellationToken)
    {
        if (_disposed)
        {
            return false;
        }

        foreach (var code in SDHttpResponseCodeExtensions.LoginResponseCodes)
        {
            if (_apiErrorManager.IsInCooldown(code))
            {
                var cooldown = _sdSettings.CurrentValue.ErrorCooldowns
                    .First(c => c.ErrorCode == (int)code);

                _logger.LogWarning(
                    "Token refresh blocked due to active cooldown: {Reason}. Retry after {Time}",
                    cooldown.Reason,
                    cooldown.CooldownUntil.ToString("g"));

                return false;
            }
        }

        try
        {
            await _tokenSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            if (!IsReady)
            {
                GoodToken = false;
                return false;
            }

            // Avoid unnecessary refreshes if token is still fresh
            if (!string.IsNullOrEmpty(Token) &&
                GoodToken &&
                SMDT.UtcNow - TokenTimestamp < TimeSpan.FromMinutes(1))
            {
                return true;
            }

            string username = _sdSettings.CurrentValue.SDUserName;
            string password = _sdSettings.CurrentValue.SDPassword;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                _logger.LogWarning("Username or password is missing.");
                return false;
            }
            ClearToken();

            // We are performing an authentication request, so authentication isn't required
            var httpClient = GetHttpClient(authenticationRequired: false);
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

            var requestPayload = JsonSerializer.Serialize(new { username, password });
            using var request = new HttpRequestMessage(HttpMethod.Post, "token")
            {
                Content = new StringContent(requestPayload, Encoding.UTF8, "application/json")
            };
            request.Options.Set(SkipTokenAuthKey, true);

            using var response = await httpClient.SendAsync(request, linkedCts.Token).ConfigureAwait(false);

            TokenResponse? tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: linkedCts.Token);
            if (response.IsSuccessStatusCode)
            {
                if (tokenResponse?.Code == 0 && !string.IsNullOrEmpty(tokenResponse.Token))
                {
                    SetToken(tokenResponse.Token, tokenResponse.Datetime);

                    _logger.LogInformation("Token refreshed successfully. Token={Token}...",
                        Token?[..Math.Min(5, Token.Length)]);
                    _sdSettings.CurrentValue.TokenErrorTimestamp = DateTime.MinValue;
                    //TODO: Clean up any login related error timeout
                    SettingsHelper.UpdateSetting(_sdSettings.CurrentValue);
                    await _dataRefreshService.RefreshSDReady().ConfigureAwait(false);
                    return true;
                }
            }

            string content = await response.Content.ReadAsStringAsync(linkedCts.Token);
            using HttpResponseMessage httpResponse = HandleHttpResponseError(response, content);

            _sdSettings.CurrentValue.TokenErrorTimestamp =
                tokenResponse == null ||
                tokenResponse.Code == (int)SDHttpResponseCode.ACCOUNT_LOCKOUT ||
                tokenResponse.Code == (int)SDHttpResponseCode.TOO_MANY_LOGINS ||
                tokenResponse.Code == (int)SDHttpResponseCode.JSON_ACCOUNT_ACCESS_DISABLED
                    ? SMDT.UtcNow.AddHours(24.0)
                    : SMDT.UtcNow.AddMinutes(1.0);

            SettingsHelper.UpdateSetting(_sdSettings.CurrentValue);
            await _dataRefreshService.RefreshSDReady().ConfigureAwait(false);
            _logger.LogError("Failed to fetch token. Status code: {StatusCode} {reason} {message}",
                httpResponse.StatusCode, httpResponse.ReasonPhrase, tokenResponse?.Message ?? "");
            return false;
        }
        finally
        {
            _tokenSemaphore.Release();
        }
    }

    public async Task<bool> ValidateTokenAsync(bool forceReset = false, CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            return false;
        }

        if (!_sdSettings.CurrentValue.SDEnabled)
        {
            return false;
        }

        // Prevent multiple concurrent refreshes with a semaphore
        await _tokenSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (forceReset || string.IsNullOrEmpty(Token) || !GoodToken || SMDT.UtcNow - TokenTimestamp > TimeSpan.FromHours(23))
            {
                // Release semaphore before long-running refresh
                _tokenSemaphore.Release();
                bool refreshed = await RefreshTokenAsync(cancellationToken).ConfigureAwait(false);
                if (!refreshed)
                {
                    _logger.LogError("Token validation failed. Unable to refresh token.");
                    return false;
                }
                return true;
            }

            return GoodToken;
        }
        finally
        {
            // Ensure semaphore is released if we didn't release it earlier
            if (_tokenSemaphore.CurrentCount == 0)
            {
                _tokenSemaphore.Release();
            }
        }
    }

    public void ClearToken()
    {
        _tokenStore.ClearToken();
        GoodToken = false;
        TokenTimestamp = DateTime.MinValue;
        _logger.LogWarning("Token cleared.");
    }

    private void SetToken(string token, DateTime timestamp)
    {
        _tokenStore.SetToken(token);
        TokenTimestamp = timestamp;
        GoodToken = true;
    }

    private HttpClient GetHttpClient(bool authenticationRequired = false)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SchedulesDirectHttpService));
        }

        if (authenticationRequired && string.IsNullOrEmpty(Token))
        {
            throw new UnauthorizedAccessException("Auth token is found as empty, but caller expects authentication to be made.");
        }

        var httpClient = _httpClientFactory.CreateClient(nameof(SchedulesDirectHttpService));

        httpClient.DefaultRequestHeaders.UserAgent.Clear();
        string userAgent = _sdSettings.CurrentValue.UserAgent ?? "StreamMaster/1.0";
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);

        return httpClient; // ✅ Fixed: Don't set timeout on factory-created client
    }

    private static async Task<HttpRequestMessage> CloneHttpRequestAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version
        };

        if (request.Content != null)
        {
            var contentBytes = await request.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            clone.Content = new ByteArrayContent(contentBytes);

            // Copy content headers
            foreach (var header in request.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var option in request.Options)
        {
            clone.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);
        }

        return clone;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _tokenSemaphore.Dispose();
        }

        _disposed = true;
    }
}