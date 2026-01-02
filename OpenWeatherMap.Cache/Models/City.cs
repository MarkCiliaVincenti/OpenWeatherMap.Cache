// Copyright (c) All contributors.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace OpenWeatherMap.Cache.Models;

/// <summary>
/// Class for the location with city name and optional country.
/// </summary>
public sealed class City : ILocationQuery, IEquatable<City>
{
    private string _cityName;
    private string _countryCode;

    /// <summary>
    /// The city name of the <see cref="City"/>.
    /// </summary>
    public string CityName
    {
        get
        {
            return _cityName;
        }
        set
        {
            _cityName = value?.Trim().ToLowerInvariant()
                ?? throw new ArgumentNullException(nameof(value));
        }
    }

    /// <summary>
    /// Optional 2 letter ISO 3166-1 alpha-2 country code of the <see cref="City"/>.
    /// </summary>
    public string CountryCode
    {
        get
        {
            return _countryCode;
        }
        set
        {
            _countryCode = value?.Trim().ToLowerInvariant();
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="City"/>.
    /// </summary>
    /// <param name="cityName">The city name of the location.</param>
    /// <param name="countryCode">Optional 2 letter ISO 3166-1 alpha-2 country code of the location.</param>
    public City(string cityName, string countryCode = "")
    {
        CityName = cityName;
        CountryCode = countryCode;
    }

    /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
    public bool Equals(City other)
    {
        if (other == null)
        {
            return false;
        }
        return CityName.Equals(other.CityName, StringComparison.Ordinal) && CountryCode.Equals(other.CountryCode, StringComparison.Ordinal);
    }

    /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
    public override bool Equals(object obj)
    {
        return obj is City other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(CityName, CountryCode);
    }
}
