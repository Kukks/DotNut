using System.Text.Json.Serialization;

namespace DotNut.ApiModels;

public class PostRestoreResponse
{
    [JsonPropertyName("outputs")]
    public BlindedMessage[] Outputs { get; set; }
    [JsonPropertyName("signatures")]
    public BlindSignature[] Signatures { get; set; }
}