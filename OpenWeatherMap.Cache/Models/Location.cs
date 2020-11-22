using System.Collections.Generic;

namespace OpenWeatherMap.Cache.Models
{
    public class Location
    {

        public double Latitude { get; set; }
        public double Longtitude { get; set; }

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
