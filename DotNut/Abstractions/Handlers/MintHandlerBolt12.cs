using DotNut.ApiModels;
using DotNut.ApiModels.Mint.bolt12;

namespace DotNut.Abstractions.Handlers;

public class MintHandlerBolt12(
    IWalletBuilder wallet,
    PostMintQuoteBolt12Response quote,
    GetKeysResponse.KeysetItemResponse keyset,
    List<OutputData> outputs
) : IMintHandler<PostMintQuoteBolt12Response, List<Proof>>
{
    private PostMintQuoteBolt12Response _quote = quote;
    private string? _signature;

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
        this._signature = privkey.SignMintQuote(
            _quote.Quote,
            outputs.Select(o => o.BlindedMessage).ToList()
        );
        return this;
    }

    public PostMintQuoteBolt12Response GetQuote() => _quote;

    public List<OutputData> GetOutputs() => outputs;

    public async Task<PostMintQuoteBolt12Response> RefreshQuote(CancellationToken ct = default)
    {
        var client = await wallet.GetMintApi(ct);
        var fresh = await client.CheckMintQuote<PostMintQuoteBolt12Response>(
            "bolt12",
            _quote.Quote,
            ct
        );

        if (_quote.UpdatedAt is { } held && fresh.UpdatedAt is { } incoming && incoming < held)
        {
            return _quote;
        }

        _quote = fresh;
        return _quote;
    }

    public async Task<List<Proof>> Mint(CancellationToken ct = default)
    {
        if (this._signature is null)
        {
            throw new ArgumentNullException(
                nameof(this._signature),
                $"Signature for mint quote {_quote.Quote} is required!"
            );
        }

        var client = await wallet.GetMintApi(ct);
        var req = new PostMintRequest
        {
            Outputs = outputs.Select(o => o.BlindedMessage).ToArray(),
            Quote = _quote.Quote,
            Signature = _signature,
        };

        var promises = await client.Mint<PostMintRequest, PostMintResponse>("bolt12", req, ct);
        return Utils.ConstructProofsFromPromises(
            promises.Signatures.ToList(),
            outputs,
            keyset.Keys
        );
    }
}
