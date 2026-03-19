using DotNut.ApiModels.Mint.bolt12;

namespace DotNut.ApiModels;

public class PostMintQuotesByPubkeyResponse
{
    public PostMintQuoteBolt11Response[] Bolt11Quotes { get; set; }
    public PostMintQuoteBolt12Response[] Bolt12Quotes { get; set; }
}