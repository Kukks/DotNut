using System.Text.Json.Serialization;
using DotNut.ApiModels.Melt;

namespace DotNut.ApiModels;

public class PostMeltQuoteBolt11Request
{
    
    [JsonPropertyName("request")] 
    public string Request { get; set; }

    [JsonPropertyName("unit")] 
    public string Unit { get; set; }
    
    [JsonPropertyName("options")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MeltQuoteRequestOptions? Options { get; set; }
}