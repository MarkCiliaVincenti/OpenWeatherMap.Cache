// Copyright (c) All contributors.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace OpenWeatherMap.Cache.Models;

/// <summary>
/// Struct for the location with latitude and longitude.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="Location"/>.
/// </remarks>
/// <param name="latitude">The latitude of the location.</param>
/// <param name="longitude">The longitude of the location.</param>
public readonly struct Location(double latitude, double longitude) : ILocationQuery, IEquatable<Location>
{
    /// <summary>
    /// The latitude of the <see cref="Location"/>.
    /// </summary>
    public readonly double Latitude { get; } = latitude;
    /// <summary>
    /// The longitude of the <see cref="Location"/>.
    /// </summary>
    public readonly double Longitude { get; } = longitude;

    /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
    public bool Equals(Location other)
        => Latitude.Equals(other.Latitude) && Longitude.Equals(other.Longitude);

    /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
    public override bool Equals(object obj)
        => obj is Location other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
        => HashCode.Combine(Latitude, Longitude);

    /// <inheritdoc />
    public static bool operator ==(Location left, Location right)
        => left.Equals(right);

    /// <inheritdoc />
    public static bool operator !=(Location left, Location right)
        => !(left == right);
}
