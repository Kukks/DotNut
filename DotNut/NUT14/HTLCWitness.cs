using System.Text.Json.Serialization;

namespace DotNut;

public class HTLCWitness : P2PKWitness
{
    // this field is nullable now, because after locktime expiry only signatures are needed.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("preimage")]
    public string? Preimage { get; set; }
}
