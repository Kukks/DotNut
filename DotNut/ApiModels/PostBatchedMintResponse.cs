using System.Text.Json.Serialization;

namespace DotNut.ApiModels;

public class PostBatchedMintResponse
{
    [JsonPropertyName("signatures")]
    public BlindSignature[] Signatures { get; set; }
}