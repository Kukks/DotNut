using System.Text.Json.Serialization;

namespace DotNut.ApiModels;

public class PostMeltQuoteBolt11Request
{
    
    [JsonPropertyName("request")] 
    public string Request { get; set; }

    [JsonPropertyName("unit")] 
    public string Unit { get; set; }
}