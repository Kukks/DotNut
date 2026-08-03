using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace DotNut.ApiModels;

public class PostMintRequest
{
    [JsonPropertyName("quote")]
    public string Quote { get; set; }

    [JsonPropertyName("outputs")]
    public BlindedMessage[] Outputs { get; set; }

    // this should be non-nullable for onchain and bolt12
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("signature")]
    public string? Signature { get; set; }
}
