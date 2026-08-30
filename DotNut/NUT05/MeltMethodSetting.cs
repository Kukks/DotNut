using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNut;

public class MeltMethodSetting
{
    [JsonPropertyName("method")]
    public string Method { get; set; }

    [JsonPropertyName("unit")]
    public string Unit { get; set; }

    /// <summary>
    /// Human-readable name for the method. Null or absent when the mint does not send one, in
    /// which case <see cref="DisplayName"/> derives it from <see cref="Method"/>.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("method_name")]
    public string? MethodName { get; set; }

    /// <inheritdoc cref="MethodNameFallback.DisplayNameFor"/>
    [JsonIgnore]
    public string DisplayName => MethodNameFallback.DisplayNameFor(Method, MethodName);

    [JsonPropertyName("min_amount")]
    public ulong? Min { get; set; }

    [JsonPropertyName("max_amount")]
    public ulong? Max { get; set; }

    /// <summary>Method-specific settings, defined by the method's own NUT.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("options")]
    public JsonDocument? Options { get; set; }
}
