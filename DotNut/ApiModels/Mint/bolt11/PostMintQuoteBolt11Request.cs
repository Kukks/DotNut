using System.Diagnostics;
using System.Text.Json.Serialization;

namespace DotNut.ApiModels;

public class PostMintQuoteBolt11Request
{
  
    [JsonPropertyName("amount")] 
    public ulong Amount {get; set;}
    
    [JsonPropertyName("unit")] 
    public string Unit {get; set;}
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("pubkey")]
    public string? Pubkey {get; set;}
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("description")] 
    public string? Description {get; set;}
}