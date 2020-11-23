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
        /// <param name="apiKey">The unique API key obtained from OpenWeatherMap.</param>
        /// <param name="apiCachePeriod">The number of milliseconds to cache for.</param>
        /// <param name="resiliencyPeriod">The number of milliseconds to keep on using cache values if API is unavailable.</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddOpenWeatherMapCache(this IServiceCollection services, string apiKey, int apiCachePeriod, int resiliencyPeriod)
        {
            services.AddSingleton<IOpenWeatherMapCache>(new OpenWeatherMapCache(apiKey, apiCachePeriod, resiliencyPeriod));
            return services;
        }

        /// <summary>
        /// Adds a singleton service for OpenWeatherMap.Cache for Dependency Injection, using the default resiliency period of 5 minutes.
        /// </summary>
        /// <param name="apiKey">The unique API key obtained from OpenWeatherMap.</param>
        /// <param name="apiCachePeriod">The number of milliseconds to cache for.</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddOpenWeatherMapCache(this IServiceCollection services, string apiKey, int apiCachePeriod)
        {
            services.AddSingleton<IOpenWeatherMapCache>(new OpenWeatherMapCache(apiKey, apiCachePeriod));
            return services;
        }
    }
}
