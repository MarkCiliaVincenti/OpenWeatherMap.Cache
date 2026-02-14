// Copyright (c) All contributors.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using OpenWeatherMap.Cache.Constants;
using static OpenWeatherMap.Cache.Enums;

namespace OpenWeatherMap.Cache.Extensions;

/// <summary>
/// Extension class for <see cref="IServiceCollection"/>.
/// </summary>
public static class IServiceCollectionExtension
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds a singleton service for OpenWeatherMap.Cache for Dependency Injection.
        /// </summary>
        /// <param name="apiKey">The unique API key obtained from OpenWeatherMap.</param>
        /// <param name="apiCachePeriod">The number of milliseconds to cache for.</param>
        /// <param name="fetchMode">The mode of operation. Defaults to <see cref="FetchMode.AlwaysUseLastMeasuredButExtendCache"/>.</param>
        /// <param name="resiliencyPeriod">The number of milliseconds to keep on using cache values if API is unavailable. Defaults to <see cref="OpenWeatherMapCacheDefaults.DefaultResiliencyPeriod"/></param>
        /// <param name="timeout">The number of milliseconds for the <see cref="System.Net.WebRequest"/> timeout. Defaults to <see cref="OpenWeatherMapCacheDefaults.DefaultTimeout"/></param>
        /// <param name="logPath">Logs the latest result for a given location to file. Defaults to null (disabled).</param>
        /// <returns>The service collection</returns>
        public IServiceCollection AddOpenWeatherMapCache(string apiKey, int apiCachePeriod, FetchMode fetchMode = FetchMode.AlwaysUseLastMeasuredButExtendCache, int resiliencyPeriod = OpenWeatherMapCacheDefaults.DefaultResiliencyPeriod, int timeout = OpenWeatherMapCacheDefaults.DefaultTimeout, string logPath = null)
        {
            services.AddSingleton<IOpenWeatherMapCache>(provider =>
                new OpenWeatherMapCache(apiKey, apiCachePeriod, fetchMode, resiliencyPeriod, timeout, logPath));
            return services;
        }
    }
}
