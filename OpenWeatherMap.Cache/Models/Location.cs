using System.Collections.Generic;

namespace OpenWeatherMap.Cache.Models
{
    public class Location
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

        public class EqualityComparer : IEqualityComparer<Location>
        {

            public bool Equals(Location x, Location y)
            {
                return x.Latitude == y.Latitude && x.Longtitude == y.Longtitude;
            }

            public int GetHashCode(Location obj)
            {
                unchecked
                {
                    int hash = 17;

                    hash = hash * 23 + obj.Latitude.GetHashCode();
                    hash = hash * 23 + obj.Longtitude.GetHashCode();
                    return hash;
                }
            }
        }
    }
}
