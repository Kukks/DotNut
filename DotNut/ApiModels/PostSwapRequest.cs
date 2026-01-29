using System.Text.Json.Serialization;

namespace DotNut.ApiModels;

public class PostSwapRequest
{
    [JsonPropertyName("inputs")]
    public Proof[] Inputs { get; set; }

    [JsonPropertyName("outputs")]
    public BlindedMessage[] Outputs { get; set; }
}
