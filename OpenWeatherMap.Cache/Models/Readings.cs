using System;

namespace OpenWeatherMap.Cache.Models
{
    public class Readings : IEquatable<Readings>
    {
        /// <summary>
        /// The temperature of the <see cref="Readings"/>.
        /// </summary>
        public double Temperature { get; set; }
        /// <summary>
        /// The humidity of the <see cref="Readings"/>.
        /// </summary>
        public double Humidity { get; set; }
        /// <summary>
        /// The pressure of the <see cref="Readings"/>.
        /// </summary>
        public double Pressure { get; set; }
        /// <summary>
        /// The time the <see cref="Readings"/> object was fetched.
        /// </summary>
        public DateTime FetchedTime { get; set; }
        /// <summary>
        /// The time the <see cref="Readings"/> data was updated by OpenWeatherMap.
        /// </summary>
        public DateTime CalculatedTime { get; set; }
        /// <summary>
        /// Indicates whether the <see cref="Readings"/> were successful or not.
        /// </summary>
        public bool IsSuccessful => !double.IsNaN(Temperature);
        /// <summary>
        /// Indicates whether the <see cref="Readings"/> were retrieved from cache or directly from the API.
        /// </summary>
        public bool IsFromCache { get; set; }

        internal Readings(double temperature, double humidity, double pressure, DateTime calcuatedTime)
        {
            Temperature = temperature;
            Humidity = humidity;
            Pressure = pressure;
            CalculatedTime = calcuatedTime;
            FetchedTime = DateTime.UtcNow;
        }

        internal Readings(DateTime calculatedTime)
        {
            Temperature = double.NaN;
            Humidity = double.NaN;
            Pressure = double.NaN;
            CalculatedTime = calculatedTime;
            FetchedTime = DateTime.UtcNow;
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        public bool Equals(Readings other)
        {
            if (other == null)
                return false;
            return CalculatedTime.Equals(other.CalculatedTime) && Temperature.Equals(other.Temperature) && Humidity.Equals(other.Humidity) && Pressure.Equals(other.Pressure);
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        public override bool Equals(object obj)
        {
            return obj is Readings other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                // Suitable nullity checks etc, of course :)
                hash = hash * 23 + Temperature.GetHashCode();
                hash = hash * 23 + Humidity.GetHashCode();
                hash = hash * 23 + Pressure.GetHashCode();
                return hash;
            }
        }
    }
}
