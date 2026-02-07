using System.Text.Json.Serialization;

namespace DotNut.ApiModels;

public class PostCheckStateRequest
{
    [JsonPropertyName("Ys")]
    public string[] Ys { get; set; }
}
