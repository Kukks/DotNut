using System.Text.Json.Serialization;

namespace DotNut.ApiModels.Mint.bolt12;

public class PostMintQuoteBolt12Response
{
    [JsonPropertyName("quote")]
    public string Quote { get; set; }
    
    [JsonPropertyName("request")]
    public string Request {get; set;}
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("amount")]
    public ulong? Amount { get; set; }
    
    [JsonPropertyName("unit")]
    public string Unit {get; set;}
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("expiry")]
    public int? Expiry {get; set;} 
    
    [JsonPropertyName("pubkey")]
    public string Pubkey {get; set;}
    
    [JsonPropertyName("amount_paid")]
    public ulong AmountPaid {get; set;}
    
    [JsonPropertyName("amount_issued")]
    public ulong AmountIssued {get; set;}
}