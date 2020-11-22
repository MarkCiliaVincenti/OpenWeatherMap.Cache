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
        bool TryGetReadings(Location location, out Readings readings);
    }
    public class OpenWeatherMapCache : IOpenWeatherMapCache
    {
        private const int defaultApiServerSyncPeriod = 600_000;
        private const int defaultResiliencyPeriod = 300_000;

        private readonly string _apiKey;
        private readonly int _apiCachePeriod;
        private readonly int _apiServerSyncPeriod;
        private readonly int _resiliencyPeriod;
        private readonly object apiReadingsLock = new object();
        private ConcurrentDictionary<Location, Readings> dictCache = new ConcurrentDictionary<Location, Readings>(new Location.EqualityComparer());

        public OpenWeatherMapCache(string apiKey, int apiCachePeriod)
        {
            _apiKey = apiKey;
            _apiCachePeriod = apiCachePeriod;
            _apiServerSyncPeriod = defaultApiServerSyncPeriod;
            _resiliencyPeriod = defaultResiliencyPeriod;
        }

        public OpenWeatherMapCache(string apiKey, int apiCachePeriod, int resiliencyPeriod, int apiServerSyncPeriod)
        {
            _apiKey = apiKey;
            _apiCachePeriod = apiCachePeriod;
            _apiServerSyncPeriod = apiServerSyncPeriod;
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
            // Get the stream associated with the response
            using (Stream responseStream = response.GetResponseStream())
            // Get a reader capable of reading the response stream
            using (StreamReader myStreamReader = new StreamReader(responseStream, Encoding.UTF8))
            {
                // Read stream content as string
                string responseJSON = myStreamReader.ReadToEnd();

                return JObject.Parse(responseJSON);
            }

        }

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

                // ... Endpoint
                string apiUrl = $"https://api.openweathermap.org/data/2.5/weather?lat={location.Latitude}&lon={location.Longtitude}&appid={_apiKey}&units=metric&cache={Guid.NewGuid()}";

                // ... Use HttpClient.
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
