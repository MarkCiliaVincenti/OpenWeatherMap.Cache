using Microsoft.Extensions.Caching.Memory;
using OpenWeatherMap.Cache.Models;
using System;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Text.Json;

namespace OpenWeatherMap.Cache
{
    /// <summary>
    /// Interface for <see cref="OpenWeatherMapCache"/>
    /// </summary>
    public interface IOpenWeatherMapCache
    {
        /// <inheritdoc cref="OpenWeatherMapCache.TryGetReadings"/>
        bool TryGetReadings(Location location, out Readings readings);
    }
    /// <summary>
    /// Class for OpenWeatherMapCache
    /// </summary>
    public class OpenWeatherMapCache : IOpenWeatherMapCache
    {
        private const int DefaultResiliencyPeriod = 300_000;
        private const int DefaultTimeout = 5_000;

        private readonly string _apiKey;
        private readonly int _apiCachePeriod;
        private readonly int _resiliencyPeriod;
        private readonly int _timeout;
        private readonly object apiReadingsLock = new object();
        private readonly MemoryCache _memoryCache;

        /// <summary>
        /// Initializes a new instance of <see cref="OpenWeatherMapCache"/>.
        /// </summary>
        /// <param name="apiKey">The unique API key obtained from OpenWeatherMap.</param>
        /// <param name="apiCachePeriod">The number of milliseconds to cache for.</param>
        /// <param name="resiliencyPeriod">The number of milliseconds to keep on using cache values if API is unavailable. Default 5 minutes.</param>
        /// <param name="timeout">The number of milliseconds for the <see cref="WebRequest"/> timeout. Default 5 seconds.</param>
        public OpenWeatherMapCache(string apiKey, int apiCachePeriod, int resiliencyPeriod = DefaultResiliencyPeriod, int timeout = DefaultTimeout)
        {
            _apiKey = apiKey;
            _apiCachePeriod = apiCachePeriod;
            _resiliencyPeriod = resiliencyPeriod;
            _timeout = timeout;
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
        }

        private ApiWeatherResult GetApiWeatherResultFromUri(string uri, int timeout)
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

            using (var response = (HttpWebResponse)request.GetResponse())
            using (Stream responseStream = response.GetResponseStream())
            using (StreamReader myStreamReader = new StreamReader(responseStream, Encoding.UTF8))
            {
                string responseJSON = myStreamReader.ReadToEnd();
                return JsonSerializer.Deserialize<ApiWeatherResult>(responseJSON);
            }
        }

        /// <summary>
        /// Attempts to get the readings for the provided <see cref="Location"/>.
        /// </summary>
        /// <param name="location">The <see cref="Location"/> for which to get the readings.</param>
        /// <param name="readings">When this method returns, contains the <see cref="Readings"/> object for the provided location, or the default value if the operation failed.</param>
        /// <returns>true if the operation was successful; otherwise, false.</returns>
        public bool TryGetReadings(Location location, out Readings readings)
        {
            lock (apiReadingsLock)
            {
                var dateTime = DateTime.UtcNow;
                var found = _memoryCache.TryGetValue(location, out Readings apiCache);
                if (found && dateTime.Subtract(apiCache.FetchedTime).TotalMilliseconds < _apiCachePeriod)
                {
                    apiCache.IsFromCache = true;
                    readings = apiCache;
                    return true;
                }

                string apiUrl = $"https://api.openweathermap.org/data/2.5/weather?lat={location.Latitude}&lon={location.Longtitude}&appid={_apiKey}&cache={Guid.NewGuid()}";

                try
                {
                    var apiWeatherResult = GetApiWeatherResultFromUri(apiUrl, _timeout);
                    var newValue = new Readings(apiWeatherResult);

                    if (!found || !apiCache.IsSuccessful || (newValue.FetchedTime > apiCache.FetchedTime && newValue.MeasuredTime >= apiCache.MeasuredTime))
                    {
                        _memoryCache.Set(location, newValue, new MemoryCacheEntryOptions
                        {
                            SlidingExpiration = TimeSpan.FromMilliseconds(_resiliencyPeriod)
                        });
                        readings = newValue;
                    }
                    else
                    {
                        // either readings are unchanged or reverted back to older values, so use the newer values in cache
                        apiCache.IsFromCache = true;
                        readings = apiCache;
                    }

                    return true;
                }
                catch
                {
                    if (found && apiCache.IsSuccessful && dateTime.Subtract(apiCache.FetchedTime).TotalMilliseconds <= _resiliencyPeriod)
                    {
                        apiCache.IsFromCache = true;
                        readings = apiCache;
                        return true;
                    }

                    readings = new Readings();
                    return false;
                }
            }
        }
    }
}
