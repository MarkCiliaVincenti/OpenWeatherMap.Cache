using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OpenWeatherMap.Cache.Tests
{
    public class Tests
    {
        [Fact]
        public void TestConcurrency()
        {
            var openWeatherMapCache = new OpenWeatherMapCache("[API-Key]", 1000);
            int totalFromCache = 0;
            int totalFromAPI = 0;

            var result = Parallel.For(1, 101, (i, state) =>
            {
                var location = new Models.Location(1, 1);
                openWeatherMapCache.TryGetReadings(location, out var readings);
                if (readings.IsFromCache)
                    Interlocked.Increment(ref totalFromCache);
                else
                    Interlocked.Increment(ref totalFromAPI);
            });

            Assert.Equal(99, totalFromCache);
            Assert.Equal(1, totalFromAPI);

            Thread.Sleep(1000);

            result = Parallel.For(1, 101, (i, state) =>
            {
                var location = new Models.Location(1, 1);
                openWeatherMapCache.TryGetReadings(location, out var readings);
                if (readings.IsFromCache)
                    Interlocked.Increment(ref totalFromCache);
                else
                    Interlocked.Increment(ref totalFromAPI);
            });

            Assert.Equal(198, totalFromCache);
            Assert.Equal(2, totalFromAPI);
        }
    }
}
