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
    private readonly OutputData _outputs;

    private string? _signature;
    private WebsocketService? _websocketService;

    public MintHandlerBolt11(
        IWalletBuilder wallet,
        PostMintQuoteBolt11Response postMintQuoteBolt11Response, 
        GetKeysResponse.KeysetItemResponse? verifiedKeyset,
        OutputData outputs
        )
    {
        this._wallet = wallet;
        this._quote = postMintQuoteBolt11Response;
        this._keyset = verifiedKeyset;
        this._outputs = outputs;
    }
    
    public IMintHandler<PostMintQuoteBolt11Response, List<Proof>> WithSignature(string signature)
    {
        _signature = signature;
        return this;
    }

    public IMintHandler<PostMintQuoteBolt11Response, List<Proof>> SignWithPrivkey(string privKeyHex)
    {
        return this.SignWithPrivkey(new PrivKey(privKeyHex));
    }

    public IMintHandler<PostMintQuoteBolt11Response, List<Proof>> SignWithPrivkey(PrivKey privkey)
    {
        this._signature = privkey.SignMintQuote(_quote.Quote, this._outputs.BlindedMessages);
        return this;
    }
    
    public async Task<PostMintQuoteBolt11Response> GetQuote(CancellationToken ct = default) => _quote;

    public async Task<List<Proof>> Mint(CancellationToken ct = default)
    {
        if (this._quote.PubKey is not null && this._signature is null)
        {
            throw new ArgumentNullException(nameof(_signature),$"Signature for mint quote {this._quote.Quote} is required!" );
        }
        var client = await this._wallet.GetMintApi();

        var req = new PostMintRequest
        {
            Outputs = this._outputs.BlindedMessages.ToArray(),
            Quote = _quote.Quote,
        };
        
        var promises=  await client.Mint<PostMintRequest, PostMintResponse>("bolt11", req, ct);
        return Utils.ConstructProofsFromPromises(promises.Signatures.ToList(), _outputs, _keyset.Keys);
    }

}