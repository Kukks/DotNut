using System.Text.Json.Serialization;
using DotNut.JsonConverters;

//
// [JsonConverter(typeof(HexSecretJsonConverter))]
// public class HexSecret:IProofSecret
// {
//     public byte[] Secret { get; set; }
// }
//


namespace DotNut;

public class Proof
{
    [JsonPropertyName("amount")] public int Amount { get; set; }

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
    
}