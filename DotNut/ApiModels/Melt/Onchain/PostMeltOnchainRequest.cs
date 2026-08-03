using System.Text.Json.Serialization;

namespace DotNut.ApiModels.Melt.Onchain;

public class PostMeltOnchainRequest : PostMeltRequest
{
    [JsonPropertyName("fee_index")]
    public ulong FeeIndex { get; set; }
}