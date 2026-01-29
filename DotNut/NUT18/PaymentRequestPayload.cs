using System.Text.Json.Serialization;

namespace DotNut;

public class PaymentRequestPayload
{
    [JsonPropertyName("id")]
    public string PaymentId { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("memo")]
    public string? Memo { get; set; }

    [JsonPropertyName("mint")]
    public string Mint { get; set; }

    [JsonPropertyName("unit")]
    public string Unit { get; set; }

    [JsonPropertyName("proofs")]
    public Proof[] Proofs { get; set; }
}
