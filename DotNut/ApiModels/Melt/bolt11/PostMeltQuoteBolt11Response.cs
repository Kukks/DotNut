using System.Text.Json.Serialization;

namespace DotNut.ApiModels;

public class PostMeltQuoteBolt11Response
{
    [JsonPropertyName("quote")]
    public string Quote { get; set; }

    [JsonPropertyName("amount")]
    public ulong Amount { get; set; }

    [JsonPropertyName("fee_reserve")]
    public int FeeReserve { get; set; }

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
