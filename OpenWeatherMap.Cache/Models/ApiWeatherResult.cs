// Copyright (c) All contributors.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenWeatherMap.Cache.Models;

[JsonSourceGenerationOptions(WriteIndented = false, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ApiWeatherResult))]
[JsonSerializable(typeof(ApiCoord))]
[JsonSerializable(typeof(ApiWeather))]
[JsonSerializable(typeof(ApiMain))]
[JsonSerializable(typeof(ApiWind))]
[JsonSerializable(typeof(ApiClouds))]
[JsonSerializable(typeof(ApiVolume))]
[JsonSerializable(typeof(ApiSys))]
internal partial class ApiWeatherResultJsonContext : JsonSerializerContext
{
}

internal struct ApiCoord
{
    [JsonPropertyName("lon")]
    public float Lon { get; set; }
    [JsonPropertyName("lat")]
    public float Lat { get; set; }
}

internal struct ApiWeather
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("main")]
    public string Main { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("icon")]
    public string Icon { get; set; }
}

internal struct ApiMain
{
    [JsonPropertyName("temp")]
    public float Temp { get; set; }

    [JsonPropertyName("feels_like")]
    public float FeelsLike { get; set; }

    [JsonPropertyName("temp_min")]
    public float TempMin { get; set; }

    [JsonPropertyName("temp_max")]
    public float TempMax { get; set; }

    [JsonPropertyName("pressure")]
    public int Pressure { get; set; }

    [JsonPropertyName("humidity")]
    public int Humidity { get; set; }

    [JsonPropertyName("sea_level")]
    public int? SeaLevel { get; set; }

    [JsonPropertyName("grnd_level")]
    public int? GrndLevel { get; set; }
}

internal struct ApiWind
{
    [JsonPropertyName("speed")]
    public float Speed { get; set; }

    [JsonPropertyName("deg")]
    public float Deg { get; set; }

    [JsonPropertyName("gust")]
    public float? Gust { get; set; }
}

internal struct ApiClouds
{
    [JsonPropertyName("all")]
    public int All { get; set; }
}

internal struct ApiVolume
{
    [JsonPropertyName("1h")]
    public float OneHour { get; set; }

    [JsonPropertyName("3h")]
    public float ThreeHours { get; set; }
}

internal struct ApiSys
{
    [JsonPropertyName("type")]
    public int? Type { get; set; }

    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("message")]
    public double? Message { get; set; }

    [JsonPropertyName("country")]
    public string Country { get; set; }

    [JsonPropertyName("sunrise")]
    public long Sunrise { get; set; }

    [JsonPropertyName("sunset")]
    public long Sunset { get; set; }
}

internal struct ApiWeatherResult
{
    [JsonPropertyName("coord")]
    public ApiCoord? Coord { get; set; }

    [JsonPropertyName("weather")]
    public IEnumerable<ApiWeather> Weather { get; set; }

    [JsonPropertyName("base")]
    public string Base { get; set; }

    [JsonPropertyName("main")]
    public ApiMain Main { get; set; }

    [JsonPropertyName("visibility")]
    public int Visibility { get; set; }

    [JsonPropertyName("wind")]
    public ApiWind Wind { get; set; }

    [JsonPropertyName("clouds")]
    public ApiClouds Clouds { get; set; }

    [JsonPropertyName("rain")]
    public ApiVolume? Rain { get; set; }

    [JsonPropertyName("snow")]
    public ApiVolume? Snow { get; set; }

    [JsonPropertyName("dt")]
    public long Dt { get; set; }

    [JsonPropertyName("sys")]
    public ApiSys Sys { get; set; }

    [JsonPropertyName("timezone")]
    public int Timezone { get; set; }

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("cod")]
    public int Cod { get; set; }
}
