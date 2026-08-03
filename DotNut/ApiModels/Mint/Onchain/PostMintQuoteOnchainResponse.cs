using System.Text.Json.Serialization;

namespace DotNut.ApiModels.Onchain;

public class PostMintQuoteOnchainResponse
{
    [JsonPropertyName("quote")]
    public string Quote { get; set; }
    
    [JsonPropertyName("request")]
    public string Request { get; set; }
    
    [JsonPropertyName("unit")]
    public string Unit { get; set; }
    
    [JsonPropertyName("expiry")]
    public ulong? Expiry { get; set; }
    
    [JsonPropertyName("pubkey")]
    public string PubKey { get; set; }
    
    [JsonPropertyName("amount_paid")]
    public ulong AmountPaid { get; set; }

    [JsonPropertyName("amount_issued")]
    public ulong AmountIssued { get; set; }

    /// <summary>Unix timestamp of the last change to the amounts. Increases monotonically.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("updated_at")]
    public long? UpdatedAt { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("method")]
    public string? Method { get; set; }

    /// <summary>
    /// What can still be minted: <c>amount_paid - amount_issued</c>. An onchain address can be
    /// paid more than once, so this grows as UTXOs confirm and a wallet may mint repeatedly
    /// against the same quote, up to this amount each time.
    /// </summary>
    [JsonIgnore]
    public ulong Mintable => AmountPaid > AmountIssued ? AmountPaid - AmountIssued : 0;
}