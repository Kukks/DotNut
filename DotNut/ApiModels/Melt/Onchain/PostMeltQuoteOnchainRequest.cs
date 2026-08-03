using System.Text.Json.Serialization;

namespace DotNut.ApiModels.Melt.Onchain;

public class PostMeltQuoteOnchainRequest
{
    [JsonPropertyName("request")]
    public string Request { get; set; }

    [JsonPropertyName("unit")]
    public string Unit { get; set; }
    
    [JsonPropertyName("amount")]
    public ulong Amount { get; set; }
}