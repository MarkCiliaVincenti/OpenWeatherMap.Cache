using Newtonsoft.Json.Linq;
using OpenWeatherMap.Cache.Models;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Text;

namespace OpenWeatherMap.Cache
{
    public interface IOpenWeatherMapCache
    {
        /// <inheritdoc cref="OpenWeatherMap.Cache.OpenWeatherMapCache.TryGetReadings"/>
        bool TryGetReadings(Location location, out Readings readings);
    }
    public class OpenWeatherMapCache : IOpenWeatherMapCache
    {
        private const int defaultResiliencyPeriod = 300_000;

        private readonly string _apiKey;
        private readonly int _apiCachePeriod;
        private readonly int _resiliencyPeriod;
        private readonly object apiReadingsLock = new object();
        private ConcurrentDictionary<Location, Readings> dictCache = new ConcurrentDictionary<Location, Readings>(new Location.EqualityComparer());

        /// <summary>
        /// Initializes a new instance of <see cref="OpenWeatherMapCache"/> with the default resiliency of 5 minutes.
        /// </summary>
        /// <param name="apiKey">The unique API key obtained from OpenWeatherMap.</param>
        /// <param name="apiCachePeriod">The number of milliseconds to cache for.</param>
        public OpenWeatherMapCache(string apiKey, int apiCachePeriod)
        {
            _apiKey = apiKey;
            _apiCachePeriod = apiCachePeriod;
            _resiliencyPeriod = defaultResiliencyPeriod;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="OpenWeatherMapCache"/>.
        /// </summary>
        /// <param name="apiKey">The unique API key obtained from OpenWeatherMap.</param>
        /// <param name="apiCachePeriod">The number of milliseconds to cache for.</param>
        /// <param name="resiliencyPeriod">The number of milliseconds to keep on using cache values if API is unavailable.</param>
        public OpenWeatherMapCache(string apiKey, int apiCachePeriod, int resiliencyPeriod)
        {
            _apiKey = apiKey;
            _apiCachePeriod = apiCachePeriod;
            _resiliencyPeriod = resiliencyPeriod;
        }

        private JObject GetJObjectFromUri(string uri)
        {
            var request = WebRequest.CreateHttp(uri);
            request.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
            request.AllowAutoRedirect = true;
            request.CookieContainer = new CookieContainer();
            request.AllowReadStreamBuffering = true;
            request.Date = DateTime.UtcNow;
            request.IfModifiedSince = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            request.Accept = "application/json";
            request.KeepAlive = false;
            request.ProtocolVersion = new Version(1, 1);
            request.UserAgent = null;
            request.Method = "GET";
            request.Timeout = 5000;

            using (var response = (HttpWebResponse)request.GetResponse())
                using (Stream responseStream = response.GetResponseStream())
                    using (StreamReader myStreamReader = new StreamReader(responseStream, Encoding.UTF8))
                    {
                        string responseJSON = myStreamReader.ReadToEnd();
                        return JObject.Parse(responseJSON);
                    }
        }

        /// <summary>
        /// Attempts to get the readings for the provided <see cref="OpenWeatherMap.Cache.Models.Location"/>.
        /// </summary>
        /// <param name="location">The <see cref="OpenWeatherMap.Cache.Models.Location"/> for which to get the readings.</param>
        /// <param name="readings">When this method returns, contains the <see cref="OpenWeatherMap.Cache.Models.Readings"/> object for the provided location, or the default value if the operation failed.</param>
        /// <returns>true if the operation was successful; otherwise, false.</returns>
        public bool TryGetReadings(Location location, out Readings readings)
        {
            lock (apiReadingsLock)
            {
                var dateTime = DateTime.UtcNow;
                var found = dictCache.TryGetValue(location, out var apiCache);
                if (found && dateTime.Subtract(apiCache.FetchedTime).TotalMilliseconds < _apiCachePeriod)
                {
                    readings = apiCache;
                    return true;
                }

                string apiUrl = $"https://api.openweathermap.org/data/2.5/weather?lat={location.Latitude}&lon={location.Longtitude}&appid={_apiKey}&units=metric&cache={Guid.NewGuid()}";

                try
                {
                    var jObject = GetJObjectFromUri(apiUrl);
                    var jToken = jObject["main"];

                    var newValue = new Readings(
                        temperature: jToken["temp"].Value<double>(),
                        humidity: jToken["humidity"].Value<double>(),
                        pressure: jToken["pressure"].Value<double>(),
                        calcuatedTime: DateTimeOffset.FromUnixTimeSeconds(jObject["dt"].Value<long>()).UtcDateTime
                    );

                    if (!found)
                    {
                        dictCache.TryAdd(location, newValue);
                        readings = newValue;
                    }
                    else if (!apiCache.IsSuccessful || (newValue.FetchedTime > apiCache.FetchedTime && newValue.CalculatedTime >= apiCache.CalculatedTime))
                    {
                        dictCache.TryUpdate(location, newValue, apiCache);
                        readings = newValue;
                    }
                    else
                    {
                        // either readings are unchanged or reverted back to older values, so use the newer values in cache
                        readings = apiCache;
                    }

                    return true;
                }
                catch
                {
                    if (found && apiCache.IsSuccessful && apiCache.CalculatedTime.AddMilliseconds(_resiliencyPeriod) >= dateTime)
                    {
                        readings = apiCache;
                        return true;
                    }

                    var newValue = new Readings(dateTime);

                    if (!found)
                        dictCache.TryAdd(location, newValue);
                    else if (apiCache != newValue)
                        dictCache.TryUpdate(location, newValue, apiCache);
                    readings = newValue;
                    return false;
                }
            }
        }
    }
}
