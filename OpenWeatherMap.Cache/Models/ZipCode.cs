// Copyright (c) All contributors.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace OpenWeatherMap.Cache.Models;

/// <summary>
/// Struct for the location with zip code (postal code) and country.
/// </summary>
public struct ZipCode : ILocationQuery, IEquatable<ZipCode>
{
    private string _zip;
    private string _countryCode;

    /// <summary>
    /// The zip code (postal code) of the <see cref="ZipCode"/>.
    /// </summary>
    public string Zip
    {
        readonly get => _zip;
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
        readonly get => _countryCode;
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
    public readonly bool Equals(ZipCode other)
        => Zip.Equals(other.Zip, StringComparison.Ordinal) && CountryCode.Equals(other.CountryCode, StringComparison.Ordinal);

    /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
    public override readonly bool Equals(object obj)
        => obj is ZipCode other && Equals(other);

    /// <inheritdoc />
    public override readonly int GetHashCode()
        => HashCode.Combine(Zip, CountryCode);

    /// <inheritdoc />
    public static bool operator ==(ZipCode left, ZipCode right)
        => left.Equals(right);

    /// <inheritdoc />
    public static bool operator !=(ZipCode left, ZipCode right)
        => !(left == right);
}
