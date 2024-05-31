using System.Text.Json.Serialization;

namespace DotNuts;

public class PostSwapResponse
{
    [JsonPropertyName("signatures")] public BlindSignature[] Signatures { get; set; }
}