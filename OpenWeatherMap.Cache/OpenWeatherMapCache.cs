using AsyncKeyedLock;
using Microsoft.Extensions.Caching.Memory;
using OpenWeatherMap.Cache.Constants;
using OpenWeatherMap.Cache.Models;
using System;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Text.Json;
using System.Threading;
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
        Task<Readings> GetReadingsAsync<T>(T locationQuery, CancellationToken cancellationToken = default) where T : ILocationQuery;

        /// <inheritdoc cref="OpenWeatherMapCache.GetReadings"/>
        Readings GetReadings<T>(T locationQuery) where T : ILocationQuery;

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
        private readonly AsyncKeyedLocker<ILocationQuery> _asyncKeyedLocker;

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
            _asyncKeyedLocker = new AsyncKeyedLocker<ILocationQuery>();
        }

        private static HttpWebRequest BuildHttpWebRequest(string uri, int timeout)
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

        private static async Task<HttpWebResponse> GetResponseAsync(HttpWebRequest request, CancellationToken cancellationToken)
        {
            if (cancellationToken == default)
            {
                var response = await request.GetResponseAsync().ConfigureAwait(false);
                return (HttpWebResponse)response;
            }
            using (cancellationToken.Register(request.Abort, false))
            {
                var response = await request.GetResponseAsync().ConfigureAwait(false);
                return (HttpWebResponse)response;
            }
        }

        private async Task<ApiWeatherResult> GetApiWeatherResultFromUri(string logFileName, string uri, int timeout, CancellationToken cancellationToken)
        {
            var request = BuildHttpWebRequest(uri, timeout);

            try
            {
                using (var response = await GetResponseAsync(request, cancellationToken).ConfigureAwait(false))
                {
                    using (var streamReader = new StreamReader(response.GetResponseStream()))
                    {
                        var content = await streamReader.ReadToEndAsync().ConfigureAwait(false);
                        if (_logPath != null)
                            File.WriteAllText(Path.Combine(_logPath, logFileName), content);
                        return JsonSerializer.Deserialize<ApiWeatherResult>(content);
                    }
                }
            }
            catch (WebException webException)
            {
                if (cancellationToken != default && cancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException(webException.Message, webException, cancellationToken);

                if (webException.Status == WebExceptionStatus.ProtocolError)
                {
                    ApiErrorResult errorResult;
                    try
                    {
                        errorResult = await JsonSerializer.DeserializeAsync<ApiErrorResult>(webException.Response.GetResponseStream(), cancellationToken: cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        if (cancellationToken != default && cancellationToken.IsCancellationRequested)
                            throw new OperationCanceledException(ex.Message, ex, cancellationToken);

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

        private async Task<ApiWeatherResult> GetApiWeatherResultFromUri(Location location, string uri, int timeout, CancellationToken cancellationToken)
        {
            var fileName = $"{location.Latitude.ToString().Replace('.', '_')}-{location.Longitude.ToString().Replace('.', '_')}.json";
            return await GetApiWeatherResultFromUri(fileName, uri, timeout, cancellationToken).ConfigureAwait(false);
        }

        private async Task<ApiWeatherResult> GetApiWeatherResultFromUri(ZipCode zipCode, string uri, int timeout, CancellationToken cancellationToken)
        {
            var fileName = $"{zipCode.Zip}-{zipCode.CountryCode}.json";
            return await GetApiWeatherResultFromUri(fileName, uri, timeout, cancellationToken).ConfigureAwait(false);
        }

        private async Task<ApiWeatherResult> GetApiWeatherResultFromLocationQuery<T>(T locationQuery, CancellationToken cancellationToken) where T : ILocationQuery
        {
            if (locationQuery is Location location)
            {
                var apiUrl = $"https://api.openweathermap.org/data/2.5/weather?lat={location.Latitude}&lon={location.Longitude}&appid={_apiKey}&cache={Guid.NewGuid()}";
                return await GetApiWeatherResultFromUri(location, apiUrl, _timeout, cancellationToken).ConfigureAwait(false);
            }

            if (locationQuery is ZipCode zipCode)
            {
                var apiUrl = $"https://api.openweathermap.org/data/2.5/weather?zip={zipCode.Zip},{zipCode.CountryCode}&appid={_apiKey}&cache={Guid.NewGuid()}";
                return await GetApiWeatherResultFromUri(zipCode, apiUrl, _timeout, cancellationToken).ConfigureAwait(false);
            }

            throw new ArgumentException("Unsupported type provided", nameof(locationQuery));
        }

        /// <summary>
        /// Attempts to get the readings for the provided <see cref="Location"/> or <see cref="ZipCode"/>.
        /// </summary>
        /// <param name="locationQuery">The <see cref="Location"/> or <see cref="ZipCode"/> for which to get the readings.</param>
        /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
        /// <returns>A <see cref="Readings"/> object for the provided location, or the default value if the operation failed (<see cref="Readings.IsSuccessful"/> = false).</returns>
        public async Task<Readings> GetReadingsAsync<T>(T locationQuery, CancellationToken cancellationToken = default) where T : ILocationQuery
        {
            var lockObj = await _asyncKeyedLocker.LockAsync(locationQuery, cancellationToken).ConfigureAwait(false);

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
                var apiWeatherResult = await GetApiWeatherResultFromLocationQuery(locationQuery, cancellationToken).ConfigureAwait(false);
                var newValue = new Readings(apiWeatherResult)
                {
                    IsFromCache = false
                };

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

        /// <summary>
        /// Attempts to get the readings for the provided <see cref="Location"/> or <see cref="ZipCode"/> by calling GetReadingsAsync synchronously (not ideal).
        /// </summary>
        /// <param name="locationQuery">The <see cref="Location"/> or <see cref="ZipCode"/> for which to get the readings.</param>
        /// <returns>A <see cref="Readings"/> object for the provided location, or the default value if the operation failed (<see cref="Readings.IsSuccessful"/> = false).</returns>
        public Readings GetReadings<T>(T locationQuery) where T : ILocationQuery
        {
            return GetReadingsAsync<T>(locationQuery).GetAwaiter().GetResult();
        }
    }
}
