using DotNut.Abstractions.Websockets;
using DotNut.Api;
using DotNut.ApiModels;
using DotNut.ApiModels.Melt.bolt12;
using DotNut.ApiModels.Mint.bolt12;

namespace DotNut.Abstractions;

public class MintHandlerBolt12: IMintHandler<PostMintQuoteBolt12Response, List<Proof>>
{
    
    private readonly IWalletBuilder _wallet;
    private PostMintQuoteBolt12Response _quote;
    private GetKeysResponse.KeysetItemResponse _keyset;

    private OutputData? _outputs;
    private ulong? _amount;
    private List<ulong>? _amounts;

    public MintHandlerBolt12(Wallet wallet, PostMintQuoteBolt12Response quote, GetKeysResponse.KeysetItemResponse keyset)
    {
        this._wallet = wallet;
        this._quote = quote;
        this._keyset = keyset;
    }

    public async Task<PostMintQuoteBolt12Response> GetQuote(CancellationToken cts = default) => this._quote;
    public async Task<List<Proof>> Mint(CancellationToken cts = default)
    {
        var client = await this._wallet.GetMintApi();

        _amount ??= _quote.Amount ?? throw new ArgumentNullException(nameof(_quote.Amount), "Can't determine amount of quote!");
        
        _amounts??= CashuUtils.SplitToProofsAmounts(_amount.Value, _keyset.Keys);
        
        this._outputs ??= await _wallet.CreateOutputs(_amounts!, _keyset.Id, cts);

        var req = new PostMintRequest
        {
            Outputs = this._outputs.BlindedMessages,
            Quote = _quote.Quote
        };
        
        var promises=  await client.Mint<PostMintRequest, PostMintResponse>("bolt11", req, cts);
        return CashuUtils.ConstructProofsFromPromises(promises.Signatures.ToList(), _outputs, _keyset.Keys);
    }
    
    private async Task<PostMintResponse> _processMint(PostMintRequest req, CancellationToken cts = default)
    {
        var client = await this._wallet.GetMintApi();

        return await client.Mint<PostMintRequest, PostMintResponse>("bolt12", req, cts);
    }
    
    public Task<Subscription> Subscribe(CancellationToken cts = default)
    {
        throw new NotImplementedException();
    }

}