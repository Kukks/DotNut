using System.Text.Json.Serialization;

namespace DotNut.ApiModels;

public class PostMintResponse
{
    [JsonPropertyName("signatures")]
    public BlindSignature[] Signatures { get; set; }
}