using System.Text.Json.Serialization;

public class CashuProtocolError
{
    [JsonPropertyName("detail")] public string Detail { get; set; }
    [JsonPropertyName("code")] public int Code { get; set; }
}