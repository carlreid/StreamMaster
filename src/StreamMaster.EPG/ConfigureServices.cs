using Microsoft.Extensions.DependencyInjection;
using StreamMaster.Domain.EPG;

namespace StreamMaster.EPG;

public static class ConfigureServices
{
    public static IServiceCollection AddFuzzyEPGMatcherServices(this IServiceCollection services)
    {
        services.AddScoped<IEpgMatcher, FuzzyEpgMatcher>();
        return services;
    }
}