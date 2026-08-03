using DotNut.ApiModels;
using DotNut.ApiModels.Onchain;

namespace DotNut.Abstractions.Handlers;

public class MintHandlerOnchain(IWalletBuilder wallet,
    PostMintQuoteOnchainResponse quote,
    GetKeysResponse.KeysetItemResponse keyset,
    List<OutputData> outputs
    ): IMintHandler<PostMintQuoteOnchainResponse, List<Proof>>
{
    private string? _signature;
    private List<OutputData> _outputs = outputs;
    
    public IMintHandler<PostMintQuoteOnchainResponse, List<Proof>> WithSignature(string signature)
    {
        this._signature = signature;
        return this;
    }

    public IMintHandler<PostMintQuoteOnchainResponse, List<Proof>> SignWithPrivkey(PrivKey privkey)
    {
        this._signature = privkey.SignMintQuote(
            quote.Quote,
            _outputs.Select(o => o.BlindedMessage).ToList()
        );
        return this;
    }

    public IMintHandler<PostMintQuoteOnchainResponse, List<Proof>> SignWithPrivkey(string privKeyHex)
    {
        return this.SignWithPrivkey(new PrivKey(privKeyHex));
    }

    public IMintHandler<PostMintQuoteOnchainResponse, List<Proof>> WithOutputs(IEnumerable<OutputData> newOutputs)
    {
        _outputs = newOutputs as List<OutputData> ?? newOutputs.ToList();
        return this;
    }

    public PostMintQuoteOnchainResponse GetQuote() => quote;

    // onchain takes quite some time
    public async Task<List<Proof>> Mint(CancellationToken ct = default)
    {
        if (_outputs.Count == 0)
            throw new ArgumentException("Outputs are empty. Call WithOutputs() with the mintable amount before minting.");

        if (this._signature is null)
        {
            throw new ArgumentNullException(
                nameof(this._signature),
                $"Signature for mint quote {quote.Quote} is required!"
            );
        }
        
        var client = await wallet.GetMintApi(ct);
        var req = new PostMintRequest
        {
            Outputs = _outputs.Select(o => o.BlindedMessage).ToArray(),
            Quote = quote.Quote,
            Signature = _signature,
        };
        var promises = await client.Mint<PostMintRequest, PostMintResponse>("onchain", req, ct);
        
        return Utils.ConstructProofsFromPromises(
            promises.Signatures.ToList(),
            _outputs,
            keyset.Keys
        );

    }

    public List<OutputData> GetOutputs() => _outputs;
}