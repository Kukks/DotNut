using System.Diagnostics;
using System.Text.Json.Serialization;

namespace DotNut.ApiModels;

public class PostMintQuoteBolt11Request
{
  
    [JsonPropertyName("amount")] 
    public int Amount {get; set;}
    
    [JsonPropertyName("unit")] 
    public string Unit {get; set;}
    
    [JsonPropertyName("description")] 
    public string? Description {get; set;}
}