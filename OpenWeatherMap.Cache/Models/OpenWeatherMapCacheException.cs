using System;

namespace OpenWeatherMap.Cache.Models;

/// <summary>
/// Class for exceptions during API calls
/// </summary>
[Serializable]
public sealed class OpenWeatherMapCacheException : Exception
{
    internal OpenWeatherMapCacheException(string message, Exception innerException)
        : base($"Exception during API call: {message}", innerException)
    { }
}
