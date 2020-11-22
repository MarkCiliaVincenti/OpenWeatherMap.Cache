using Microsoft.Extensions.DependencyInjection;

namespace OpenWeatherMap.Cache.Extensions
{
    public static class IServiceCollectionExtension
    {
        public static IServiceCollection AddOpenWeatherMapCache(this IServiceCollection services, string apiKey, int apiCachePeriod, int resiliencyPeriod, int apiServerSyncPeriod)
        {
            services.AddSingleton<IOpenWeatherMapCache>(new OpenWeatherMapCache(apiKey, apiCachePeriod, resiliencyPeriod, apiServerSyncPeriod));
            return services;
        }
    }
}
