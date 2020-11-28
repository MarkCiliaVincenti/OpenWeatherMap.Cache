namespace OpenWeatherMap.Cache
{
    /// <summary>
    /// Enumeration class for OpenWeatherMap.Cache
    /// </summary>
    public class Enums
    {
        public enum FetchMode
        {
            /// <summary>
            /// If the time elapsed since the last fetch for the given location exceeds the cache period but is
            /// within the resiliency period (i.e. still available in the cache), and the API reported measured
            /// time in the cache value is more recent than the latest value fetched from the API, then return
            /// the more recent cached result.<br></br><br></br>IMPORTANT: Frequent calls may impact your API
            /// usage.
            /// </summary>
            AlwaysUseLastMeasured,
            /// <summary>
            /// If the time elapsed since the last fetch for the given location exceeds the cache period but is
            /// within the resiliency period (i.e. still available in the cache), and the API reported measured
            /// time in the cache value is more recent than the latest value fetched from the API, then return
            /// the more recent cached result.<br></br><br></br>In order to protect impact on your API usage,
            /// this setting updates the cache value's fetched date and extends the cache lifetime.
            /// </summary>
            AlwaysUseLastMeasuredButExtendCache,
            /// <summary>
            /// If the time elapsed since the last fetch for the given location exceeds the cache period but is
            /// within the resiliency period (i.e. still available in the cache), and the API reported measured
            /// time in the cache value is more recent than the latest value fetched from the API, then return
            /// and cache the last fetched API result, even though it is being reported to be older by the API.
            /// </summary>
            AlwaysUseLastFetchedValue
        }
    }
}
