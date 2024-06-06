using System.Text.Json.Serialization;

namespace DotNut;

public class MultipathPaymentSetting 
{
    [JsonPropertyName("method")] public string Method { get; set; }
    [JsonPropertyName("unit")] public List<Proof> Unit { get; set; }
    [JsonPropertyName("mpp")] public bool MultiPathPayments { get; set; }
}