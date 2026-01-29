using System.Text.Json.Serialization;

namespace DotNut.ApiModels.Melt;

public class MeltQuoteRequestOptions
{
    [JsonPropertyName("amountless")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AmountlessMeltQuoteOptions? Amountless { get; set; }
}

public class AmountlessMeltQuoteOptions
{
    [JsonPropertyName("amount_msat")]
    public ulong AmountMsat { get; set; }
}
