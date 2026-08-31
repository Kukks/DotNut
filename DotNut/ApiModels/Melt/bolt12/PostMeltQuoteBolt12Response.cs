using System.Text.Json.Serialization;

namespace DotNut.ApiModels.Melt.bolt12;

public class PostMeltQuoteBolt12Response
{
    [JsonPropertyName("quote")]
    public string Quote { get; set; }

    [JsonPropertyName("request")]
    public string Request { get; set; }

    [JsonPropertyName("amount")]
    public ulong Amount { get; set; }

    [JsonPropertyName("unit")]
    public string Unit { get; set; }

    /// <summary>
    /// Extra reserve for the method, on top of <see cref="Amount"/>. Optional since NUT-05;
    /// the inputs have to cover <c>amount + fee_reserve + input fee</c> (NUT-02).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("fee_reserve")]
    public ulong? FeeReserve { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("method")]
    public string? Method { get; set; }

    [JsonPropertyName("state")]
    public string State { get; set; }

    [JsonPropertyName("expiry")]
    public int Expiry { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("payment_preimage")]
    public string? PaymentPreimage { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("change")]
    public BlindSignature[]? Change { get; set; }
}
