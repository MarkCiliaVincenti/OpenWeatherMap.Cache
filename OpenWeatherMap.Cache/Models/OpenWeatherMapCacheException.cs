// Copyright (c) All contributors.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace OpenWeatherMap.Cache.Models;

/// <summary>
/// Class for exceptions during API calls
/// </summary>
[Serializable]
public sealed class OpenWeatherMapCacheException : Exception
{
    public OpenWeatherMapCacheException()
        : base("Exception during API call.")
    { }

    public OpenWeatherMapCacheException(string message)
        : base($"Exception during API call: {message}")
    { }

    internal OpenWeatherMapCacheException(string message, Exception innerException)
        : base($"Exception during API call: {message}", innerException)
    { }
}
