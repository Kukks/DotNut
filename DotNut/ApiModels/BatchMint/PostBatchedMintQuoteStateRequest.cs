using System.Text.Json.Serialization;

namespace DotNut.ApiModels;

public class PostBatchedMintQuoteStateRequest
{
    [JsonPropertyName("quotes")]
    public string[] Quotes { get; set; }
}
