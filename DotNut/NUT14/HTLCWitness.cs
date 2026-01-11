using System.Text.Json.Serialization;

namespace DotNut;

public class HTLCWitness : P2PKWitness
{
    [JsonPropertyName("preimage")]
    public string Preimage { get; set; }
}
