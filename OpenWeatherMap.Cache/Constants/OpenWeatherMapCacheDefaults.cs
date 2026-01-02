// Copyright (c) All contributors.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace OpenWeatherMap.Cache.Constants;

/// <summary>
/// Class for OpenWeatherMap.Cache defaults
/// </summary>
public static class OpenWeatherMapCacheDefaults
{
    /// <summary>
    /// The default resiliency period in milliseconds to use if this is not passed on.
    /// </summary>
    public const int DefaultResiliencyPeriod = 300_000;
    /// <summary>
    /// The default timeout in milliseconds to use if this is not passed on.
    /// </summary>
    public const int DefaultTimeout = 5_000;
}
