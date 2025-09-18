using System.Text.Json.Serialization;

namespace DotNut.ApiModels;

public class PostMeltRequest
{

    [JsonPropertyName("quote")]
    public string Quote { get; set; }
    
    [JsonPropertyName("inputs")]
    public Proof[] Inputs { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("outputs")]
    public BlindedMessage[]? Outputs { get; set; }
}