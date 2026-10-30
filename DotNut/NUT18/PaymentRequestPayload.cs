using System.Text.Json.Serialization;
using DotNut.JsonConverters;

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

    [JsonConverter(typeof(SkipUnknownKeysetsJsonConverter))]
    [JsonPropertyName("proofs")]
    public Proof[] Proofs { get; set; }
}
