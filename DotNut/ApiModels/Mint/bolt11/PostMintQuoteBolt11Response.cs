using System.Text.Json.Serialization;

namespace DotNut.ApiModels;

public class PostMintQuoteBolt11Response
{
    [JsonPropertyName("quote")]
    public string Quote { get; set; }

    [JsonPropertyName("request")]
    public string Request { get; set; }

    /// <summary>
    /// Deprecated since NUT-23 gained the accounting fields. Prefer <see cref="AmountPaid"/> and
    /// <see cref="AmountIssued"/>, which also describe partially issued quotes.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("state")]
    public string? State { get; set; }

    /// <summary>Total paid to the mint for this quote, in <see cref="Unit"/>.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("amount_paid")]
    public ulong? AmountPaid { get; set; }

    /// <summary>Total already issued against this quote, in <see cref="Unit"/>.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("amount_issued")]
    public ulong? AmountIssued { get; set; }

    /// <summary>Unix timestamp of the last change to the amounts. Increases monotonically.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("updated_at")]
    public long? UpdatedAt { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("method")]
    public string? Method { get; set; }

    /// <summary>
    /// What can still be minted: <c>amount_paid - amount_issued</c>. Null when the mint does not
    /// send the accounting fields, in which case only <see cref="State"/> is available.
    /// </summary>
    [JsonIgnore]
    public ulong? Mintable =>
        AmountPaid is { } paid && AmountIssued is { } issued
            ? paid > issued
                ? paid - issued
                : 0
            : null;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("expiry")]
    public int? Expiry { get; set; }

    // 'amount' and 'unit' were recently added to the spec in PostMintQuoteBolt11Response, so they are optional for now
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("amount")]
    public ulong? Amount { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("unit")]
    public string? Unit { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("pubkey")]
    public string? PubKey { get; set; }
}
