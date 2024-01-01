using AsyncKeyedLock;
using Microsoft.Extensions.Caching.Memory;
using OpenWeatherMap.Cache.Constants;
using OpenWeatherMap.Cache.Models;
using System;
using System.Diagnostics;
using System.Globalization;
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
        ValueTask<Readings> GetReadingsAsync<T>(T locationQuery, CancellationToken cancellationToken = default) where T : ILocationQuery;

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
        private const string BASE_WEATHER_URI = "https://api.openweathermap.org/data/2.5/weather";
        private readonly NumberFormatInfo _numberFormatInfo = new NumberFormatInfo { NumberDecimalSeparator = "_" };

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
            _asyncKeyedLocker = new AsyncKeyedLocker<ILocationQuery>(o =>
            {
                o.PoolSize = 20;
                o.PoolInitialFill = 1;
            });
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

        private static HttpWebResponse GetResponse(HttpWebRequest request)
        {
            var response = request.GetResponse();
            return (HttpWebResponse)response;
        }

        private static async ValueTask<HttpWebResponse> GetResponseAsync(HttpWebRequest request, CancellationToken cancellationToken)
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

        private ApiWeatherResult GetApiWeatherResultFromUri(string logFileName, string uri, int timeout)
        {
            var request = BuildHttpWebRequest(uri, timeout);

            try
            {
                using var response = GetResponse(request);
                using var streamReader = new StreamReader(response.GetResponseStream());
                var content = streamReader.ReadToEnd();
                if (_logPath != null)
                {
                    File.WriteAllText(Path.Combine(_logPath, logFileName), content);
                }
                return JsonSerializer.Deserialize<ApiWeatherResult>(content);
            }
            catch (WebException webException)
            {
                if (webException.Status == WebExceptionStatus.ProtocolError)
                {
                    ApiErrorResult errorResult;
                    try
                    {
                        errorResult = JsonSerializer.Deserialize<ApiErrorResult>(webException.Response.GetResponseStream());
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

        private async ValueTask<ApiWeatherResult> GetApiWeatherResultFromUriAsync(string logFileName, string uri, int timeout, CancellationToken cancellationToken)
        {
            var request = BuildHttpWebRequest(uri, timeout);

            try
            {
                using var response = await GetResponseAsync(request, cancellationToken).ConfigureAwait(false);
                using var streamReader = new StreamReader(response.GetResponseStream());
                var content = await streamReader.ReadToEndAsync().ConfigureAwait(false);
                if (_logPath != null)
                {
                    File.WriteAllText(Path.Combine(_logPath, logFileName), content);
                }
                return JsonSerializer.Deserialize<ApiWeatherResult>(content);
            }
            catch (WebException webException)
            {
                if (cancellationToken != default && cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(webException.Message, webException, cancellationToken);
                }

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
                        {
                            throw new OperationCanceledException(ex.Message, ex, cancellationToken);
                        }

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

        private ApiWeatherResult GetApiWeatherResultFromUri(Location location, string uri, int timeout)
        {
            var fileName = $"{location.Latitude.ToString(_numberFormatInfo)}-{location.Longitude.ToString(_numberFormatInfo)}.json";
            return GetApiWeatherResultFromUri(fileName, uri, timeout);
        }

        private async ValueTask<ApiWeatherResult> GetApiWeatherResultFromUriAsync(Location location, string uri, int timeout, CancellationToken cancellationToken)
        {
            var fileName = $"{location.Latitude.ToString(_numberFormatInfo)}-{location.Longitude.ToString(_numberFormatInfo)}.json";
            return await GetApiWeatherResultFromUriAsync(fileName, uri, timeout, cancellationToken).ConfigureAwait(false);
        }

        private ApiWeatherResult GetApiWeatherResultFromUri(ZipCode zipCode, string uri, int timeout)
        {
            var fileName = $"{zipCode.Zip}-{zipCode.CountryCode}.json";
            return GetApiWeatherResultFromUri(fileName, uri, timeout);
        }

        private async ValueTask<ApiWeatherResult> GetApiWeatherResultFromUriAsync(ZipCode zipCode, string uri, int timeout, CancellationToken cancellationToken)
        {
            var fileName = $"{zipCode.Zip}-{zipCode.CountryCode}.json";
            return await GetApiWeatherResultFromUriAsync(fileName, uri, timeout, cancellationToken).ConfigureAwait(false);
        }

        private ApiWeatherResult GetApiWeatherResultFromLocationQuery<T>(T locationQuery) where T : ILocationQuery
        {
            switch (locationQuery)
            {
                case Location location:
                    var locationApiUrl = $"{BASE_WEATHER_URI}?lat={location.Latitude}&lon={location.Longitude}&appid={_apiKey}&cache={Guid.NewGuid()}";
                    return GetApiWeatherResultFromUri(location, locationApiUrl, _timeout);

                case ZipCode zipCode:
                    var zipCodeApiUrl = $"{BASE_WEATHER_URI}?zip={zipCode.Zip},{zipCode.CountryCode}&appid={_apiKey}&cache={Guid.NewGuid()}";
                    return GetApiWeatherResultFromUri(zipCode, zipCodeApiUrl, _timeout);

                default:
                    throw new ArgumentException("Unsupported type provided", nameof(locationQuery));
            }
        }

        private ValueTask<ApiWeatherResult> GetApiWeatherResultFromLocationQueryAsync<T>(T locationQuery, CancellationToken cancellationToken) where T : ILocationQuery
        {
            switch (locationQuery)
            {
                case Location location:
                    var locationApiUrl = $"{BASE_WEATHER_URI}?lat={location.Latitude}&lon={location.Longitude}&appid={_apiKey}&cache={Guid.NewGuid()}";
                    return GetApiWeatherResultFromUriAsync(location, locationApiUrl, _timeout, cancellationToken);

                case ZipCode zipCode:
                    var zipCodeApiUrl = $"{BASE_WEATHER_URI}?zip={zipCode.Zip},{zipCode.CountryCode}&appid={_apiKey}&cache={Guid.NewGuid()}";
                    return GetApiWeatherResultFromUriAsync(zipCode, zipCodeApiUrl, _timeout, cancellationToken);

                default:
                    throw new ArgumentException("Unsupported type provided", nameof(locationQuery));
            }
        }

        /// <summary>
        /// Attempts to get the readings for the provided <see cref="Location"/> or <see cref="ZipCode"/>.
        /// </summary>
        /// <param name="locationQuery">The <see cref="Location"/> or <see cref="ZipCode"/> for which to get the readings.</param>
        /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
        /// <returns>A <see cref="Readings"/> object for the provided location, or the default value if the operation failed (<see cref="Readings.IsSuccessful"/> = false).</returns>
        public async ValueTask<Readings> GetReadingsAsync<T>(T locationQuery, CancellationToken cancellationToken = default) where T : ILocationQuery
        {
            using (await _asyncKeyedLocker.LockAsync(locationQuery, cancellationToken, true).ConfigureAwait(true))
            {
                var dateTime = DateTime.UtcNow;
                var found = _memoryCache.TryGetValue(locationQuery, out Readings apiCache);

                if (found)
                {
                    var timeElapsed = dateTime.Subtract(apiCache.FetchedTime).TotalMilliseconds;
                    if (timeElapsed <= _apiCachePeriod)
                    {
                        apiCache.IsFromCache = true;
                        apiCache.ApiRequestMade = false;
                        return apiCache;
                    }
                }

                try
                {
                    var apiWeatherResult = await GetApiWeatherResultFromLocationQueryAsync(locationQuery, cancellationToken).ConfigureAwait(true);
                    var newValue = new Readings(apiWeatherResult)
                    {
                        ApiRequestMade = true
                    };

                    if (!found || !apiCache.IsSuccessful || _fetchMode == FetchMode.AlwaysUseLastFetchedValue || (newValue.MeasuredTime >= apiCache.MeasuredTime))
                    {
                        _memoryCache.Set(locationQuery, newValue, new MemoryCacheEntryOptions
                        {
                            AbsoluteExpiration = newValue.FetchedTime.AddMilliseconds(_resiliencyPeriod)
                        });
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
                        apiCache.IsFromCache = false;
                        apiCache.ApiRequestMade = true;
                        return apiCache;
                    }
                }
                catch (OpenWeatherMapCacheException exception)
                {
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

        /// <summary>
        /// Attempts to get the readings for the provided <see cref="Location"/> or <see cref="ZipCode"/> by calling GetReadingsAsync synchronously (not ideal).
        /// </summary>
        /// <param name="locationQuery">The <see cref="Location"/> or <see cref="ZipCode"/> for which to get the readings.</param>
        /// <returns>A <see cref="Readings"/> object for the provided location, or the default value if the operation failed (<see cref="Readings.IsSuccessful"/> = false).</returns>
        public Readings GetReadings<T>(T locationQuery) where T : ILocationQuery
        {
            using (_asyncKeyedLocker.Lock(locationQuery))
            {
                var dateTime = DateTime.UtcNow;
                var found = _memoryCache.TryGetValue(locationQuery, out Readings apiCache);

                if (found)
                {
                    var timeElapsed = dateTime.Subtract(apiCache.FetchedTime).TotalMilliseconds;
                    if (timeElapsed <= _apiCachePeriod)
                    {
                        apiCache.IsFromCache = true;
                        apiCache.ApiRequestMade = false;
                        return apiCache;
                    }
                }

                try
                {
                    var apiWeatherResult = GetApiWeatherResultFromLocationQuery(locationQuery);
                    var newValue = new Readings(apiWeatherResult)
                    {
                        ApiRequestMade = true
                    };

                    if (!found || !apiCache.IsSuccessful || _fetchMode == FetchMode.AlwaysUseLastFetchedValue || (newValue.MeasuredTime >= apiCache.MeasuredTime))
                    {
                        _memoryCache.Set(locationQuery, newValue, new MemoryCacheEntryOptions
                        {
                            AbsoluteExpiration = newValue.FetchedTime.AddMilliseconds(_resiliencyPeriod)
                        });
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
                        apiCache.IsFromCache = false;
                        apiCache.ApiRequestMade = true;
                        return apiCache;
                    }
                }
                catch (OpenWeatherMapCacheException exception)
                {
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
}
