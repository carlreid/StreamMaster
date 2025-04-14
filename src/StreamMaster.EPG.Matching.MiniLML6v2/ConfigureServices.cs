using Microsoft.Extensions.DependencyInjection;
using StreamMaster.Domain.EPG;

namespace StreamMaster.EPG.Matching.MiniLML6v2;

public static class ConfigureServices
{
    public static IServiceCollection AddMiniLML6v2EPGMatcherServices(this IServiceCollection services)
    {
        services.AddScoped<IEpgMatcher, MiniLML6v2EPGMatcher>();
        return services;
    }
}