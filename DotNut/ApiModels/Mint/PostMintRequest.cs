using System.Text.Json.Serialization;

namespace DotNut.ApiModels;

public class PostMintRequest
{
    [JsonPropertyName("quote")]
    public string Quote { get; set; }

    [JsonPropertyName("outputs")]
    public BlindedMessage[] Outputs { get; set; }
}