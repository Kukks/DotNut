using System.Text.Json.Serialization;

namespace DotNut;

public class MintMethodSetting
{
    [JsonPropertyName("method")] public string Method { get; set; }
    [JsonPropertyName("unit")] public List<Proof> Unit { get; set; }
    [JsonPropertyName("min_amount")] public int? Min { get; set; }
    [JsonPropertyName("max_amount")] public int? Max { get; set; }
}

