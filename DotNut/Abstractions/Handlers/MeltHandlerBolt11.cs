using System.Text.Json;
using DotNut.Abstractions.Interfaces;
using DotNut.Abstractions.Websockets;
using DotNut.ApiModels;

namespace DotNut.Abstractions.Handlers;

public class MeltHandlerBolt11 : IMeltHandler<PostMeltQuoteBolt11Response, List<Proof>>
{
    private IWalletBuilder _wallet;
    private PostMeltQuoteBolt11Response _quote;
    private OutputData _blankOutputs;
    private bool _withSignatureVerification;
    private List<PrivKey>? _privKeys;
    private string? _htlcPreimage;

    public MeltHandlerBolt11(
        IWalletBuilder wallet,
        PostMeltQuoteBolt11Response quote,
        List<PrivKey>? _privKeys = null,
        string? htlcPreimage = null)
    {
        _wallet = wallet;
        _quote = quote;
        _privKeys = _privKeys;
        _htlcPreimage = htlcPreimage;
    }
    public MeltHandlerBolt11(
        IWalletBuilder wallet,
        PostMeltQuoteBolt11Response quote,
        OutputData blankOutputs,
        List<PrivKey>? privKeys = null,
        string? htlcPreimage = null)
    {
        _wallet = wallet;
        _quote = quote;
        _blankOutputs = blankOutputs;
        _privKeys = privKeys;
        _htlcPreimage = htlcPreimage;
    }
    
    public async Task<PostMeltQuoteBolt11Response> GetQuote(CancellationToken ct = default) => this._quote;
    public async Task<List<Proof>> Melt(List<Proof> inputs, CancellationToken ct = default)
    {
        MaybeProcessP2PkHTLC(inputs);
        var client = await _wallet.GetMintApi();
        var req = new PostMeltRequest
        {
            Quote = _quote.Quote,
            Inputs = inputs.ToArray(),
            Outputs = _blankOutputs.BlindedMessages.ToArray(),
        };
        
       var res = await  client.Melt<PostMeltQuoteBolt11Response, PostMeltRequest>("bolt11", req, ct);
       if (res.Change == null)
       {
           return [];
       }

       var keyset = await _wallet.GetKeys(res.Change.First().Id, false, ct);
       return Utils.ConstructProofsFromPromises(res.Change.ToList(), _blankOutputs, keyset.Keys);
    }
    
    private void MaybeProcessP2PkHTLC(List<Proof> proofs)
    {
        if (_privKeys == null || _privKeys.Count == 0)
        {
            return;
        }
        
        if (proofs == null)
        {
            throw new ArgumentNullException(nameof(proofs), "No proofs to melt!");
        }
        
        var sigAllHandler = new SigAllHandler
        {
            Proofs = proofs,
            BlindedMessages = this._blankOutputs?.BlindedMessages ?? [],
            MeltQuoteId = _quote.Quote,
            HTLCPreimage = this._htlcPreimage,
        };

        if (sigAllHandler.TrySign(out P2PKWitness? witness))
        {
            if (witness == null)
            {
                throw new ArgumentNullException(nameof(witness), "sig_all input was correct, but couldn't create a witness signature!");
            }
            proofs[0].Witness = JsonSerializer.Serialize(witness);
            return;
        }

        foreach (var proof in proofs)
        {
            
            if (proof.Secret is not Nut10Secret { ProofSecret: P2PKProofSecret p2pk, Key: { } key }) continue;
            if (proof.Secret is Nut10Secret { ProofSecret: HTLCProofSecret htlc } && _htlcPreimage is {} preimage)
            {
                var w = htlc.GenerateWitness(proof, _privKeys.Select(p=>p.Key).ToArray(), preimage);
                proof.Witness = JsonSerializer.Serialize(w);
                continue;
            }
            var proofWitness = p2pk.GenerateWitness(proof, _privKeys.Select(p => p.Key).ToArray());
            proof.Witness = JsonSerializer.Serialize(proofWitness);
        }
    }
}