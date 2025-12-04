using Microsoft.Extensions.DependencyInjection;

namespace StreamMaster.SchedulesDirect.Services
{
    public class TokenHandler : DelegatingHandler
    {
        private readonly ITokenStore _tokenStore;

        public TokenHandler(ITokenStore tokenStore)
        {
            _tokenStore = tokenStore;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Check if request explicitly opts out of token authentication
            if (request.Options.TryGetValue(new HttpRequestOptionsKey<bool>("SkipTokenAuth"), out bool skipAuth) && skipAuth)
            {
                return await base.SendAsync(request, cancellationToken);
            }

            // Add token if available and not already present
            if (!string.IsNullOrEmpty(_tokenStore.Token) && !request.Headers.Contains("token"))
            {
                request.Headers.Add("token", _tokenStore.Token);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}