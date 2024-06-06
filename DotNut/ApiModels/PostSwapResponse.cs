using System.Text.Json.Serialization;

namespace DotNut.ApiModels;

public class PostSwapResponse
{
    [JsonPropertyName("signatures")] public BlindSignature[] Signatures { get; set; }
}