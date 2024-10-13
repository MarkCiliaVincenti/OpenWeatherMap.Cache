using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OpenWeatherMap.Cache.Tests
{
    public class Tests
    {
        private string _apiKey;

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
    }
}
