using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OpenWeatherMap.Cache.Tests
{
    public class Tests
    {
        private const string apiKey = "[API-Key]";

        [Fact]
        public void TestConcurrency()
        {
            var openWeatherMapCache = new OpenWeatherMapCache(apiKey, 10_000);
            int totalFromCache = 0;
            int totalFromAPI = 0;

            var location = new Models.Location(48.6371, -122.1237);
            var readings = openWeatherMapCache.GetReadingsAsync(location).Result;

            var result = Parallel.For(1, 101, (i, state) =>
            {
                var location = new Models.Location(48.6371, -122.1237);
                var readings = openWeatherMapCache.GetReadingsAsync(location).Result;
                if (readings.IsFromCache)
                    Interlocked.Increment(ref totalFromCache);
                else
                    Interlocked.Increment(ref totalFromAPI);
            });

            while (totalFromCache + totalFromAPI < 100)
            {
            }

            Assert.Equal(99, totalFromCache);
            Assert.Equal(1, totalFromAPI);

            Thread.Sleep(10_001);

            result = Parallel.For(1, 101, (i, state) =>
            {
                var location = new Models.Location(1, 1);
                var readings = openWeatherMapCache.GetReadingsAsync(location).Result;
                if (readings.IsFromCache)
                    Interlocked.Increment(ref totalFromCache);
                else
                    Interlocked.Increment(ref totalFromAPI);
            });

            while (totalFromCache + totalFromAPI < 200)
            {
            }

            Assert.Equal(198, totalFromCache);
            Assert.Equal(2, totalFromAPI);
        }
        
        [Fact]
        public async Task TestConcurrencyAsync()
        {
            var openWeatherMapCache = new OpenWeatherMapCache(apiKey, 10_000);
            int totalFromCache = 0;
            int totalFromAPI = 0;

            var result = Parallel.For(1, 101, async (i, state) =>
            {
                var location = new Models.Location(48.6371, -122.1237);
                var readings = await openWeatherMapCache.GetReadingsAsync(location);
                if (readings.IsFromCache)
                    Interlocked.Increment(ref totalFromCache);
                else
                    Interlocked.Increment(ref totalFromAPI);
            });
            
            while (totalFromCache + totalFromAPI < 100)
            {
            }

            Assert.Equal(99, totalFromCache);
            Assert.Equal(1, totalFromAPI);

            await Task.Delay(10_001);

            result = Parallel.For(1, 101, async (i, state) =>
            {
                var location = new Models.Location(1, 1);
                var readings = await openWeatherMapCache.GetReadingsAsync(location);
                if (readings.IsFromCache)
                    Interlocked.Increment(ref totalFromCache);
                else
                    Interlocked.Increment(ref totalFromAPI);
            });

            while (totalFromCache + totalFromAPI < 200)
            {
            }

            Assert.Equal(198, totalFromCache);
            Assert.Equal(2, totalFromAPI);
        }
    }
}
