using System.Text.Json.Serialization;

namespace DotNut;

public class DLEQ
{
    [JsonPropertyName("e")]
    public PrivKey E { get; set; }

    [JsonPropertyName("s")]
    public PrivKey S { get; set; }
}
