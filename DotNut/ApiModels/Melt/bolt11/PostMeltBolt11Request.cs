using System.Text.Json.Serialization;

namespace DotNut.ApiModels;

public class PostMeltBolt11Request
{

    [JsonPropertyName("quote")]
    public string Quote { get; set; }
    
    [JsonPropertyName("inputs")]
    public Proof[] Inputs { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("outputs")]
    public BlindedMessage[]? Outputs { get; set; }
}