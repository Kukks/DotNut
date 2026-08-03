using DotNut.ApiModels.Melt.Onchain;

namespace DotNut.Abstractions.Handlers;

public class MeltHandlerOnchain(
    IWalletBuilder wallet,
    PostMeltQuoteOnchainResponse quote,
    List<OutputData> blankOutputs,
    ulong feeIndex,
    List<PrivKey>? privKeys = null,
    string? htlcPreimage = null): IMeltHandler<PostMeltQuoteOnchainResponse, List<Proof>>
{
    public PostMeltQuoteOnchainResponse GetQuote() => quote;
    
    public async Task<List<Proof>> Melt(IEnumerable<Proof> inputs, CancellationToken ct = default)
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
        proofs.ForEach(i => i.StripFingerprints());

        var client = await wallet.GetMintApi(ct);
        var req = new PostMeltOnchainRequest
        {
            Quote = quote.Quote,
            Inputs = proofs.ToArray(),
            Outputs = blankOutputs.Select(bo => bo.BlindedMessage).ToArray(),
            FeeIndex = feeIndex
        };

        var res = await client.Melt<PostMeltQuoteOnchainResponse, PostMeltOnchainRequest>(
            "onchain",
            req,
            ct
        );
        if (res.Change == null || res.Change.Length == 0)
        {
            return [];
        }

        var keysetIds = res.Change.Select(sig => sig.Id).Distinct().ToList();
        var changeProofs = new List<Proof>();
        foreach (var keysetId in keysetIds)
        {
            var keyset = await wallet.GetKeys(keysetId, true, false, ct);
            if (keyset == null)
            {
                continue;
            }
            changeProofs.AddRange(
                Utils.ConstructProofsFromPromises(res.Change.ToList(), blankOutputs, keyset.Keys)
            );
        }
        return changeProofs;
    }
}