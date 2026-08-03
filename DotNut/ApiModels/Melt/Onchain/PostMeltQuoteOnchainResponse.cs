using System.Text.Json.Serialization;

namespace DotNut.ApiModels.Melt.Onchain;

public class PostMeltQuoteOnchainResponse
{
    [JsonPropertyName("quote")]
    public string Quote { get; set; }
    
    [JsonPropertyName("amount")]
    public ulong Amount { get; set; }
    
    [JsonPropertyName("unit")]
    public string Unit { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("method")]
    public string? Method { get; set; }

    [JsonPropertyName("state")]
    public string State { get; set; }
    
    [JsonPropertyName("expiry")]
    public ulong Expiry { get; set; }
    
    [JsonPropertyName("request")]
    public string Request { get; set; }
    
    [JsonPropertyName("fee_options")]
    public FeeOptionsObj[] FeeOptions { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyName("selected_fee_index")]
    public ulong? SelectedFeeIndex { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyName("outpoint")]
    public string? Outpoint { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyName("change")]
    public BlindSignature[]? Change { get; set; }

    public class FeeOptionsObj
    {
        [JsonPropertyName("fee_index")]
        public ulong FeeIndex { get; set; }
        [JsonPropertyName("fee_reserve")]
        public ulong FeeReserve { get; set; }
        [JsonPropertyName("estimated_blocks")]
        public ulong EstimatedBlocks { get; set; }
    }
}