using System.Text.Json.Serialization;

namespace DotNut.ApiModels;

public class PostMeltRequest
{
    [JsonPropertyName("quote")]
    public string Quote { get; set; }

    [JsonPropertyName("inputs")]
    public Proof[] Inputs { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("outputs")]
    public BlindedMessage[]? Outputs { get; set; }

    /// <summary>
    /// Asks the mint to return as soon as the request validates, leaving the quote PENDING while
    /// the payment runs. Replaces the <c>Prefer: respond-async</c> header. Ignored by mints that
    /// do not support it for the method, and unnecessary for methods whose NUT requires async
    /// execution anyway.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("prefer_async")]
    public bool? PreferAsync { get; set; }
}
