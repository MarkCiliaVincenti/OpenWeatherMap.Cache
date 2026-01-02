// Copyright (c) All contributors.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using AsyncKeyedLock;
using Microsoft.Extensions.Caching.Memory;
using OpenWeatherMap.Cache.Constants;
using OpenWeatherMap.Cache.Models;
using OpenWeatherMap.Cache.Services;
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using static OpenWeatherMap.Cache.Enums;

namespace OpenWeatherMap.Cache;

/// <summary>
/// Interface for <see cref="OpenWeatherMapCache"/>
/// </summary>
public interface IOpenWeatherMapCache : IDisposable
{
    /// <inheritdoc cref="OpenWeatherMapCache.GetReadingsAsync"/>
    ValueTask<Readings> GetReadingsAsync<T>(T locationQuery, CancellationToken cancellationToken = default) where T : ILocationQuery;

    /// <inheritdoc cref="OpenWeatherMapCache.GetReadings"/>
    Readings GetReadings<T>(T locationQuery) where T : ILocationQuery;
}

/// <summary>
/// Class for OpenWeatherMapCache
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="OpenWeatherMapCache"/>.
/// </remarks>
/// <param name="apiKey">The unique API key obtained from OpenWeatherMap.</param>
/// <param name="apiCachePeriod">The number of milliseconds to cache for.</param>
/// <param name="fetchMode">The mode of operation. Defaults to <see cref="FetchMode.AlwaysUseLastMeasuredButExtendCache"/>.</param>
/// <param name="resiliencyPeriod">The number of milliseconds to keep on using cache values if API is unavailable. Defaults to <see cref="OpenWeatherMapCacheDefaults.DefaultResiliencyPeriod"/>.</param>
/// <param name="timeout">The number of milliseconds for the <see cref="WebRequest"/> timeout. Defaults to <see cref="OpenWeatherMapCacheDefaults.DefaultTimeout"/>.</param>
/// <param name="logPath">Logs the latest result for a given location to file. Defaults to null (disabled).</param>
public sealed class OpenWeatherMapCache(string apiKey, int apiCachePeriod, FetchMode fetchMode = FetchMode.AlwaysUseLastMeasuredButExtendCache, int resiliencyPeriod = OpenWeatherMapCacheDefaults.DefaultResiliencyPeriod, int timeout = OpenWeatherMapCacheDefaults.DefaultTimeout, string logPath = null) : IOpenWeatherMapCache, IDisposable
{
    private readonly MemoryCache _memoryCache = new(new MemoryCacheOptions());
    private readonly AsyncKeyedLocker<ILocationQuery> _asyncKeyedLocker = new();
    private const string BASE_WEATHER_URI = "https://api.openweathermap.org/data/2.5/weather";
    private readonly NumberFormatInfo _numberFormatInfo = new() { NumberDecimalSeparator = "_" };
    private readonly HttpClientService _httpClientService = new(new DefaultHttpClientFactory(timeout));
    private bool _disposedValue;

    private ApiWeatherResult GetApiWeatherResultFromUri(string logFileName, string uri, int timeout)
    {
        try
        {
            var task = _httpClientService.SendAsync(uri, HttpCompletionOption.ResponseHeadersRead);
            task.Wait(timeout);
            var response = task.Result;
            response.EnsureSuccessStatusCode();

            var streamTask = response.Content.ReadAsStreamAsync();
            streamTask.Wait(timeout);
            using var reader = new StreamReader(streamTask.Result);
            var content = reader.ReadToEnd();

            if (logPath != null)
            {
                File.WriteAllText(Path.Combine(logPath, logFileName), content);
            }

            return JsonSerializer.Deserialize(content, ApiWeatherResultJsonContext.Default.ApiWeatherResult);
        }
        catch (AggregateException ae) when (ae.InnerException is HttpRequestException hre)
        {
            throw new OpenWeatherMapCacheException($"HTTP request failed: {hre.Message}", hre);
        }
        catch (JsonException ex)
        {
            throw new OpenWeatherMapCacheException("Could not deserialize JSON content", ex);
        }
        catch (Exception ex)
        {
            throw new OpenWeatherMapCacheException(ex.Message, ex);
        }
    }

    private async ValueTask<ApiWeatherResult> GetApiWeatherResultFromUriAsync(string logFileName, string uri, int timeout, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Add("Accept", "application/json");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            using var response = await _httpClientService.SendAsync(uri, HttpCompletionOption.ResponseHeadersRead, cts.Token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

#if NET5_0_OR_GREATER
            using var stream = await response.Content.ReadAsStreamAsync(cts.Token).ConfigureAwait(false);
#else
            using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif
            using var streamReader = new StreamReader(stream);
#if NET7_0_OR_GREATER
            var content = await streamReader.ReadToEndAsync(cts.Token).ConfigureAwait(false);
#else
            var content = await streamReader.ReadToEndAsync().ConfigureAwait(false);
#endif

            if (logPath != null)
            {
#if NETSTANDARD2_0
                File.WriteAllText(Path.Combine(logPath, logFileName), content);
#else
                await File.WriteAllTextAsync(Path.Combine(logPath, logFileName), content, cancellationToken);
#endif
            }

            return JsonSerializer.Deserialize(content, ApiWeatherResultJsonContext.Default.ApiWeatherResult);
        }
        catch (HttpRequestException ex)
        {
            throw new OpenWeatherMapCacheException("HTTP request failed: " + ex.Message, ex);
        }
        catch (TaskCanceledException ex) when (cts.IsCancellationRequested)
        {
            throw new OperationCanceledException("Request was canceled", ex, cancellationToken);
        }
        catch (JsonException ex)
        {
            throw new OpenWeatherMapCacheException("Could not deserialize JSON content", ex);
        }
        catch (Exception ex)
        {
            throw new OpenWeatherMapCacheException("Unexpected error: " + ex.Message, ex);
        }
    }

    private ApiWeatherResult GetApiWeatherResultFromUri(Location location, string uri, int timeout) =>
        GetApiWeatherResultFromUri($"{location.Latitude.ToString(_numberFormatInfo)}-{location.Longitude.ToString(_numberFormatInfo)}.json", uri, timeout);

    private ValueTask<ApiWeatherResult> GetApiWeatherResultFromUriAsync(Location location, string uri, int timeout, CancellationToken cancellationToken) =>
        GetApiWeatherResultFromUriAsync($"{location.Latitude.ToString(_numberFormatInfo)}-{location.Longitude.ToString(_numberFormatInfo)}.json", uri, timeout, cancellationToken);

    private ApiWeatherResult GetApiWeatherResultFromUri(ZipCode zipCode, string uri, int timeout) =>
        GetApiWeatherResultFromUri($"{zipCode.Zip}-{zipCode.CountryCode}.json", uri, timeout);

    private ValueTask<ApiWeatherResult> GetApiWeatherResultFromUriAsync(ZipCode zipCode, string uri, int timeout, CancellationToken cancellationToken) =>
        GetApiWeatherResultFromUriAsync($"{zipCode.Zip}-{zipCode.CountryCode}.json", uri, timeout, cancellationToken);

    private ApiWeatherResult GetApiWeatherResultFromUri(City city, string uri, int timeout) =>
        GetApiWeatherResultFromUri($"{city.CityName}-{city.CountryCode}.json", uri, timeout);

    private ValueTask<ApiWeatherResult> GetApiWeatherResultFromUriAsync(City city, string uri, int timeout, CancellationToken cancellationToken) =>
        GetApiWeatherResultFromUriAsync($"{city.CityName}-{city.CountryCode}.json", uri, timeout, cancellationToken);

    private ApiWeatherResult GetApiWeatherResultFromLocationQuery<T>(T locationQuery) where T : ILocationQuery =>
        locationQuery switch
        {
            Location location => GetApiWeatherResultFromUri(location, $"{BASE_WEATHER_URI}?lat={location.Latitude}&lon={location.Longitude}&appid={apiKey}&cache={Guid.NewGuid()}", timeout),
            ZipCode zipCode => GetApiWeatherResultFromUri(zipCode, $"{BASE_WEATHER_URI}?zip={HttpUtility.UrlEncode(zipCode.Zip)},{HttpUtility.UrlEncode(zipCode.CountryCode)}&appid={apiKey}&cache={Guid.NewGuid()}", timeout),
            City city => GetApiWeatherResultFromUri(city, $"{BASE_WEATHER_URI}?q={HttpUtility.UrlEncode(city.CityName)}{(string.IsNullOrEmpty(city.CountryCode) ? "" : $",{HttpUtility.UrlEncode(city.CountryCode)}")}&appid={apiKey}&cache={Guid.NewGuid()}", timeout),
            _ => throw new ArgumentException("Unsupported type provided", nameof(locationQuery))
        };

    private ValueTask<ApiWeatherResult> GetApiWeatherResultFromLocationQueryAsync<T>(T locationQuery, CancellationToken cancellationToken) where T : ILocationQuery =>
        locationQuery switch
        {
            Location location => GetApiWeatherResultFromUriAsync(location, $"{BASE_WEATHER_URI}?lat={location.Latitude}&lon={location.Longitude}&appid={apiKey}&cache={Guid.NewGuid()}", timeout, cancellationToken),
            ZipCode zipCode => GetApiWeatherResultFromUriAsync(zipCode, $"{BASE_WEATHER_URI}?zip={HttpUtility.UrlEncode(zipCode.Zip)},{HttpUtility.UrlEncode(zipCode.CountryCode)}&appid={apiKey}&cache={Guid.NewGuid()}", timeout, cancellationToken),
            City city => GetApiWeatherResultFromUriAsync(city, $"{BASE_WEATHER_URI}?q={HttpUtility.UrlEncode(city.CityName)}{(string.IsNullOrEmpty(city.CountryCode) ? "" : $",{HttpUtility.UrlEncode(city.CountryCode)}")}&appid={apiKey}&cache={Guid.NewGuid()}", timeout, cancellationToken),
            _ => throw new ArgumentException("Unsupported type provided", nameof(locationQuery))
        };

    /// <summary>
    /// Attempts to get the readings for the provided <see cref="Location"/>, <see cref="ZipCode"/> or <see cref="City"/>.
    /// </summary>
    /// <param name="locationQuery">The <see cref="Location"/>, <see cref="ZipCode"/> or <see cref="City"/> for which to get the readings.</param>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Readings"/> object for the provided location, or the default value if the operation failed (<see cref="Readings.IsSuccessful"/> = false).</returns>
    public async ValueTask<Readings> GetReadingsAsync<T>(T locationQuery, CancellationToken cancellationToken = default) where T : ILocationQuery
    {
        using (await _asyncKeyedLocker.LockAsync(locationQuery, cancellationToken, true).ConfigureAwait(true))
        {
            var dateTime = DateTime.UtcNow;
            var found = _memoryCache.TryGetValue(locationQuery, out Readings apiCache);

            if (found && dateTime.Subtract(apiCache.FetchedTime).TotalMilliseconds <= apiCachePeriod)
            {
                apiCache.IsFromCache = true;
                apiCache.ApiRequestMade = false;
                return apiCache;
            }

            try
            {
                var apiWeatherResult = await GetApiWeatherResultFromLocationQueryAsync(locationQuery, cancellationToken).ConfigureAwait(true);
                var newValue = new Readings(apiWeatherResult) { ApiRequestMade = true };

                if (!found || !apiCache.IsSuccessful || fetchMode == FetchMode.AlwaysUseLastFetchedValue || newValue.MeasuredTime >= apiCache.MeasuredTime)
                {
                    _memoryCache.Set(locationQuery, newValue, new MemoryCacheEntryOptions { AbsoluteExpiration = newValue.FetchedTime.AddMilliseconds(resiliencyPeriod) });
                    return newValue;
                }
                else
                {
                    if (fetchMode == FetchMode.AlwaysUseLastMeasuredButExtendCache)
                    {
                        apiCache.FetchedTime = newValue.FetchedTime;
                        _memoryCache.Set(locationQuery, apiCache, new MemoryCacheEntryOptions { AbsoluteExpiration = apiCache.FetchedTime.AddMilliseconds(resiliencyPeriod) });
                    }
                    apiCache.IsFromCache = false;
                    apiCache.ApiRequestMade = true;
                    return apiCache;
                }
            }
            catch (OpenWeatherMapCacheException exception)
            {
                if (found && apiCache.IsSuccessful && dateTime.Subtract(apiCache.FetchedTime).TotalMilliseconds <= resiliencyPeriod)
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
    /// Attempts to get the readings for the provided <see cref="Location"/>, <see cref="ZipCode"/> or <see cref="City"/> by calling GetReadingsAsync synchronously (not ideal).
    /// </summary>
    /// <param name="locationQuery">The <see cref="Location"/>, <see cref="ZipCode"/> or <see cref="City"/> for which to get the readings.</param>
    /// <returns>A <see cref="Readings"/> object for the provided location, or the default value if the operation failed (<see cref="Readings.IsSuccessful"/> = false).</returns>
    public Readings GetReadings<T>(T locationQuery) where T : ILocationQuery
    {
        using (_asyncKeyedLocker.Lock(locationQuery))
        {
            var dateTime = DateTime.UtcNow;
            var found = _memoryCache.TryGetValue(locationQuery, out Readings apiCache);

            if (found && dateTime.Subtract(apiCache.FetchedTime).TotalMilliseconds <= apiCachePeriod)
            {
                apiCache.IsFromCache = true;
                apiCache.ApiRequestMade = false;
                return apiCache;
            }

            try
            {
                var apiWeatherResult = GetApiWeatherResultFromLocationQuery(locationQuery);
                var newValue = new Readings(apiWeatherResult) { ApiRequestMade = true };

                if (!found || !apiCache.IsSuccessful || fetchMode == FetchMode.AlwaysUseLastFetchedValue || newValue.MeasuredTime >= apiCache.MeasuredTime)
                {
                    _memoryCache.Set(locationQuery, newValue, new MemoryCacheEntryOptions { AbsoluteExpiration = newValue.FetchedTime.AddMilliseconds(resiliencyPeriod) });
                    return newValue;
                }
                else
                {
                    if (fetchMode == FetchMode.AlwaysUseLastMeasuredButExtendCache)
                    {
                        apiCache.FetchedTime = newValue.FetchedTime;
                        _memoryCache.Set(locationQuery, apiCache, new MemoryCacheEntryOptions { AbsoluteExpiration = apiCache.FetchedTime.AddMilliseconds(resiliencyPeriod) });
                    }
                    apiCache.IsFromCache = false;
                    apiCache.ApiRequestMade = true;
                    return apiCache;
                }
            }
            catch (OpenWeatherMapCacheException exception)
            {
                if (found && apiCache.IsSuccessful && dateTime.Subtract(apiCache.FetchedTime).TotalMilliseconds <= resiliencyPeriod)
                {
                    apiCache.IsFromCache = true;
                    apiCache.ApiRequestMade = false;
                    return apiCache;
                }

                return new Readings(exception);
            }
        }
    }

    private void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _memoryCache?.Dispose();
                _asyncKeyedLocker?.Dispose();
                _httpClientService?.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
