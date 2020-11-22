using System;

namespace OpenWeatherMap.Cache.Models
{
    public class Readings : IEquatable<Readings>
    {
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public double Pressure { get; set; }
        public DateTime FetchedTime { get; set; }
        public DateTime CalculatedTime { get; set; }
        public bool IsSuccessful => !double.IsNaN(Temperature);

        public Readings(double temperature, double humidity, double pressure, DateTime calcuatedTime)
        {
            Temperature = temperature;
            Humidity = humidity;
            Pressure = pressure;
            CalculatedTime = calcuatedTime;
            FetchedTime = DateTime.UtcNow;
        }

        public Readings(DateTime calculatedTime)
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
