using Microsoft.Extensions.Caching.Memory;
using OpenWeatherMap.Cache.Constants;
using OpenWeatherMap.Cache.Helpers;
using OpenWeatherMap.Cache.Models;
using System;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Text.Json;
using System.Threading.Tasks;
using static OpenWeatherMap.Cache.Enums;

namespace OpenWeatherMap.Cache
{
    /// <summary>
    /// Interface for <see cref="OpenWeatherMapCache"/>
    /// </summary>
    public interface IOpenWeatherMapCache
    {
        /// <inheritdoc cref="OpenWeatherMapCache.GetReadingsAsync"/>
        Task<Readings> GetReadingsAsync<T>(T locationQuery) where T : LocationQuery;
    }
    /// <summary>
    /// Class for OpenWeatherMapCache
    /// </summary>
    public class OpenWeatherMapCache : IOpenWeatherMapCache
    {
        private readonly string _apiKey;
        private readonly int _apiCachePeriod;
        private readonly FetchMode _fetchMode;
        private readonly int _resiliencyPeriod;
        private readonly int _timeout;
        private readonly string _logPath;
        private readonly MemoryCache _memoryCache;
        private readonly AsyncDuplicateLock _asyncDuplicateLock;

        /// <summary>
        /// Initializes a new instance of <see cref="OpenWeatherMapCache"/>.
        /// </summary>
        /// <param name="apiKey">The unique API key obtained from OpenWeatherMap.</param>
        /// <param name="apiCachePeriod">The number of milliseconds to cache for.</param>
        /// <param name="fetchMode">The mode of operation. Defaults to <see cref="FetchMode.AlwaysUseLastMeasuredButExtendCache"/>.</param>
        /// <param name="resiliencyPeriod">The number of milliseconds to keep on using cache values if API is unavailable. Defaults to <see cref="OpenWeatherMapCacheDefaults.DefaultResiliencyPeriod"/>.</param>
        /// <param name="timeout">The number of milliseconds for the <see cref="WebRequest"/> timeout. Defaults to <see cref="OpenWeatherMapCacheDefaults.DefaultTimeout"/>.</param>
        /// <param name="logPath">Logs the latest result for a given location to file. Defaults to null (disabled).</param>
        public OpenWeatherMapCache(string apiKey, int apiCachePeriod, FetchMode fetchMode = FetchMode.AlwaysUseLastMeasuredButExtendCache, int resiliencyPeriod = OpenWeatherMapCacheDefaults.DefaultResiliencyPeriod, int timeout = OpenWeatherMapCacheDefaults.DefaultTimeout, string logPath = null)
        {
            _apiKey = apiKey;
            _apiCachePeriod = apiCachePeriod;
            _fetchMode = fetchMode;
            _resiliencyPeriod = resiliencyPeriod;
            _timeout = timeout;
            _logPath = logPath;
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _asyncDuplicateLock = new AsyncDuplicateLock();
        }

        private HttpWebRequest BuildHttpWebRequest(string uri, int timeout)
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
            request.ReadWriteTimeout = timeout;
            return request;
        }

        private async Task<ApiWeatherResult> GetApiWeatherResultFromUri(Location location, string uri, int timeout)
        {
            try
            {
                var request = BuildHttpWebRequest(uri, timeout);

                using (var response = (HttpWebResponse)await request.GetResponseAsync())
                {
                    using (var streamReader = new StreamReader(response.GetResponseStream()))
                    {
                        var content = await streamReader.ReadToEndAsync();
                        if (_logPath != null)
                            File.WriteAllText(Path.Combine(_logPath, $"{location.Latitude.ToString().Replace('.', '_')}-{location.Longitude.ToString().Replace('.', '_')}.json"), content);
                        return JsonSerializer.Deserialize<ApiWeatherResult>(content);
                    }
                }
            }
            catch (WebException webException)
            {
                if (webException.Status == WebExceptionStatus.ProtocolError)
                {
                    ApiErrorResult errorResult;
                    try
                    {
                        errorResult = await JsonSerializer.DeserializeAsync<ApiErrorResult>(webException.Response.GetResponseStream());
                    }
                    catch
                    {
                        throw new OpenWeatherMapCacheException("Could not deserialize JSON content");
                    }
                    throw new OpenWeatherMapCacheException(errorResult);
                }
                else
                {
                    throw new OpenWeatherMapCacheException(webException.Message);
                }
            }
            catch (JsonException)
            {
                throw new OpenWeatherMapCacheException("Could not deserialize JSON content");
            }
            catch (Exception exception)
            {
                throw new OpenWeatherMapCacheException(exception.Message);
            }
        }

        private async Task<ApiWeatherResult> GetApiWeatherResultFromUri(ZipCode zipCode, string uri, int timeout)
        {
            try
            {
                var request = BuildHttpWebRequest(uri, timeout);

                using (var response = (HttpWebResponse)await request.GetResponseAsync())
                {
                    using (var streamReader = new StreamReader(response.GetResponseStream()))
                    {
                        var content = await streamReader.ReadToEndAsync();
                        if (_logPath != null)
                            File.WriteAllText(Path.Combine(_logPath, $"{zipCode.Zip}-{zipCode.CountryCode}.json"), content);
                        return JsonSerializer.Deserialize<ApiWeatherResult>(content);
                    }
                }
            }
            catch (WebException webException)
            {
                if (webException.Status == WebExceptionStatus.ProtocolError)
                {
                    ApiErrorResult errorResult;
                    try
                    {
                        errorResult = await JsonSerializer.DeserializeAsync<ApiErrorResult>(webException.Response.GetResponseStream());
                    }
                    catch
                    {
                        throw new OpenWeatherMapCacheException("Could not deserialize JSON content");
                    }
                    throw new OpenWeatherMapCacheException(errorResult);
                }
                else
                {
                    throw new OpenWeatherMapCacheException(webException.Message);
                }
            }
            catch (JsonException)
            {
                throw new OpenWeatherMapCacheException("Could not deserialize JSON content");
            }
            catch (Exception exception)
            {
                throw new OpenWeatherMapCacheException(exception.Message);
            }
        }

        private async Task<ApiWeatherResult> GetApiWeatherResultFromLocationQuery<T>(T locationQuery) where T : LocationQuery
        {
            if (locationQuery is Location location)
            {
                var apiUrl = $"https://api.openweathermap.org/data/2.5/weather?lat={location.Latitude}&lon={location.Longitude}&appid={_apiKey}&cache={Guid.NewGuid()}";
                return await GetApiWeatherResultFromUri(location, apiUrl, _timeout);
            }
            else if (locationQuery is ZipCode zipCode)
            {
                var apiUrl = $"https://api.openweathermap.org/data/2.5/weather?zip={zipCode.Zip},{zipCode.CountryCode}&appid={_apiKey}&cache={Guid.NewGuid()}";
                return await GetApiWeatherResultFromUri(zipCode, apiUrl, _timeout);
            }
            else throw new ArgumentException();
        }

        /// <summary>
        /// Attempts to get the readings for the provided <see cref="Location"/> or <see cref="ZipCode"/>.
        /// </summary>
        /// <param name="locationQuery">The <see cref="Location"/> or <see cref="ZipCode"/> for which to get the readings.</param>
        /// <returns>A <see cref="Readings"/> object for the provided location, or the default value if the operation failed (<see cref="Readings.IsSuccessful"/> = false).</returns>
        public async Task<Readings> GetReadingsAsync<T>(T locationQuery) where T : LocationQuery
        {
            var lockObj = await _asyncDuplicateLock.LockAsync(locationQuery);

            var dateTime = DateTime.UtcNow;
            var found = _memoryCache.TryGetValue(locationQuery, out Readings apiCache);

            if (found)
            {
                var timeElapsed = dateTime.Subtract(apiCache.FetchedTime).TotalMilliseconds;
                if (timeElapsed <= _apiCachePeriod)
                {
                    lockObj.Dispose();
                    apiCache.IsFromCache = true;
                    apiCache.ApiRequestMade = false;
                    return apiCache;
                }
            }

            try
            {
                var apiWeatherResult = await GetApiWeatherResultFromLocationQuery(locationQuery);
                var newValue = new Readings(apiWeatherResult);
                newValue.IsFromCache = false;

                if (!found || !apiCache.IsSuccessful || _fetchMode == FetchMode.AlwaysUseLastFetchedValue || (newValue.MeasuredTime >= apiCache.MeasuredTime))
                {
                    _memoryCache.Set(locationQuery, newValue, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpiration = newValue.FetchedTime.AddMilliseconds(_resiliencyPeriod)
                    });
                    lockObj.Dispose();
                    newValue.ApiRequestMade = true;
                    return newValue;
                }
                else
                {
                    if (_fetchMode == FetchMode.AlwaysUseLastMeasuredButExtendCache)
                    {
                        apiCache.FetchedTime = newValue.FetchedTime;
                        _memoryCache.Set(locationQuery, apiCache, new MemoryCacheEntryOptions
                        {
                            AbsoluteExpiration = apiCache.FetchedTime.AddMilliseconds(_resiliencyPeriod)
                        });
                    }
                    lockObj.Dispose();
                    apiCache.IsFromCache = true;
                    apiCache.ApiRequestMade = true;
                    return apiCache;
                }
            }
            catch (OpenWeatherMapCacheException exception)
            {
                lockObj.Dispose();
                if (found && apiCache.IsSuccessful && dateTime.Subtract(apiCache.FetchedTime).TotalMilliseconds <= _resiliencyPeriod)
                {
                    apiCache.IsFromCache = true;
                    apiCache.ApiRequestMade = false;
                    return apiCache;
                }

                return new Readings(exception);
            }
        }
    }
}
