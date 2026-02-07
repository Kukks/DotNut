using System.Text.Json.Serialization;

namespace DotNut;

public class MeltMethodSetting
{
    [JsonPropertyName("method")]
    public string Method { get; set; }

    [JsonPropertyName("unit")]
    public string Unit { get; set; }

    [JsonPropertyName("min_amount")]
    public ulong? Min { get; set; }

    [JsonPropertyName("max_amount")]
    public ulong? Max { get; set; }
}
