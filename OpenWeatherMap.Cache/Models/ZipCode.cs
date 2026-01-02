// Copyright (c) All contributors.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace OpenWeatherMap.Cache.Models;

/// <summary>
/// Class for the location with zip code (postal code) and country.
/// </summary>
public sealed class ZipCode : ILocationQuery, IEquatable<ZipCode>
{
    private string _zip;
    private string _countryCode;

    /// <summary>
    /// The zip code (postal code) of the <see cref="ZipCode"/>.
    /// </summary>
    public string Zip
    {
        get
        {
            return _zip;
        }
        set
        {
            _zip = value?.Trim().ToLowerInvariant()
                ?? throw new ArgumentNullException(nameof(value));
        }
    }

    /// <summary>
    /// The 2 letter ISO 3166-1 alpha-2 country code of the <see cref="ZipCode"/>.
    /// </summary>
    public string CountryCode
    {
        get
        {
            return _countryCode;
        }
        set
        {
            _countryCode = value?.Trim().ToLowerInvariant()
                ?? throw new ArgumentNullException(nameof(value));
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ZipCode"/>.
    /// </summary>
    /// <param name="zip">The zip code (postal code) of the location.</param>
    /// <param name="countryCode">The 2 letter ISO 3166-1 alpha-2 country code of the location.</param>
    public ZipCode(string zip, string countryCode)
    {
        Zip = zip;
        CountryCode = countryCode;
    }

    /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
    public bool Equals(ZipCode other)
    {
        if (other == null)
        {
            return false;
        }
        return Zip.Equals(other.Zip, StringComparison.Ordinal) && CountryCode.Equals(other.CountryCode, StringComparison.Ordinal);
    }

    /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
    public override bool Equals(object obj)
    {
        return obj is ZipCode other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Zip, CountryCode);
    }
}
