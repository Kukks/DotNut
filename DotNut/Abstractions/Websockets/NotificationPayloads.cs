using System.Text.Json.Serialization;

namespace DotNut.Abstractions.Websockets;

public class MintQuoteNotificationPayload
{
    [JsonPropertyName("quote")]
    public string Quote { get; set; } = string.Empty;

    [JsonPropertyName("request")]
    public string Request { get; set; } = string.Empty;

    [JsonPropertyName("paid")]
    public bool Paid { get; set; }

    [JsonPropertyName("expiry")]
    public long? Expiry { get; set; }
}

public class MeltQuoteNotificationPayload
{
    [JsonPropertyName("quote")]
    public string Quote { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public ulong Amount { get; set; }

    [JsonPropertyName("fee_reserve")]
    public ulong FeeReserve { get; set; }

    [JsonPropertyName("paid")]
    public bool Paid { get; set; }

    [JsonPropertyName("expiry")]
    public long? Expiry { get; set; }

    [JsonPropertyName("payment_preimage")]
    public string? PaymentPreimage { get; set; }

    [JsonPropertyName("change")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object[]? Change { get; set; }
}

public class ProofStateNotificationPayload
{
    [JsonPropertyName("Y")]
    public string Y { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public ProofState State { get; set; }

    [JsonPropertyName("witness")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Witness { get; set; }
}
