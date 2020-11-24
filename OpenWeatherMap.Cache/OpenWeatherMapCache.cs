using Microsoft.Extensions.Caching.Memory;
using OpenWeatherMap.Cache.Constants;
using OpenWeatherMap.Cache.Models;
using System;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OpenWeatherMap.Cache
{
    /// <summary>
    /// Interface for <see cref="OpenWeatherMapCache"/>
    /// </summary>
    public interface IOpenWeatherMapCache
    {
        /// <inheritdoc cref="OpenWeatherMapCache.GetReadingsAsync"/>
        Task<Readings> GetReadingsAsync(Location location);
    }
    /// <summary>
    /// Class for OpenWeatherMapCache
    /// </summary>
    public class OpenWeatherMapCache : IOpenWeatherMapCache
    {
        private readonly string _apiKey;
        private readonly int _apiCachePeriod;
        private readonly int _resiliencyPeriod;
        private readonly int _timeout;
        private readonly MemoryCache _memoryCache;
        private readonly SemaphoreSlim _apiReadingsSemaphoreSlim = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Initializes a new instance of <see cref="OpenWeatherMapCache"/>.
        /// </summary>
        /// <param name="apiKey">The unique API key obtained from OpenWeatherMap.</param>
        /// <param name="apiCachePeriod">The number of milliseconds to cache for.</param>
        /// <param name="resiliencyPeriod">The number of milliseconds to keep on using cache values if API is unavailable. Defaults to <see cref="OpenWeatherMapCacheDefaults.DefaultResiliencyPeriod"/>.</param>
        /// <param name="timeout">The number of milliseconds for the <see cref="WebRequest"/> timeout. Defaults to <see cref="OpenWeatherMapCacheDefaults.DefaultTimeout"/>.</param>
        public OpenWeatherMapCache(string apiKey, int apiCachePeriod, int resiliencyPeriod = OpenWeatherMapCacheDefaults.DefaultResiliencyPeriod, int timeout = OpenWeatherMapCacheDefaults.DefaultTimeout)
        {
            _apiKey = apiKey;
            _apiCachePeriod = apiCachePeriod;
            _resiliencyPeriod = resiliencyPeriod;
            _timeout = timeout;
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
        }

        private async Task<ApiWeatherResult> GetApiWeatherResultFromUri(string uri, int timeout)
        {
            var request = WebRequest.CreateHttp(uri);
            request.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
            request.AllowAutoRedirect = true;
            request.CookieContainer = new CookieContainer();
            request.AllowReadStreamBuffering = true;
            request.Date = DateTime.UtcNow;
            request.IfModifiedSince = DateTime.MinValue;
            request.Accept = "application/json";
            request.KeepAlive = false;
            request.ProtocolVersion = new Version(1, 1);
            request.UserAgent = null;
            request.Method = "GET";
            request.Timeout = timeout;

            using (var response = (HttpWebResponse)await request.GetResponseAsync())
                return await JsonSerializer.DeserializeAsync<ApiWeatherResult>(response.GetResponseStream());
        }

        /// <summary>
        /// Attempts to get the readings for the provided <see cref="Location"/>.
        /// </summary>
        /// <param name="location">The <see cref="Location"/> for which to get the readings.</param>
        /// <returns>A <see cref="Readings"/> object for the provided location, or the default value if the operation failed (<see cref="Readings.IsSuccessful"/> = false).</returns>
        public async Task<Readings> GetReadingsAsync(Location location)
        {
            _apiReadingsSemaphoreSlim.Wait();

            var dateTime = DateTime.UtcNow;
            var found = _memoryCache.TryGetValue(location, out Readings apiCache);
            if (found && dateTime.Subtract(apiCache.FetchedTime).TotalMilliseconds < _apiCachePeriod)
            {
                _apiReadingsSemaphoreSlim.Release();
                apiCache.IsFromCache = true;
                return apiCache;
            }

            string apiUrl = $"https://api.openweathermap.org/data/2.5/weather?lat={location.Latitude}&lon={location.Longtitude}&appid={_apiKey}&cache={Guid.NewGuid()}";

            try
            {
                var apiWeatherResult = await GetApiWeatherResultFromUri(apiUrl, _timeout);
                var newValue = new Readings(apiWeatherResult);

                if (!found || !apiCache.IsSuccessful || (newValue.FetchedTime > apiCache.FetchedTime && newValue.MeasuredTime >= apiCache.MeasuredTime))
                {
                    _memoryCache.Set(location, newValue, new MemoryCacheEntryOptions
                    {
                        SlidingExpiration = TimeSpan.FromMilliseconds(_resiliencyPeriod)
                    });
                    _apiReadingsSemaphoreSlim.Release();
                    return newValue;
                }
                else
                {
                    _apiReadingsSemaphoreSlim.Release();
                    // either readings are unchanged or reverted back to older values, so use the newer values in cache
                    apiCache.IsFromCache = true;
                    return apiCache;
                }
            }
            catch
            {
                _apiReadingsSemaphoreSlim.Release();
                if (found && apiCache.IsSuccessful && dateTime.Subtract(apiCache.FetchedTime).TotalMilliseconds <= _resiliencyPeriod)
                {
                    apiCache.IsFromCache = true;
                    return apiCache;
                }

                return new Readings();
            }
        }
    }
}
