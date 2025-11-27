using System.Xml;
using DotNut.Abstractions.Websockets;
using DotNut.Api;
using DotNut.ApiModels;
using DotNut.ApiModels.Melt.bolt12;
using DotNut.ApiModels.Mint.bolt12;
using DotNut;
namespace DotNut.Abstractions;

public class MintHandlerBolt12: IMintHandler<PostMintQuoteBolt12Response, List<Proof>>
{
    
    private readonly IWalletBuilder _wallet;
    private readonly PostMintQuoteBolt12Response _quote;
    private readonly GetKeysResponse.KeysetItemResponse _keyset;
    private readonly List<OutputData> _outputs;
    private string? _signature;
    private string? SubscriptionId;
    private WebsocketService? _websocketService;

    public MintHandlerBolt12(Wallet wallet, PostMintQuoteBolt12Response quote, GetKeysResponse.KeysetItemResponse keyset, List<OutputData> outputs)
    {
        this._wallet = wallet;
        this._quote = quote;
        this._keyset = keyset;
        this._outputs = outputs;
    }

    public IMintHandler<PostMintQuoteBolt12Response, List<Proof>> WithSignature(string signature)
    {
        _signature = signature;
        return this;
    }

    public IMintHandler<PostMintQuoteBolt12Response, List<Proof>> SignWithPrivkey(string privKeyHex)
    {
        return this.SignWithPrivkey(new PrivKey(privKeyHex));
    }

    public IMintHandler<PostMintQuoteBolt12Response, List<Proof>> SignWithPrivkey(PrivKey privkey)
    {
        this._signature = privkey.SignMintQuote(_quote.Quote, this._outputs.Select(o=>o.BlindedMessage).ToList());
        return this;
    }
    
    public async Task<PostMintQuoteBolt12Response> GetQuote(CancellationToken ct = default) => this._quote;
    
    public async Task<List<Proof>> Mint(CancellationToken ct = default)
    {
        if (this._signature is null)
        {
            throw new ArgumentNullException(nameof(this._signature), $"Signature for mint quote {this._quote.Quote} is required!");
        }
        
        var client = await this._wallet.GetMintApi();
        var req = new PostMintRequest
        {
            Outputs = this._outputs.Select(o=>o.BlindedMessage).ToArray(),
            Quote = _quote.Quote,
            Signature = _signature,
        };
        
        var promises=  await client.Mint<PostMintRequest, PostMintResponse>("bolt12", req, ct);
        return Utils.ConstructProofsFromPromises(promises.Signatures.ToList(), _outputs, _keyset.Keys);
    }
}