using System.Text.Json.Serialization;

namespace DotNut;

public class P2PKWitness
{
    [JsonPropertyName("signatures")]
    public string[] Signatures { get; set; } = Array.Empty<string>();
}
