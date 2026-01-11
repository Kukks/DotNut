using DotNut.ApiModels;

namespace DotNut.Abstractions.Handlers;

public class MeltHandlerBolt11(
    IWalletBuilder wallet,
    PostMeltQuoteBolt11Response quote,
    List<OutputData> blankOutputs,
    List<PrivKey>? privKeys = null,
    string? htlcPreimage = null
) : IMeltHandler<PostMeltQuoteBolt11Response, List<Proof>>
{
    public PostMeltQuoteBolt11Response GetQuote() => quote;

    public async Task<List<Proof>> Melt(List<Proof> inputs, CancellationToken ct = default)
    {
        //we're operating on copy here since later the proof state is mutated in stripFingerprints
        var proofs = inputs.DeepCopyList();

        Nut10Helper.MaybeProcessNut10(
            privKeys ?? [],
            proofs,
            blankOutputs,
            htlcPreimage,
            quote.Quote
        );
        //since nut10 (with p2bk) is processed, now it's safe to strip P2PkE
        proofs.ForEach(i => i.StripFingerprints());

        var client = await wallet.GetMintApi(ct);
        var req = new PostMeltRequest
        {
            Quote = quote.Quote,
            Inputs = proofs.ToArray(),
            Outputs = blankOutputs.Select(bo => bo.BlindedMessage).ToArray(),
        };

        var res = await client.Melt<PostMeltQuoteBolt11Response, PostMeltRequest>(
            "bolt11",
            req,
            ct
        );
        if (res.Change == null || res.Change.Length == 0)
        {
            return [];
        }

        var keyset = await wallet.GetKeys(res.Change.First().Id, true, false, ct);
        return Utils.ConstructProofsFromPromises(res.Change.ToList(), blankOutputs, keyset.Keys);
    }
}
