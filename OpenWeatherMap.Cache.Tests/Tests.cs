using Microsoft.Extensions.Configuration;
using OpenWeatherMap.Cache.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OpenWeatherMap.Cache.Tests;

public class Tests
{
    private readonly string _apiKey;

    public Tests()        
    {
        var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        _apiKey = config["apiKey"];

        if (string.IsNullOrWhiteSpace(_apiKey) || _apiKey == "[API Key]")
        {
            throw new Exception("OpenWeather API key not successfully set.");
        }
    }

    [Fact]
    public async Task TestConcurrency()
    {
        int apiCachePeriod = 1_000;
        int concurrency = 100;
        int tries = 5;

        var openWeatherMapCache = new OpenWeatherMapCache(_apiKey, apiCachePeriod);
        int totalFromCache = 0;
        int totalFromAPI = 0;
        int totalSuccessful = 0;

        ParallelQuery<Task> tasks;

        for (int i = 1; i <= tries; i++)
        {
            tasks = Enumerable.Range(0, concurrency)
                .Select(i =>
                {
                    var location = new Models.Location(48.6371, -122.1237);
                    var readings = openWeatherMapCache.GetReadings(location);
                    if (readings.IsSuccessful)
                    {
                        Interlocked.Increment(ref totalSuccessful);
                    }
                    if (readings.IsFromCache)
                    {
                        Interlocked.Increment(ref totalFromCache);
                    }
                    else
                    {
                        Interlocked.Increment(ref totalFromAPI);
                    }
                    return Task.CompletedTask;
                }).ToList().AsParallel();
            await Task.WhenAll(tasks);

            if (i < tries)
            {
                await Task.Delay(apiCachePeriod + 1);
            }

            Assert.Equal(concurrency * i, totalSuccessful);
            Assert.Equal((concurrency - 1) * i, totalFromCache);
            Assert.Equal(i, totalFromAPI);
        }
    }

    [Fact]
    public async Task TestConcurrencyAsync()
    {
        int apiCachePeriod = 1_000;
        int concurrency = 100;
        int tries = 5;

        var openWeatherMapCache = new OpenWeatherMapCache(_apiKey, apiCachePeriod);
        int totalFromCache = 0;
        int totalFromAPI = 0;
        int totalSuccessful = 0;

        ParallelQuery<Task> tasks;

        for (int i = 1; i <= tries; i++)
        {
            tasks = Enumerable.Range(0, concurrency)
                .Select(async (i) =>
                {
                    var location = new Models.Location(48.6371, -122.1237);
                    var readings = await openWeatherMapCache.GetReadingsAsync(location);
                    if (readings.IsSuccessful)
                    {
                        Interlocked.Increment(ref totalSuccessful);
                    }
                    if (readings.IsFromCache)
                    {
                        Interlocked.Increment(ref totalFromCache);
                    }
                    else
                    {
                        Interlocked.Increment(ref totalFromAPI);
                    }
                }).ToList().AsParallel();
            await Task.WhenAll(tasks);

            if (i < tries)
            {
                await Task.Delay(apiCachePeriod + 1);
            }

            Assert.Equal(concurrency * i, totalSuccessful);
            Assert.Equal((concurrency - 1) * i, totalFromCache);
            Assert.Equal(i, totalFromAPI);
        }
    }

    [Fact]
    public async Task GetReadingsAsync_ShouldReturnSuccessfulReading_WhenNotCached()
    {
        var location = new Models.Location(37.7749, -122.4194);
        var cache = new OpenWeatherMapCache(_apiKey, 1_000);

        var result = await cache.GetReadingsAsync(location);

        Assert.True(result.IsSuccessful);
        Assert.True(result.ApiRequestMade);
        Assert.False(result.IsFromCache);
    }

    [Fact]
    public void GetReadings_ShouldReturnFromCache_WhenWithinCachePeriod()
    {
        var location = new Models.Location(40.7128, -74.0060);
        var cache = new OpenWeatherMapCache(_apiKey, 10_000);

        var first = cache.GetReadings(location);
        var second = cache.GetReadings(location);

        Assert.True(first.IsSuccessful);
        Assert.True(second.IsFromCache);
        Assert.False(second.ApiRequestMade);
    }

    public class InvalidQuery : ILocationQuery { }

    [Fact]
    public async Task GetReadingsAsync_ShouldThrowArgumentException_WhenInvalidLocationQuery()
    {
        var cache = new OpenWeatherMapCache(_apiKey, 1000);
        await Assert.ThrowsAsync<ArgumentException>(async () => await cache.GetReadingsAsync(new InvalidQuery()));
    }

    [Fact]
    public async Task GetReadingsAsync_ShouldLogResponse_WhenLogPathIsSet()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var cache = new OpenWeatherMapCache(_apiKey, 1000, logPath: tempDir);
        var location = new Models.Location(48.8566, 2.3522);

        var _ = await cache.GetReadingsAsync(location);

        var expectedFile = Path.Combine(tempDir, $"{location.Latitude.ToString().Replace('.', '_')}-{location.Longitude.ToString().Replace('.', '_')}.json");
        Assert.True(File.Exists(expectedFile));

        // Cleanup
        Directory.Delete(tempDir, true);
    }

    [Fact]
    public void GetReadings_ShouldLogResponse_WhenLogPathIsSet()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var cache = new OpenWeatherMapCache(_apiKey, 1000, logPath: tempDir);
        var location = new Models.Location(48.8566, 2.3522);

        var _ = cache.GetReadings(location);

        var expectedFile = Path.Combine(tempDir, $"{location.Latitude.ToString().Replace('.', '_')}-{location.Longitude.ToString().Replace('.', '_')}.json");
        Assert.True(File.Exists(expectedFile));

        // Cleanup
        Directory.Delete(tempDir, true);
    }

    [Fact]
    public void GetReadings_SynchronousMethod_ReturnsValidReading()
    {
        var cache = new OpenWeatherMapCache(_apiKey, apiCachePeriod: 1000);
        var location = new Location(48.6371, -122.1237);

        var reading = cache.GetReadings(location);

        Assert.True(reading.IsSuccessful);
        Assert.False(reading.IsFromCache);
    }

    [Fact]
    public async Task GetReadingsAsync_ZipCodeQuery_ReturnsValidReading()
    {
        var cache = new OpenWeatherMapCache(_apiKey, apiCachePeriod: 1000);
        var zipCode = new ZipCode("90210", "US");

        var reading = await cache.GetReadingsAsync(zipCode);

        Assert.True(reading.IsSuccessful);
        Assert.False(reading.IsFromCache);
    }

    [Fact]
    public void LocationMatches()
    {
        var location1 = new Location(48.6371, -122.1237);
        var location2 = new Location(48.6371, -122.1237);
        var location3 = new Location(37.7749, -122.4194);
        Assert.True(location1.Equals(location2));
        Assert.False(location1.Equals(location3));
        Assert.False(location1.Equals(null));
    }

    [Fact]
    public void LocationLongitudeAndLatitude()
    {
        var longitude = 48.6371;
        var latitude = -122.1237;

        var location = new Location(latitude, longitude);
        Assert.Equal(latitude, location.Latitude);
        Assert.Equal(longitude, location.Longitude);
        location.Latitude = 0;
        location.Longitude = 0;
        Assert.Equal(0, location.Latitude);
        Assert.Equal(0, location.Longitude);
        Assert.True(location.Equals(new Location(0, 0)));
    }
}
