using System.Text.Json.Serialization;

public class DLEQ
{
    [JsonPropertyName("e")] public PrivKey E { get; set; }
    [JsonPropertyName("s")] public PrivKey S { get; set; }
    [JsonPropertyName("r")] public PrivKey R { get; set; }
}