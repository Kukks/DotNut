using System.Text.Json.Serialization;

namespace DotNut.ApiModels;

public class PostCheckStateResponse
{
    [JsonPropertyName("states")]
    public StateResponseItem[] States { get; set; }
}
