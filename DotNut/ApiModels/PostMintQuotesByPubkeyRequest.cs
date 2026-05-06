using System.Text.Json.Serialization;

namespace DotNut.ApiModels;

public class PostMintQuotesByPubkeyRequest
{
    [JsonPropertyName("pubkeys")]
    public string[] Pubkeys { get; set; }
    
    [JsonPropertyName("pubkey_signatures")]
    public string[] PubKey_Signatures { get; set; }
}