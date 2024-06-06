using System.Text.Json.Serialization;

namespace DotNut.ApiModels;

public class GetKeysetsResponse
{
    [JsonPropertyName("keysets")] public Keyset[] Keysets { get; set; }

    public class Keyset
    {
        [JsonPropertyName("id")] public KeysetId Id { get; set; }
        [JsonPropertyName("unit")] public string Unit { get; set; }
        [JsonPropertyName("active")] public bool Active { get; set; }
    }
}