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

        /// <summary>
        /// The keyset public keys. For secp256k1 (v0/v1/v2) keysets this is a <see cref="Keyset"/>;
        /// for BLS12-381 v3 keysets this is a <see cref="BlsKeyset"/>. Use <c>is BlsKeyset</c> to distinguish.
        /// </summary>
        [JsonPropertyName("keys")]
        [JsonConverter(typeof(JsonConverters.AnyKeysetJsonConverter))]
        public Keyset Keys { get; set; }
    }
}
