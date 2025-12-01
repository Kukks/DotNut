using DotNut.ApiModels;
using DotNut.ApiModels.Melt.bolt12;

namespace DotNut.Abstractions.Handlers;

public class MeltHandlerBolt12(
    IWalletBuilder wallet,
    PostMeltQuoteBolt12Response quote,
    List<OutputData> blankOutputs,
    List<PrivKey>? privKeys = null,
    string? htlcPreimage = null)
    : IMeltHandler<PostMeltQuoteBolt12Response, List<Proof>>
{
    public PostMeltQuoteBolt12Response GetQuote() => quote;
    public async Task<List<Proof>> Melt(List<Proof> inputs, CancellationToken ct = default)
    {
        Nut10Helper.MaybeProcessNut10(privKeys??[], inputs, blankOutputs, htlcPreimage, quote.Quote);
        var client = await wallet.GetMintApi(ct);
        var req = new PostMeltRequest
        {
            Quote = quote.Quote,
            Inputs = inputs.ToArray(),
            Outputs = blankOutputs.Select(bo=>bo.BlindedMessage).ToArray(),
        };
        
        var res = await  client.Melt<PostMeltQuoteBolt12Response, PostMeltRequest>("bolt12", req, ct);
        if (res.Change == null || res.Change.Length == 0)
        {
            return [];
        }

        var keyset = await wallet.GetKeys(res.Change.First().Id, true, false, ct);
        return Utils.ConstructProofsFromPromises(res.Change.ToList(), blankOutputs, keyset.Keys);
    }
}