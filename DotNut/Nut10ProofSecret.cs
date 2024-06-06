using System.Text.Json.Serialization;

namespace DotNut;

public class Nut10ProofSecret
{
    
    [JsonPropertyName("nonce")]
    public string Nonce { get; set; }
    [JsonPropertyName("data")]
    public string Data { get; set; }
    [JsonPropertyName("tags")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[][]? Tags { get; set; }
}