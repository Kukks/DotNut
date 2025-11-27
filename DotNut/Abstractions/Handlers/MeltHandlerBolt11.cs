using System.Text.Json;
using DotNut.Abstractions.Interfaces;
using DotNut.Abstractions.Websockets;
using DotNut.ApiModels;

namespace DotNut.Abstractions.Handlers;

public class MeltHandlerBolt11 : IMeltHandler<PostMeltQuoteBolt11Response, List<Proof>>
{
    private IWalletBuilder _wallet;
    private PostMeltQuoteBolt11Response _quote;
    private List<OutputData> _blankOutputs;
    private bool _withSignatureVerification;
    private List<PrivKey>? _privKeys;
    private string? _htlcPreimage;

    public MeltHandlerBolt11(
        IWalletBuilder wallet,
        PostMeltQuoteBolt11Response quote,
        List<PrivKey>? privKeys = null,
        string? htlcPreimage = null)
    {
        _wallet = wallet;
        _quote = quote;
        _privKeys = privKeys;
        _htlcPreimage = htlcPreimage;
    }
    public MeltHandlerBolt11(
        IWalletBuilder wallet,
        PostMeltQuoteBolt11Response quote,
        List<OutputData> blankOutputs,
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
        Nut10Helper.MaybeProcessNut10(_privKeys??[], inputs, _blankOutputs, _htlcPreimage, _quote.Quote);
        var client = await _wallet.GetMintApi(ct);
        var req = new PostMeltRequest
        {
            Quote = _quote.Quote,
            Inputs = inputs.ToArray(),
            Outputs = _blankOutputs.Select(bo=> bo.BlindedMessage).ToArray(),
        };
        
       var res = await  client.Melt<PostMeltQuoteBolt11Response, PostMeltRequest>("bolt11", req, ct);
       if (res.Change == null)
       {
           return [];
       }

       var keyset = await _wallet.GetKeys(res.Change.First().Id, false, ct);
       return Utils.ConstructProofsFromPromises(res.Change.ToList(), _blankOutputs, keyset.Keys);
    }
}