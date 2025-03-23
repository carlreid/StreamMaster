using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using StreamMaster.Domain.Configuration;
using StreamMaster.Domain.Extensions;
using StreamMaster.Domain.Helpers;
using StreamMaster.Domain.Services;

namespace StreamMaster.SchedulesDirect.Services;

public class HttpService : IHttpService, IDisposable
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpService> _logger;
    private readonly IOptionsMonitor<SDSettings> _sdSettings;
    private readonly IDataRefreshService _dataRefreshService;
    private readonly SemaphoreSlim _tokenSemaphore = new(1, 1);
    private bool _disposed;

    public string? Token { get; private set; }
    public DateTime TokenTimestamp { get; private set; }
    public bool GoodToken { get; private set; }
    public bool IsReady => !_disposed && _sdSettings.CurrentValue.TokenErrorTimestamp < SMDT.UtcNow;

    public HttpService(
        IHttpClientFactory httpClientFactory,
        ILogger<HttpService> logger,
        IOptionsMonitor<SDSettings> sdSettings,
        IDataRefreshService dataRefreshService)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sdSettings = sdSettings ?? throw new ArgumentNullException(nameof(sdSettings));
        _dataRefreshService = dataRefreshService ?? throw new ArgumentNullException(nameof(dataRefreshService));
    }

    public async Task<T?> SendRequestAsync<T>(APIMethod method, string endpoint, object? payload = null, CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(HttpService));
        }

        if (!_sdSettings.CurrentValue.SDEnabled)
        {
            return default;
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

            using var httpClient = GetHttpClient();
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

    public async Task<HttpResponseMessage> SendRawRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(HttpService));
        }

        await ValidateTokenAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        try
        {
            using var httpClient = GetHttpClient();
            var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

            var clonedRequest = CloneHttpRequest(request);
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

                    case SDHttpResponseCode.ACCOUNT_DISABLED:
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
                        response.StatusCode = HttpStatusCode.TooManyRequests;
                        response.ReasonPhrase = "Too Many Requests";
                        break;

                    case SDHttpResponseCode.TOKEN_MISSING:
                    case SDHttpResponseCode.TOKEN_INVALID:
                    case SDHttpResponseCode.INVALID_USER:
                    case SDHttpResponseCode.TOKEN_EXPIRED:
                    case SDHttpResponseCode.TOKEN_DUPLICATED:
                    case SDHttpResponseCode.UNKNOWN_USER:
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

            using var httpClient = GetHttpClient();
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

            using var response = await httpClient.PostAsJsonAsync(
                "token",
                new { username, password },
                linkedCts.Token
            ).ConfigureAwait(false);

            TokenResponse? tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: linkedCts.Token);
            if (response.IsSuccessStatusCode)
            {
                if (tokenResponse?.Code == 0 && !string.IsNullOrEmpty(tokenResponse.Token))
                {
                    Token = tokenResponse.Token;
                    TokenTimestamp = tokenResponse.Datetime;
                    GoodToken = true;

                    _logger.LogInformation("Token refreshed successfully. Token={Token}...",
                        Token?[..Math.Min(5, Token.Length)]);
                    _sdSettings.CurrentValue.TokenErrorTimestamp = DateTime.MinValue;
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
                tokenResponse.Code == (int)SDHttpResponseCode.ACCOUNT_DISABLED
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
        Token = null;
        GoodToken = false;
        TokenTimestamp = DateTime.MinValue;
        _logger.LogWarning("Token cleared.");
    }

    private HttpClient GetHttpClient()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(HttpService));
        }

        var httpClient = _httpClientFactory.CreateClient(nameof(HttpService));

        if (!string.IsNullOrEmpty(Token))
        {
            httpClient.DefaultRequestHeaders.Remove("token");
            httpClient.DefaultRequestHeaders.Add("token", Token);
        }

        httpClient.DefaultRequestHeaders.UserAgent.Clear();
        string userAgent = _sdSettings.CurrentValue.UserAgent ?? "StreamMaster/1.0";
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);

        httpClient.Timeout = TimeSpan.FromSeconds(30);

        return httpClient;
    }

    private static HttpRequestMessage CloneHttpRequest(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Content = request.Content,
            Version = request.Version
        };

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
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