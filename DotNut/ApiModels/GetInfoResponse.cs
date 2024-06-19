using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNut.ApiModels;

public class GetInfoResponse
{
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("pubkey")] public string Pubkey { get; set; }
    [JsonPropertyName("version")] public string Version { get; set; }
    [JsonPropertyName("description")] public string Description { get; set; }
    [JsonPropertyName("description_long")] public string DescriptionLong { get; set; }
    [JsonPropertyName("contact")] public List<ContactInfo> Contact { get; set; }
    [JsonPropertyName("motd")] public string Motd { get; set; }
    [JsonPropertyName("nuts")] public Dictionary<string, JsonDocument> Nuts { get; set; }
}

public class ContactInfo
{
    [JsonPropertyName("method")] public string Method { get; set; }
    [JsonPropertyName("info")] public string Info { get; set; }
}