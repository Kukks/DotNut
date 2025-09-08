using System.Text.Json.Serialization;

namespace DotNut.ApiModels.Mint.bolt12;

public class PostMintQuoteBolt12Request
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("amount")]
    public ulong? Amount { get; set; }
    
    [JsonPropertyName("unit")]
    public string Unit {get; set;}
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("pubkey")]
    public string Pubkey { get; set; }
    
}