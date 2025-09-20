using System.Text.Json.Serialization;

namespace DotNut.ApiModels;

public class WebSocketSupport
{
    [JsonPropertyName("method")]
    public string Method { get; set; }
    [JsonPropertyName("unit")]
    public string Unit {get; set;}
    [JsonPropertyName("commands")]
    public string[] Commands { get; set; }
}