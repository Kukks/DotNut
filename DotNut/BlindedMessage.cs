using System.Text.Json.Serialization;

namespace DotNut;

public class BlindedMessage
{
    [JsonPropertyName("amount")] public int Amount { get; set; }
    [JsonPropertyName("id")] public KeysetId Id { get; set; }
    [JsonPropertyName("B_")] public PubKey B_ { get; set; }
    [JsonPropertyName("witness")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? Witness { get; set; }
}