using DotNut.Abstractions.Websockets;
using DotNut.Api;
using DotNut.ApiModels;
using DotNut.ApiModels.Melt.bolt12;
using DotNut.ApiModels.Mint.bolt12;

namespace DotNut.Abstractions;

public class MintHandlerBolt12: IMintHandler<PostMintQuoteBolt12Response, List<Proof>>
{
    
    private readonly IWalletBuilder _wallet;
    private readonly PostMintQuoteBolt12Response _quote;
    private readonly GetKeysResponse.KeysetItemResponse _keyset;
    private readonly OutputData _outputs;
    
    private string? SubscriptionId;
    private WebsocketService? _websocketService;

    public MintHandlerBolt12(Wallet wallet, PostMintQuoteBolt12Response quote, GetKeysResponse.KeysetItemResponse keyset, OutputData outputs)
    {
        this._wallet = wallet;
        this._quote = quote;
        this._keyset = keyset;
        this._outputs = outputs;
    }

    public async Task<PostMintQuoteBolt12Response> GetQuote(CancellationToken ct = default) => this._quote;
    public async Task<List<Proof>> Mint(CancellationToken ct = default)
    {
        var client = await this._wallet.GetMintApi();
        
        var req = new PostMintRequest
        {
            Outputs = this._outputs.BlindedMessages.ToArray(),
            Quote = _quote.Quote
        };
        
        var promises=  await client.Mint<PostMintRequest, PostMintResponse>("bolt12", req, ct);
        return CashuUtils.ConstructProofsFromPromises(promises.Signatures.ToList(), _outputs, _keyset.Keys);
    }
    
    private async Task<PostMintResponse> _processMint(PostMintRequest req, CancellationToken cts = default)
    {
        var client = await this._wallet.GetMintApi();

        return await client.Mint<PostMintRequest, PostMintResponse>("bolt12", req, cts);
    }
    
}