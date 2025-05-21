using Microsoft.Extensions.DependencyInjection;

namespace StreamMaster.SchedulesDirect.Services;

public static class ConfigureServices
{
    public static IServiceCollection AddSchedulesDirectAPIServices(this IServiceCollection services)
    {
        return services
            .AddSingleton<ISchedulesDirectAPIService, SchedulesDirectAPIService>()
            .AddSingleton<ISchedulesDirectRepository, SchedulesDirectRepository>()
            .AddSingleton<IApiErrorManager, ApiErrorManager>()
            .AddSingleton<IHttpService, HttpService>();
    }
}