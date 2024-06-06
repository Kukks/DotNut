using System.Text.Json.Serialization;

namespace DotNut.Api;

public class CashuProtocolError
{
    [JsonPropertyName("detail")] public string Detail { get; set; }
    [JsonPropertyName("code")] public int Code { get; set; }
}