using System.Text.Json.Serialization;

namespace DotNut.ApiModels.Mint.bolt12;

public class PostMintQuoteBolt12Response
{
    [JsonPropertyName("quote")]
    public string Quote { get; set; }

    [JsonPropertyName("request")]
    public string Request { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("amount")]
    public ulong? Amount { get; set; }

    [JsonPropertyName("unit")]
    public string Unit { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("expiry")]
    public int? Expiry { get; set; }

    [JsonPropertyName("pubkey")]
    public string Pubkey { get; set; }

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

    /// <summary>What can still be minted: <c>amount_paid - amount_issued</c>.</summary>
    [JsonIgnore]
    public ulong Mintable => AmountPaid > AmountIssued ? AmountPaid - AmountIssued : 0;
}
