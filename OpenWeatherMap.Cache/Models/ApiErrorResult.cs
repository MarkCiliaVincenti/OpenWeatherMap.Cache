using System.Text.Json.Serialization;

namespace OpenWeatherMap.Cache.Models;

internal sealed class ApiErrorResult
{
    [JsonPropertyName("cod")]
    public int Cod { get; set;  }

    [JsonPropertyName("message")]
    public string Message { get; set; }
}
