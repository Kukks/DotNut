using System.Text.Json.Serialization;

namespace DotNut.ApiModels.Onchain;

public class PostMintQuoteOnchainRequest
{
    [JsonPropertyName("unit")]
    public string Unit { get; set; }
    [JsonPropertyName("pubkey")]
    public string PublicKey { get; set; }
}