using Microsoft.Extensions.DependencyInjection;
using OpenWeatherMap.Cache.Constants;

namespace OpenWeatherMap.Cache.Extensions
{
    /// <summary>
    /// Extension class for <see cref="IServiceCollection"/>.
    /// </summary>
    public static class IServiceCollectionExtension
    {
        /// <summary>
        /// Adds a singleton service for OpenWeatherMap.Cache for Dependency Injection.
        /// </summary>
        /// <param name="services">The interface being extended</param>
        /// <param name="apiKey">The unique API key obtained from OpenWeatherMap.</param>
        /// <param name="apiCachePeriod">The number of milliseconds to cache for.</param>
        /// <param name="resiliencyPeriod">The number of milliseconds to keep on using cache values if API is unavailable. Defaults to <see cref="OpenWeatherMapCacheDefaults.DefaultResiliencyPeriod"/></param>
        /// /// <param name="timeout">The number of milliseconds for the <see cref="System.Net.WebRequest"/> timeout. Defaults to <see cref="OpenWeatherMapCacheDefaults.DefaultTimeout"/></param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddOpenWeatherMapCache(this IServiceCollection services, string apiKey, int apiCachePeriod, int resiliencyPeriod = OpenWeatherMapCacheDefaults.DefaultResiliencyPeriod, int timeout = OpenWeatherMapCacheDefaults.DefaultTimeout)
        {
            services.AddSingleton<IOpenWeatherMapCache>(new OpenWeatherMapCache(apiKey, apiCachePeriod, resiliencyPeriod, timeout));
            return services;
        }
    }
}
