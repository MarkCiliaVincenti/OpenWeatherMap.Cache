using Newtonsoft.Json;
using System.Collections.Generic;

namespace OpenWeatherMap.Cache.Models
{
    internal class ApiWeatherResult
    {
        [JsonProperty("coord")]
        public ApiCoord Coord { get; set; }
        internal class ApiCoord
        {
            [JsonProperty("lon")]
            public float Lon { get; set; }
            [JsonProperty("lat")]
            public float Lat { get; set; }
        }

        [JsonProperty("weather")]
        public IEnumerable<ApiWeather> Weather { get; set; }
        internal class ApiWeather
        {
            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("main")]
            public string Main { get; set; }

            [JsonProperty("description")]
            public string Description { get; set; }

            [JsonProperty("icon")]
            public string Icon { get; set; }
        }

        [JsonProperty("base")]
        public string Base { get; set; }

        [JsonProperty("main")]
        public ApiMain Main { get; set; }
        internal class ApiMain
        {
            [JsonProperty("temp")]
            public float Temp { get; set; }

            [JsonProperty("feels_like")]
            public float FeelsLike { get; set; }

            [JsonProperty("temp_min")]
            public float TempMin { get; set; }

            [JsonProperty("temp_max")]
            public float TempMax { get; set; }

            [JsonProperty("pressure")]
            public int Pressure { get; set; }

            [JsonProperty("humidity")]
            public int Humidity { get; set; }

            [JsonProperty("sea_level")]
            public int? SeaLevel { get; set; }

            [JsonProperty("grnd_level")]
            public int? GrndLevel { get; set; }
        }

        [JsonProperty("visibility")]
        public int Visibility { get; set; }

        [JsonProperty("wind")]
        public ApiWind Wind { get; set; }
        internal class ApiWind
        {
            [JsonProperty("speed")]
            public float Speed { get; set; }

            [JsonProperty("deg")]
            public float Deg { get; set; }

            [JsonProperty("gust")]
            public float? Gust { get; set; }
        }

        [JsonProperty("clouds")]
        public ApiClouds Clouds { get; set; }
        internal class ApiClouds
        {
            [JsonProperty("all")]
            public int All { get; set; }
        }

        [JsonProperty("rain")]
        public ApiVolume Rain { get; set; }

        [JsonProperty("snow")]
        public ApiVolume Snow { get; set; }

        internal class ApiVolume
        {
            [JsonProperty("1h")]
            public float OneHour { get; set; }

            [JsonProperty("3h")]
            public float ThreeHours { get; set; }
        }

        [JsonProperty("dt")]
        public long Dt { get; set; }

        [JsonProperty("sys")]
        public ApiSys Sys { get; set; }
        internal class ApiSys
        {
            [JsonProperty("type")]
            public int? Type { get; set; }

            [JsonProperty("id")]
            public int? Id { get; set; }

            [JsonProperty("message")]
            public double? Message { get; set; }

            [JsonProperty("country")]
            public string Country { get; set; }

            [JsonProperty("sunrise")]
            public long Sunrise { get; set; }
            
            [JsonProperty("sunset")]
            public long Sunset { get; set; }
        }

        [JsonProperty("timezone")]
        public int Timezone { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("cod")]
        public int Cod { get; set; }
    }
}
