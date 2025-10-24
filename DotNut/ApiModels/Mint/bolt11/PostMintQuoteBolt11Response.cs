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
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("expiry")] 
    public int? Expiry { get; set; }
    
    // 'amount' and 'unit' were recently added to the spec in PostMintQuoteBolt11Response, so they are optional for now
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("amount")]
    public ulong? Amount { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("unit")]
    public string? Unit {get; set;}
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("pubkey")]
    public string PubKey {get; set;}
}