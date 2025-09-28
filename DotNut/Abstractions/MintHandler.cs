using System.Runtime.CompilerServices;
using DotNut.Abstractions.Websockets;
using DotNut.Api;
using DotNut.ApiModels;
using DotNut.ApiModels.Mint.bolt12;

namespace DotNut.Abstractions.Quotes;

// todo 
// at this point we should already have everything that we need for minting the tokens. also, we assume the invoice is paid or it will be paid soon 

public class MintHandlerBolt11: IMintHandler<PostMintResponse>
{
    private readonly ICashuWalletBuilder _wallet;
    private readonly PostMintQuoteBolt11Response _quote;
    private readonly GetKeysResponse.KeysetItemResponse _keyset;
    private KeysetId _keysetId => _keyset.Id; // at this point keysetid MUST be validated so it's safe to asssume its correct
    
    private List<ulong>? _amounts;
    private OutputData? _outputs;
    
    private string SubscriptionId;
    private WebsocketService _websocketService;

    public MintHandlerBolt11(
        ICashuWalletBuilder wallet,
        PostMintQuoteBolt11Response postMintQuoteBolt11Response, 
        GetKeysResponse.KeysetItemResponse? verifiedKeyset
        )
    {
        this._quote = postMintQuoteBolt11Response;
        this._keyset = verifiedKeyset;
    }
    
    public MintHandlerBolt11(PostMintQuoteBolt11Response postMintQuoteBolt11Response, GetKeysResponse.KeysetItemResponse? verifiedKeyset, List<ulong>? amounts)
    {
        this._quote = postMintQuoteBolt11Response;
        this._keyset = verifiedKeyset;
        this._amounts = amounts;
    }

    public async Task<PostMintResponse> Mint(CancellationToken cts = default)
    {
        
        if (_quote.Amount == null)
        {
            //todo amountless flow 
            return new PostMintResponse();
        }

        if (_amounts == null)
        {
            var amounts = CashuUtils.SplitToProofsAmounts(_quote.Amount.Value, _keyset.Keys);
        }

        if (this._outputs == null)
        {
            this._outputs = await _wallet.CreateOutputs(_amounts!, _keysetId, cts);
        }

        var req = new PostMintRequest
        {
            Outputs = this._outputs.BlindedMessages,
            Quote = _quote.Quote
        };
        return await this._processMint(req, cts);
    }
    
    private async Task<PostMintResponse> _processMint(PostMintRequest req, CancellationToken cts = default)
    {
        var client = this._wallet.GetMintApi();
        if (client is null)
        {
            throw new ArgumentNullException(nameof(CashuHttpClient), "Mint api can't be null!");
        }

        return await client.Mint<PostMintRequest, PostMintResponse>("bolt11", req, cts);
    }
    
    public Task<Subscription> Subscribe()
    {
        throw new NotImplementedException();
        // await this._websocketService.SubscribeToSingleMeltQuoteAsync();
    }
}