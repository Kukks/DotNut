using System.Text.Json.Serialization;

namespace DotNut.ApiModels;

public class PostMintBolt11Response
{
    [JsonPropertyName("signatures")]
    public BlindSignature[] Signatures { get; set; }
}