using System.Text.Json.Serialization;

namespace DotNut.ApiModels;

public class ContactInfo
{
    [JsonPropertyName("method")] public string Method { get; set; }
    [JsonPropertyName("info")] public string Info { get; set; }
}