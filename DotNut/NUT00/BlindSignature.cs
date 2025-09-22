using System.Text.Json.Serialization;
using DotNut.JsonConverters;

namespace DotNut;

public class BlindSignature
{
    [JsonPropertyName("amount")] public ulong Amount { get; set; }

    [JsonConverter(typeof(KeysetIdJsonConverter))]
    [JsonPropertyName("id")]
    public KeysetId Id { get; set; }

    [JsonPropertyName("C_")] public PubKey C_ { get; set; }
    
    
    [JsonPropertyName("dleq")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DLEQProof? DLEQ { get; set; }
}