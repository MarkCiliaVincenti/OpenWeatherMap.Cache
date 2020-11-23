using Microsoft.Extensions.DependencyInjection;

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
        /// <param name="resiliencyPeriod">The number of milliseconds to keep on using cache values if API is unavailable. Default 5 minutes</param>
        /// /// <param name="timeout">The number of milliseconds for the <see cref="System.Net.WebRequest"/> timeout. Default 5 seconds</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddOpenWeatherMapCache(this IServiceCollection services, string apiKey, int apiCachePeriod, int resiliencyPeriod = 300_000, int timeout = 5_000)
        {
            services.AddSingleton<IOpenWeatherMapCache>(new OpenWeatherMapCache(apiKey, apiCachePeriod, resiliencyPeriod, timeout));
            return services;
        }
    }
}
