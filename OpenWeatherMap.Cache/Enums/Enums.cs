// Copyright (c) All contributors.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace OpenWeatherMap.Cache;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Enumeration class for OpenWeatherMap.Cache
/// </summary>
public sealed class Enums
{
    /// <summary>
    /// If the time elapsed since the last fetch for the given location exceeds the cache period (i.e. a new API call is required)
    /// but is within the resiliency period (i.e. the previous readings are still available in the cache), the API reported measured
    /// time in the cache value is sometimes more recent than the latest value fetched from the API. There are three modes to tackle
    /// this issue.
    /// </summary>
    public enum FetchMode
    {
        /// <summary>
        /// If the time elapsed since the last fetch for the given location exceeds the cache period (i.e.
        /// a new API call is required) but is within the resiliency period (i.e. the previous readings are
        /// still available in the cache), and the API reported measured time in the cache value is more
        /// recent than the latest value fetched from the API, then return the more recent cached result. The
        /// implication is that if you immediately request another reading it will also hit the API again.
        /// <br></br><br></br>IMPORTANT: Frequent calls may impact your API usage.
        /// </summary>
        AlwaysUseLastMeasured,
        /// <summary>
        /// If the time elapsed since the last fetch for the given location exceeds the cache period (i.e.
        /// a new API call is required) but is within the resiliency period (i.e. the previous readings are
        /// still available in the cache), and the API reported measured time in the cache value is more
        /// recent than the latest value fetched from the API, then return the more recent cached result.
        /// <br></br><br></br>In order to protect impact on your API usage, this setting updates the cache
        /// value's fetched date and extends the cache lifetime.
        /// </summary>
        AlwaysUseLastMeasuredButExtendCache,
        /// <summary>
        /// If the time elapsed since the last fetch for the given location exceeds the cache period (i.e.
        /// a new API call is required) but is within the resiliency period (i.e. the previous readings are
        /// still available in the cache), and the API reported measured time in the cache value is more
        /// recent than the latest value fetched from the API, then return and cache the last fetched API
        /// result, even though it is being reported to be older by the API.
        /// </summary>
        AlwaysUseLastFetchedValue
    }
}
