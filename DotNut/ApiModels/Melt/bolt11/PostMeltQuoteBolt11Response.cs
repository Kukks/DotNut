using System.Text.Json.Serialization;

namespace DotNut.ApiModels;

public class PostMeltQuoteBolt11Response
{
    [JsonPropertyName("quote")]
    public string Quote { get; set; }

    /// <summary>The method-specific payment target the quote routes to.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("request")]
    public string? Request { get; set; }

    [JsonPropertyName("amount")]
    public ulong Amount { get; set; }

    /// <summary>
    /// Extra reserve for the method, on top of <see cref="Amount"/>. Optional since NUT-05;
    /// the inputs have to cover <c>amount + fee_reserve + input fee</c> (NUT-02).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("fee_reserve")]
    public ulong? FeeReserve { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("unit")]
    public string? Unit { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("method")]
    public string? Method { get; set; }

    [JsonPropertyName("state")]
    public string State { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("expiry")]
    public int? Expiry { get; set; }

    [JsonPropertyName("payment_preimage")]
    public string? PaymentPreimage { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("change")]
    public BlindSignature[]? Change { get; set; }
}
