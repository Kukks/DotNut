using System.Text.Json.Serialization;

namespace DotNut.ApiModels;

public class GetKeysResponse
{
    [JsonPropertyName("keysets")] public KeysetItemResponse[] Keysets { get; set; }

    public class KeysetItemResponse
    {
        [JsonPropertyName("id")] public KeysetId Id { get; set; }
        [JsonPropertyName("unit")] public string Unit { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("final_expiry")] public ulong? FinalExpiry { get; set; }
        [JsonPropertyName("keys")] public Keyset Keys { get; set; }
    }
}