using System.Text.Json.Serialization;

namespace DotNuts;

public class PostSwapRequest
{
    [JsonPropertyName("inputs")] public Proof[] Inputs { get; set; }
    [JsonPropertyName("outputs")] public BlindedMessage[] Outputs { get; set; }
}