using System.Text.Json.Serialization;

namespace DotNut.ApiModels;

public class PostRestoreRequest
{
    [JsonPropertyName("outputs")]
    public BlindedMessage[] Outputs { get; set; }
}
