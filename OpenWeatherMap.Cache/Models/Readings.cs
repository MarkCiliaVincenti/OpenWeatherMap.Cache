using System;
using System.Collections.Generic;
using UnitsNet;

namespace OpenWeatherMap.Cache.Models
{
    /// <summary>
    /// Class for weather conditions
    /// </summary>
    [Serializable]
    public sealed class WeatherCondition : IEquatable<WeatherCondition>
    {
        /// <summary>
        /// Weather condition id
        /// </summary>
        public int ConditionId { get; internal set; }
        /// <summary>
        /// Group of weather parameters (Rain, Snow, Extreme etc.)
        /// </summary>
        public string Main { get; internal set; }
        /// <summary>
        /// Weather condition within the group.
        /// </summary>
        public string Description { get; internal set; }
        /// <summary>
        /// Weather icon id
        /// </summary>
        public string IconId { get; internal set; }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        public bool Equals(WeatherCondition other)
        {
            if (other == null)
                return false;
            return ConditionId.Equals(other.ConditionId);
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        public override bool Equals(object obj)
        {
            return obj is WeatherCondition other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + ConditionId.GetHashCode();
                return hash;
            }
        }
    }

    /// <summary>
    /// Class for readings
    /// </summary>
    [Serializable]
    public sealed class Readings : IEquatable<Readings>
    {
        /// <summary>
        /// Weather conditions
        /// </summary>
        public List<WeatherCondition> Weather { get; internal set; }
        /// <summary>
        /// The temperature of the <see cref="Readings"/>.
        /// </summary>
        public Temperature Temperature { get; internal set; }
        /// <summary>
        /// Temperature. This temperature parameter accounts for the human perception of weather.
        /// </summary>
        public Temperature FeelsLike { get; internal set; }
        /// <summary>
        /// The pressure of the <see cref="Readings"/>.
        /// </summary>
        public Pressure Pressure { get; internal set; }
        /// <summary>
        /// The humidity of the <see cref="Readings"/>.
        /// </summary>
        public RelativeHumidity Humidity { get; internal set; }
        /// <summary>
        /// Minimum temperature at the moment. This is minimal currently observed temperature (within large megalopolises and urban areas).
        /// </summary>
        public Temperature MinimumTemperature { get; internal set; }
        /// <summary>
        /// Maximum temperature at the moment. This is maximal currently observed temperature (within large megalopolises and urban areas).
        /// </summary>
        public Temperature MaximumTemperature { get; internal set; }
        /// <summary>
        /// Atmospheric pressure at sea level
        /// </summary>
        public Pressure? SeaLevelPressure { get; internal set; }
        /// <summary>
        /// Atmospheric pressure at ground level
        /// </summary>
        public Pressure? GroundLevelPressure { get; internal set; }
        /// <summary>
        /// Wind speed
        /// </summary>
        public Speed WindSpeed { get; internal set; }
        /// <summary>
        /// Wind direction
        /// </summary>
        public Angle WindDirection { get; internal set; }
        /// <summary>
        /// Wind gust
        /// </summary>
        public Speed? WindGust { get; internal set; }
        /// <summary>
        /// Cloudiness
        /// </summary>
        public Ratio Cloudiness { get; internal set; }
        /// <summary>
        /// Rainfall in the last hour
        /// </summary>
        public Length? RainfallLastHour { get; internal set; }
        /// <summary>
        /// Rainfall in the last three hours
        /// </summary>
        public Length? RainfallLastThreeHours { get; internal set; }
        /// <summary>
        /// Snowfall in the last hour
        /// </summary>
        public Length? SnowfallLastHour { get; internal set; }
        /// <summary>
        /// Snowfall in the last three hours
        /// </summary>
        public Length? SnowfallLastThreeHours { get; internal set; }
        /// <summary>
        /// Two-letter country code
        /// </summary>
        public string CountryCode { get; internal set; }
        /// <summary>
        /// The sunrise time in UTC
        /// </summary>
        public DateTime Sunrise { get; internal set; }
        /// <summary>
        /// The sunset time in UTC
        /// </summary>
        public DateTime Sunset { get; internal set; }
        /// <summary>
        /// The offset for the time zone from UTC
        /// </summary>
        public TimeSpan TimeZoneOffset { get; internal set; }
        /// <summary>
        /// The city id
        /// </summary>
        public int CityId { get; internal set; }
        /// <summary>
        /// The city name
        /// </summary>
        public string CityName { get; internal set; }
        /// <summary>
        /// The time the <see cref="Readings"/> object was fetched, in UTC.
        /// </summary>
        public DateTime FetchedTime { get; internal set; }
        /// <summary>
        /// The time the <see cref="Readings"/> data was updated by OpenWeatherMap, in UTC.
        /// </summary>
        public DateTime MeasuredTime { get; internal set; }
        /// <summary>
        /// The <see cref="OpenWeatherMapCacheException"/> that was thrown while getting the readings.
        /// </summary>
        public OpenWeatherMapCacheException Exception { get; internal set; }
        /// <summary>
        /// Indicates whether the <see cref="Readings"/> were successful or not.
        /// </summary>
        public bool IsSuccessful => (Exception == null);
        /// <summary>
        /// Indicates whether the <see cref="Readings"/> were retrieved from cache or directly from the API.
        /// </summary>
        public bool IsFromCache { get; internal set; }
        /// <summary>
        /// Indicates whether an API request needed to be made or not to service the request.
        /// </summary>
        public bool ApiRequestMade { get; internal set; }

        internal Readings(ApiWeatherResult apiWeatherResult)
        {
            Weather = new List<WeatherCondition>();
            foreach (var weather in apiWeatherResult.Weather)
            {
                Weather.Add(new WeatherCondition
                {
                    ConditionId = weather.Id,
                    Main = weather.Main,
                    Description = weather.Description,
                    IconId = weather.Icon
                });
            }
            Temperature = Temperature.FromKelvins(apiWeatherResult.Main.Temp);
            FeelsLike = Temperature.FromKelvins(apiWeatherResult.Main.FeelsLike);
            Pressure = Pressure.FromHectopascals(apiWeatherResult.Main.Pressure);
            Humidity = RelativeHumidity.FromPercent(apiWeatherResult.Main.Humidity);
            MinimumTemperature = Temperature.FromKelvins(apiWeatherResult.Main.TempMin);
            MaximumTemperature = Temperature.FromKelvins(apiWeatherResult.Main.TempMax);
            if (apiWeatherResult.Main.SeaLevel.HasValue)
                SeaLevelPressure = Pressure.FromHectopascals(apiWeatherResult.Main.SeaLevel.Value);
            if (apiWeatherResult.Main.GrndLevel.HasValue)
                GroundLevelPressure = Pressure.FromHectopascals(apiWeatherResult.Main.GrndLevel.Value);
            WindSpeed = Speed.FromMetersPerSecond(apiWeatherResult.Wind.Speed);
            WindDirection = Angle.FromDegrees(apiWeatherResult.Wind.Deg);
            if (apiWeatherResult.Wind.Gust.HasValue)
                WindGust = Speed.FromMetersPerSecond(apiWeatherResult.Wind.Gust.Value);
            Cloudiness = Ratio.FromPercent(apiWeatherResult.Clouds.All);
            if (apiWeatherResult.Rain != null)
            {
                RainfallLastHour = Length.FromMillimeters(apiWeatherResult.Rain.OneHour);
                RainfallLastThreeHours = Length.FromMillimeters(apiWeatherResult.Rain.ThreeHours);
            }
            if (apiWeatherResult.Snow != null)
            {
                SnowfallLastHour = Length.FromMillimeters(apiWeatherResult.Snow.OneHour);
                SnowfallLastThreeHours = Length.FromMillimeters(apiWeatherResult.Snow.ThreeHours);
            }
            MeasuredTime = DateTimeOffset.FromUnixTimeSeconds(apiWeatherResult.Dt).UtcDateTime;
            CountryCode = apiWeatherResult.Sys.Country;
            Sunrise = DateTimeOffset.FromUnixTimeSeconds(apiWeatherResult.Sys.Sunrise).UtcDateTime;
            Sunset = DateTimeOffset.FromUnixTimeSeconds(apiWeatherResult.Sys.Sunset).UtcDateTime;
            TimeZoneOffset = TimeSpan.FromSeconds(apiWeatherResult.Timezone);
            CityId = apiWeatherResult.Id;
            CityName = apiWeatherResult.Name;
            FetchedTime = DateTime.UtcNow;
        }

        internal Readings(OpenWeatherMapCacheException exception)
        {
            FetchedTime = DateTime.UtcNow;
            Exception = exception;
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        public bool Equals(Readings other)
        {
            if (other == null)
                return false;
            return FetchedTime.Equals(other.FetchedTime) && MeasuredTime.Equals(other.MeasuredTime);
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        public override bool Equals(object obj)
        {
            return obj is Readings other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + FetchedTime.GetHashCode();
                hash = hash * 23 + MeasuredTime.GetHashCode();
                return hash;
            }
        }
    }
}
