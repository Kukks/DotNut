using System.Text.Json;
using DotNut.Abstractions.Interfaces;
using DotNut.Abstractions.Websockets;
using DotNut.ApiModels;
using DotNut.ApiModels.Melt.bolt12;

namespace DotNut.Abstractions.Handlers;

public class MeltHandlerBolt12: IMeltHandler<PostMeltQuoteBolt12Response, List<Proof>>
{
    private IWalletBuilder _wallet;
    private PostMeltQuoteBolt12Response _quote;
    private List<OutputData> _blankOutputs;
    private bool _withSignatureVerification;
    private List<PrivKey>? _privKeys;
    private string? _htlcPreimage;

    public MeltHandlerBolt12(        
        IWalletBuilder wallet,
        PostMeltQuoteBolt12Response quote,
        List<OutputData> blankOutputs,
        List<PrivKey>? privKeys = null,
        string? htlcPreimage = null)
    {
        
    }
    public async Task<PostMeltQuoteBolt12Response> GetQuote(CancellationToken ct = default) => this._quote;
    public async Task<List<Proof>> Melt(List<Proof> inputs, CancellationToken ct = default)
    {
        Nut10Helper.MaybeProcessNut10(_privKeys??[], inputs, _blankOutputs, _htlcPreimage, _quote.Quote);
        var client = await _wallet.GetMintApi();
        var req = new PostMeltRequest
        {
            Quote = _quote.Quote,
            Inputs = inputs.ToArray(),
            Outputs = _blankOutputs.Select(bo=>bo.BlindedMessage).ToArray(),
        };
        
        var res = await  client.Melt<PostMeltQuoteBolt12Response, PostMeltRequest>("bolt11", req, ct);
        if (res.Change == null)
        {
            return [];
        }

        var keyset = await _wallet.GetKeys(res.Change.First().Id, false, ct);
        return Utils.ConstructProofsFromPromises(res.Change.ToList(), _blankOutputs, keyset.Keys);
    }
}