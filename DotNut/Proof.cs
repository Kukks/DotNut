using System.Text.Json.Serialization;
using DotNut.JsonConverters;


namespace DotNut;

public class Proof
{
    [JsonPropertyName("amount")] public ulong Amount { get; set; }

    [JsonConverter(typeof(KeysetIdJsonConverter))]
    [JsonPropertyName("id")]
    public KeysetId Id { get; set; }

    [JsonPropertyName("secret")] public ISecret Secret { get; set; }

    [JsonPropertyName("C")] public PubKey C { get; set; }

    [JsonPropertyName("witness")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Witness { get; set; }
    
    [JsonPropertyName("dleq")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DLEQProof? DLEQ { get; set; }
    
    [JsonPropertyName("p2pk_r")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? P2PkR { get; set; } // must not be exposed to mint. strip when possible
    
}