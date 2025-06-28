using System;

namespace OpenWeatherMap.Cache.Models;

/// <summary>
/// Class for the location with zip code (postal code) and country.
/// </summary>
public class ZipCode : ILocationQuery, IEquatable<ZipCode>
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
            _zip = value.Trim().ToLowerInvariant();
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
            _countryCode = value.Trim().ToLowerInvariant();
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
        return Zip.Equals(other.Zip) && CountryCode.Equals(other.CountryCode);
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
