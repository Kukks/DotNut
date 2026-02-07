using DotNut.ApiModels;

namespace DotNut.Abstractions.Handlers;

public class MintHandlerBolt11(
    IWalletBuilder wallet,
    PostMintQuoteBolt11Response postMintQuoteBolt11Response,
    GetKeysResponse.KeysetItemResponse keyset,
    List<OutputData> outputs
) : IMintHandler<PostMintQuoteBolt11Response, List<Proof>>
{
    private string? _signature;

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
        this._signature = privkey.SignMintQuote(
            postMintQuoteBolt11Response.Quote,
            outputs.Select(o => o.BlindedMessage).ToList()
        );
        return this;
    }

    public PostMintQuoteBolt11Response GetQuote() => postMintQuoteBolt11Response;
    public List<OutputData> GetOutputs() => outputs;

    public async Task<List<Proof>> Mint(CancellationToken ct = default)
    {
        if (postMintQuoteBolt11Response.PubKey is not null && this._signature is null)
        {
            throw new ArgumentNullException(
                nameof(_signature),
                $"Signature for mint quote {postMintQuoteBolt11Response.Quote} is required!"
            );
        }
        var client = await wallet.GetMintApi(ct);

        var req = new PostMintRequest
        {
            Outputs = outputs.Select(o => o.BlindedMessage).ToArray(),
            Quote = postMintQuoteBolt11Response.Quote,
            Signature = _signature,
        };

        var promises = await client.Mint<PostMintRequest, PostMintResponse>("bolt11", req, ct);
        return Utils.ConstructProofsFromPromises(
            promises.Signatures.ToList(),
            outputs,
            keyset.Keys
        );
    }
}
