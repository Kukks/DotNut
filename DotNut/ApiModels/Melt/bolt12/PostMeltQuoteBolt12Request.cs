using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace DotNut.ApiModels.Melt.bolt12;

public class PostMeltQuoteBolt12Request
{
    [JsonPropertyName("request")]
    public string Request { get; set; }
    
    [JsonPropertyName("unit")]
    public string Unit { get; set; }
    
    [JsonPropertyName("options")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MeltQuoteRequestOptions? Options { get; set; }
}

