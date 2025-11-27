using DotNut.ApiModels;

namespace DotNut.Abstractions.Handlers;

public class MeltHandlerBolt11(
    IWalletBuilder wallet,
    PostMeltQuoteBolt11Response quote,
    List<OutputData> blankOutputs,
    List<PrivKey>? privKeys = null,
    string? htlcPreimage = null)
    : IMeltHandler<PostMeltQuoteBolt11Response, List<Proof>>
{
    public async Task<PostMeltQuoteBolt11Response> GetQuote(CancellationToken ct = default) => quote;
    public async Task<List<Proof>> Melt(List<Proof> inputs, CancellationToken ct = default)
    {
        Nut10Helper.MaybeProcessNut10(privKeys??[], inputs, blankOutputs, htlcPreimage, quote.Quote);
        var client = await wallet.GetMintApi(ct);
        var req = new PostMeltRequest
        {
            Quote = quote.Quote,
            Inputs = inputs.ToArray(),
            Outputs = blankOutputs.Select(bo=> bo.BlindedMessage).ToArray(),
        };
        
       var res = await  client.Melt<PostMeltQuoteBolt11Response, PostMeltRequest>("bolt11", req, ct);
       if (res.Change == null)
       {
           return [];
       }

       var keyset = await wallet.GetKeys(res.Change.First().Id, false, ct);
       return Utils.ConstructProofsFromPromises(res.Change.ToList(), blankOutputs, keyset.Keys);
    }
}