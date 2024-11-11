using System.Text.Json.Serialization;

namespace OpenWeatherMap.Cache.Models;

internal class ApiErrorResult
{
    [JsonPropertyName("cod")]
    public int Cod { get; set;  }

    [JsonPropertyName("message")]
    public string Message { get; set; }
}
