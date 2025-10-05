using System.Runtime.CompilerServices;
using DotNut.Abstractions.Websockets;
using DotNut.Api;
using DotNut.ApiModels;
using DotNut.ApiModels.Mint.bolt12;

namespace DotNut.Abstractions.Quotes;

public class MintHandlerBolt11: IMintHandler<PostMintQuoteBolt11Response, List<Proof>>
{
    private readonly PostMintQuoteBolt11Response _quote;
    private readonly IWalletBuilder _wallet;
    private readonly GetKeysResponse.KeysetItemResponse _keyset;

    private ulong? _amount;
    
    private List<ulong>? _amounts;
    private OutputData? _outputs;
    
    private string? SubscriptionId;
    private WebsocketService? _websocketService;

    public MintHandlerBolt11(
        IWalletBuilder wallet,
        PostMintQuoteBolt11Response postMintQuoteBolt11Response, 
        GetKeysResponse.KeysetItemResponse? verifiedKeyset
        )
    {
        this._wallet = wallet;
        this._quote = postMintQuoteBolt11Response;
        this._keyset = verifiedKeyset;
    }
    
    public MintHandlerBolt11(
        IWalletBuilder wallet,
        PostMintQuoteBolt11Response postMintQuoteBolt11Response, 
        GetKeysResponse.KeysetItemResponse? verifiedKeyset,
        ulong amount
    )
    {
        this._quote = postMintQuoteBolt11Response;
        this._keyset = verifiedKeyset;
        this._amount = amount;
    }
    
    public MintHandlerBolt11(PostMintQuoteBolt11Response postMintQuoteBolt11Response, GetKeysResponse.KeysetItemResponse? verifiedKeyset, List<ulong>? amounts, ulong? amount)
    {
        this._quote = postMintQuoteBolt11Response;
        this._keyset = verifiedKeyset;
        this._amounts = amounts;
        this._amount = amount;
    }
    
    
    public async Task<PostMintQuoteBolt11Response> GetQuote(CancellationToken cts = default) => _quote;

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

    public Task<Subscription> Subscribe(CancellationToken cts = default)
    {
        throw new NotImplementedException();
    }
    
}