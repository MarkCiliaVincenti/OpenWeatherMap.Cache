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
                var location = new Models.Location(48.6371, -122.1237);
                var readings = openWeatherMapCache.GetReadingsAsync(location).Result;
                if (readings.IsFromCache)
                    Interlocked.Increment(ref totalFromCache);
                else
                    Interlocked.Increment(ref totalFromAPI);
            });

            Assert.Equal(99, totalFromCache);
            Assert.Equal(1, totalFromAPI);

            Thread.Sleep(1001);

            result = Parallel.For(1, 101, (i, state) =>
            {
                var location = new Models.Location(1, 1);
                var readings = openWeatherMapCache.GetReadingsAsync(location).Result;
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
