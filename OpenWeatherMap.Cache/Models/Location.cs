using System;

namespace OpenWeatherMap.Cache.Models;

/// <summary>
/// Class for the location with latitude and longitude.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="Location"/>.
/// </remarks>
/// <param name="latitude">The latitude of the location.</param>
/// <param name="longitude">The longitude of the location.</param>
public sealed class Location(double latitude, double longitude) : ILocationQuery, IEquatable<Location>
{
    /// <summary>
    /// The latitude of the <see cref="Location"/>.
    /// </summary>
    public double Latitude { get; set; } = latitude;
    /// <summary>
    /// The longitude of the <see cref="Location"/>.
    /// </summary>
    public double Longitude { get; set; } = longitude;

    /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
    public bool Equals(Location other)
    {
        if (other == null)
        {
            return false;
        }
        return Latitude.Equals(other.Latitude) && Longitude.Equals(other.Longitude);
    }

    /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
    public override bool Equals(object obj)
    {
        return obj is Location other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Latitude, Longitude);
    }
}
