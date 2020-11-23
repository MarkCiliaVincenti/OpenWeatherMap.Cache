using System;
using System.Collections.Generic;

namespace OpenWeatherMap.Cache.Models
{
    /// <summary>
    /// Class for the location with latitude and longtitude.
    /// </summary>
    public class Location : IEquatable<Location>
    {
        /// <summary>
        /// The latitude of the <see cref="Location"/>.
        /// </summary>
        public double Latitude { get; set; }
        /// <summary>
        /// The longtitude of the <see cref="Location"/>.
        /// </summary>
        public double Longtitude { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="Location"/>.
        /// </summary>
        /// <param name="latitude">The latitude of the location.</param>
        /// <param name="longtitude">The longtitude of the location.</param>
        public Location(double latitude, double longtitude)
        {
            Latitude = latitude;
            Longtitude = longtitude;
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        public bool Equals(Location other)
        {
            if (other == null)
                return false;
            return Latitude.Equals(other.Latitude) && Longtitude.Equals(other.Longtitude);
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        public override bool Equals(object obj)
        {
            return obj is Location other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Latitude.GetHashCode();
                hash = hash * 23 + Longtitude.GetHashCode();
                return hash;
            }
        }
    }
}
