using System.Net;

namespace StreamMaster.SchedulesDirect.Services
{
    public class RedirectTokenPreservingHandler : DelegatingHandler
    {
        private readonly ILogger<RedirectTokenPreservingHandler> _logger;

        public RedirectTokenPreservingHandler(ILogger<RedirectTokenPreservingHandler> logger)
        {
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var tokenValue = string.Empty;
            var hasToken = request.Headers.TryGetValues("token", out var tokenValues);
            if (hasToken)
            {
                tokenValue = tokenValues.Single();
                _logger.LogTrace("Request to {Uri} contains token header", request.RequestUri);
            }

            _logger.LogTrace("Sending request to {Uri} with method {Method}", request.RequestUri, request.Method);

            var response = await base.SendAsync(request, cancellationToken);

            _logger.LogTrace("Received response from {Uri} with status {StatusCode}",
                request.RequestUri, response.StatusCode);

            if (IsRedirect(response))
            {
                var redirectLocation = response.Headers.Location;
                _logger.LogTrace("Redirect detected: {StatusCode} from {OriginalUri} to {RedirectUri}",
                    response.StatusCode, request.RequestUri, redirectLocation);

                if (!string.IsNullOrEmpty(tokenValue))
                {
                    _logger.LogDebug("Preserving token header during redirect from {OriginalUri} to {RedirectUri}",
                        request.RequestUri, redirectLocation);

                    var newRequest = new HttpRequestMessage(request.Method, redirectLocation);

                    var headerCount = 0;
                    foreach (var header in request.Headers)
                    {
                        newRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                        headerCount++;
                    }

                    newRequest.Headers.TryAddWithoutValidation("token", tokenValue);

                    _logger.LogDebug("Copied {HeaderCount} headers to redirect request, including preserved token", headerCount);

                    response.Dispose();
                    return await SendAsync(newRequest, cancellationToken);
                }
                else
                {
                    _logger.LogWarning("Redirect detected but no token to preserve from {OriginalUri} to {RedirectUri}",
                        request.RequestUri, redirectLocation);
                }
            }

            return response;
        }

        private static bool IsRedirect(HttpResponseMessage response) =>
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.MovedPermanently ||
            response.StatusCode == HttpStatusCode.Found ||
            response.StatusCode == HttpStatusCode.TemporaryRedirect ||
            response.StatusCode == HttpStatusCode.PermanentRedirect;
    }
}