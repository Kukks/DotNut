using System.Text.Json.Serialization;

namespace DotNut.ApiModels;

public class GetKeysResponse
{
    [JsonPropertyName("keysets")]
    public KeysetItemResponse[] Keysets { get; set; }

    public class KeysetItemResponse
    {
        [JsonPropertyName("id")]
        public KeysetId Id { get; set; }

        [JsonPropertyName("unit")]
        public string Unit { get; set; }

        [JsonPropertyName("active")]
        public bool? Active { get; set; } // nullable until wider adoption

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("input_fee_ppk")]
        public ulong? InputFeePpk { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("final_expiry")]
        public ulong? FinalExpiry { get; set; }

        [JsonPropertyName("keys")]
        public Keyset Keys { get; set; }
    }
}
