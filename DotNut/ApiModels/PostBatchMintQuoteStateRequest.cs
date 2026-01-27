using System.Text.Json.Serialization;

namespace DotNut.ApiModels;

public class PostBatchMintQuoteStateRequest
{
    [JsonPropertyName("quotes")]
    public string[] Quotes { get; set; }
}