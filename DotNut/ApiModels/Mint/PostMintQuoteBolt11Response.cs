using System.Text.Json.Serialization;

namespace DotNut.ApiModels;

public class PostMintQuoteBolt11Response
{
    [JsonPropertyName("quote")] 
    public string Quote { get; set; }
    
    [JsonPropertyName("request")] 
    public string Request { get; set; }
    
    [JsonPropertyName("state")] 
    public string State { get; set; }
    
    [JsonPropertyName("expiry")] 
    public int Expiry { get; set; }
}