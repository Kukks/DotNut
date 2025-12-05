using System.Text.Json.Serialization;

namespace DotNut;

public class DLEQProof: DLEQ
{
    [JsonPropertyName("r")] public PrivKey R { get; set; }
}