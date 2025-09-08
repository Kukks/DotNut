using System.Text.Json.Serialization;

namespace DotNut.ApiModels.Melt.bolt12;

public class PostMeltQuoteBolt12Response
{
    [JsonPropertyName("quote")] public string Quote { get; set; }

    [JsonPropertyName("request")] public string Request { get; set; }

    [JsonPropertyName("amount")] public ulong Amount { get; set; }

    [JsonPropertyName("fee_reserve")] public ulong FeeReserve { get; set; }

    [JsonPropertyName("state")] public string State { get; set; }
    
    [JsonPropertyName("expiry")] public int Expiry { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("payment_preimage")] public string PaymentPreimage { get; set; }

}