using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OpenWeatherMap.Cache.Tests
{
    public class Tests
    {
        private const string apiKey = "[API Key]";

        [Fact]
        public async Task TestConcurrency()
        {
            int apiCachePeriod = 1_000;
            int concurrency = 100;
            int tries = 5;

            var openWeatherMapCache = new OpenWeatherMapCache(apiKey, apiCachePeriod);
            int totalFromCache = 0;
            int totalFromAPI = 0;
            int totalSuccessful = 0;

            ParallelQuery<Task> tasks;

            for (int i = 1; i <= tries; i++)
            {
                tasks = Enumerable.Range(0, concurrency)
                    .Select(async i =>
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
                    }).ToList().AsParallel();
                await Task.WhenAll(tasks);

                Assert.Equal(concurrency * i, totalSuccessful);
                Assert.Equal((concurrency - 1) * i, totalFromCache);
                Assert.Equal(i, totalFromAPI);

                if (i < tries)
                {
                    await Task.Delay(apiCachePeriod + 1);
                }
            }
        }
        
        [Fact]
        public async Task TestConcurrencyAsync()
        {
            int apiCachePeriod = 1_000;
            int concurrency = 100;
            int tries = 5;

            var openWeatherMapCache = new OpenWeatherMapCache(apiKey, apiCachePeriod);
            int totalFromCache = 0;
            int totalFromAPI = 0;
            int totalSuccessful = 0;

            ParallelQuery<Task> tasks;

            for (int i = 1; i <= tries; i++)
            {
                tasks = Enumerable.Range(0, concurrency)
                    .Select(async i =>
                    {
                        var location = new Models.Location(48.6371, -122.1237);
                        var readings = await openWeatherMapCache.GetReadingsAsync(location).ConfigureAwait(false);
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

                Assert.Equal(concurrency * i, totalSuccessful);
                Assert.Equal((concurrency - 1) * i, totalFromCache);
                Assert.Equal(i, totalFromAPI);

                if (i < tries)
                {
                    await Task.Delay(apiCachePeriod + 1);
                }
            }
        }
    }
}
