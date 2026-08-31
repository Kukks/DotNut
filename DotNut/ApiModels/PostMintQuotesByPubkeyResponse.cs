using System.Text.Json.Serialization;
using DotNut.ApiModels.Mint.bolt12;

namespace DotNut.ApiModels;

[JsonConverter(typeof(PostMintQuotesByPubkeyResponseConverter))]
public class PostMintQuotesByPubkeyResponse
{
    public PostMintQuoteBolt11Response[] Bolt11Quotes { get; set; }
    public PostMintQuoteBolt12Response[] Bolt12Quotes { get; set; }
}