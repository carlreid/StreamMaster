using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;

namespace StreamMaster.SchedulesDirect.Services;

public static class ConfigureServices
{
    public static IServiceCollection AddSchedulesDirectAPIServices(this IServiceCollection services)
    {
        services.AddHttpClient(nameof(SchedulesDirectHttpService), client =>
        {
            client.BaseAddress = new Uri("https://json.schedulesdirect.org/20141201/");
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
            client.DefaultRequestHeaders.ExpectContinue = true;
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            AllowAutoRedirect = true,
        })
        .AddHttpMessageHandler<TokenHandler>()
        .AddHttpMessageHandler<RedirectTokenPreservingHandler>();

        return services
            .AddSingleton<ITokenStore, TokenStore>()
            .AddTransient<RedirectTokenPreservingHandler>()
            .AddTransient<TokenHandler>()
            .AddSingleton<ISchedulesDirectAPIService, SchedulesDirectAPIService>()
            .AddSingleton<ISchedulesDirectRepository, SchedulesDirectRepository>()
            .AddSingleton<IApiErrorManager, ApiErrorManager>()
            .AddSingleton<ISchedulesDirectHttpService, SchedulesDirectHttpService>();
    }
}