using MediatR;

using Microsoft.Extensions.DependencyInjection;

using StreamMasterApplication.Common.Behaviours;
using StreamMasterApplication.Icons.Commands;

namespace StreamMasterApplication;

public static class ConfigureServices
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        _ = services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehaviour<,>));
        _ = services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        _ = services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehaviour<,>));
        _ = services.AddHttpClient<IRequestHandler<CacheIconsFromEPGsRequest, bool>>((httpClient) =>
        {
            string userAgentString = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/110.0.0.0 Safari/537.36 Edg/110.0.1587.57";
            httpClient.DefaultRequestHeaders.Add("User-Agent", userAgentString);
        });

        return services;
    }
}