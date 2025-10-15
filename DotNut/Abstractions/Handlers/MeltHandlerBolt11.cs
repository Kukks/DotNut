using DotNut.Abstractions.Interfaces;
using DotNut.Abstractions.Websockets;
using DotNut.ApiModels;

namespace DotNut.Abstractions.Handlers;

public class MeltHandlerBolt11 : IMeltHandler<PostMeltQuoteBolt11Response, List<Proof>>
{
    private IWalletBuilder _wallet;
    private PostMeltQuoteBolt11Response _quote;
    private OutputData _blankOutputs;

    public MeltHandlerBolt11(IWalletBuilder wallet, PostMeltQuoteBolt11Response quote)
    {
        _wallet = wallet;
        _quote = quote;
    }
    public MeltHandlerBolt11(IWalletBuilder wallet, PostMeltQuoteBolt11Response quote, OutputData blankOutputs)
    {
        _wallet = wallet;
        _quote = quote;
        this._blankOutputs = blankOutputs;
    }
    public async Task<PostMeltQuoteBolt11Response> GetQuote(CancellationToken cts = default) => this._quote;
    public async Task<List<Proof>> Melt(List<Proof> inputs, CancellationToken cts = default)
    {
        var client = await _wallet.GetMintApi();
        var req = new PostMeltRequest
        {
            Quote = _quote.Quote,
            Inputs = inputs.ToArray(),
            Outputs = _blankOutputs.BlindedMessages.ToArray(),
        };
        
       var res = await  client.Melt<PostMeltQuoteBolt11Response, PostMeltRequest>("bolt11", req, cts);
       if (res.Change == null)
       {
           return [];
       }

       var keyset = await _wallet.GetKeys(res.Change.First().Id, false, cts);
       return CashuUtils.ConstructProofsFromPromises(res.Change.ToList(), _blankOutputs, keyset.Keys);
    }
    
    public Task<Subscription> Subscribe(CancellationToken cts = default)
    {
        throw new NotImplementedException();
    }
}