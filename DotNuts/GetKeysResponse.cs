using System.Text.Json.Serialization;

namespace DotNuts;

public class GetKeysResponse
{
    [JsonPropertyName("keysets")] public Keyset[] Keysets { get; set; }

    public class Keyset
    {
        [JsonPropertyName("id")] public KeysetId Id { get; set; }
        [JsonPropertyName("unit")] public string Unit { get; set; }
        [JsonPropertyName("keys")] public Dictionary<int, string> Keys { get; set; }
    }
}