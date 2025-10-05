using DotNut.Abstractions.Interfaces;
using DotNut.Abstractions.Quotes;
using DotNut.Api;
using DotNut.ApiModels;
using DotNut.ApiModels.Mint.bolt12;

namespace DotNut.Abstractions;

class MintQuoteBuilder : IMintQuoteBuilder
{
    private readonly Wallet _wallet;
    private ulong? _amount;
    private string _unit = "sat";
    private string? _description;
    private OutputData? _outputs;
    private string? _method = "bolt11";

    //for bolt12
    private string? _pubkey;

    private KeysetId? _keysetId;
    private GetKeysResponse.KeysetItemResponse? _keyset;

    public MintQuoteBuilder(Wallet wallet)
    {
        this._wallet = wallet;
    }

    /// <summary>
    /// Mandatory.
    /// User has to provide Mint method
    /// </summary>
    /// <param name="method">Either MintMeltMethod.Bolt11 or MintMeltMethod.Bolt12</param>
    /// <returns></returns>
    public IMintQuoteBuilder WithMethod(string method)
    {
        this._method = method;
        return this;
    }

    /// <summary>
    /// Mandatory.
    /// </summary>
    /// <param name="amount">Amount of token in currently choosen unit to be melted</param>
    public IMintQuoteBuilder WithAmount(ulong amount)
    {
        this._amount = amount;
        return this;
    }

    /// <summary>
    /// Optional.
    /// Sets unit of tokens being minted. Sat by default.
    /// </summary>
    /// <param name="unit">Unit of minted proofs</param>
    public IMintQuoteBuilder WithUnit(string unit)
    {
        this._unit = unit;
        return this;
    }

    /// <summary>
    /// Optional. Necessary for bolt12
    /// Sets pubkey for bolt12 offer 
    /// </summary>
    /// <param name="pubkey"></param>
    /// <returns></returns>
    public IMintQuoteBuilder WithPubkey(string pubkey)
    {
        this._pubkey = pubkey;
        return this;
    }

    /// <summary>
    /// Optional.
    /// Allows user to set keysetId manually. Otherwise, builder will choose active one manually, with the lowest fees.
    /// </summary>
    /// <param name="keysetId"></param>
    public IMintQuoteBuilder WithKeyset(KeysetId keysetId)
    {
        this._keysetId = keysetId;
        return this;
    }


    /// <summary>
    /// Optional.
    /// User may provide outputs for mint to sign. Blinding factors and secrets won't be revealed to mint.
    /// If not provided, wallet will try to derive them from seed and counter, or create random ones if mnemonic is not avaible.
    /// </summary>
    /// <param name="outputs">OutputData instance. Enumerables of BlindingFactors, BlindedMessages and Secrets, in right order.</param>
    public IMintQuoteBuilder WithOutputs(OutputData outputs)
    {
        this._outputs = outputs;
        return this;
    }

    /// <summary>
    /// Optional.
    /// User may provide description for melt quote invoice. 
    /// </summary>
    /// <param name="description"></param>
    /// <returns></returns>
    public IMintQuoteBuilder WithDescription(string description)
    {
        this._description = description;
        return this;
    }

    public IMintQuoteBuilder WithP2PK()
    {
        throw new NotImplementedException();
    }

    public IMintQuoteBuilder WithHTLC()
    {
        throw new NotImplementedException();
    }

    public async Task<IMintHandler<PostMintQuoteBolt11Response, List<Proof>>> ProcessAsyncBolt11(
        CancellationToken cts = default)
    {
        //todo implement info 

        await this._wallet._maybeSyncKeys(cts);
        if (_amount == null)
        {
            throw new ArgumentNullException(nameof(_amount), "can't create melt quote without amount!");
        }

        var api = this._wallet.GetMintApi();
        if (api is null)
        {
            throw new ArgumentNullException(nameof(ICashuApi), "Can't request mint quote without mint API");
        }

        if (this._keysetId == null)
        {
            this._keysetId = await this._wallet.GetActiveKeysetId(this._unit, cts) ??
                             throw new ArgumentException($"Can't get active keyset ID for unit: {_unit}");
        }

        if (this._keyset == null)
        {
            this._keyset = await this._wallet.GetKeys(this._keysetId, false, cts) ??
                           throw new ArgumentException($"Cant get keys for keysetId: {_keysetId}");
        }

        var reqBolt11 = new PostMintQuoteBolt11Request()
        {
            Amount = this._amount.Value,
            Unit = this._unit,
            Description = this._description,
        };
        var quoteBolt11 =
            await (await this._wallet.GetMintApi())
                .CreateMintQuote<PostMintQuoteBolt11Response, PostMintQuoteBolt11Request>("bolt11", reqBolt11,
                    cts);
        return new MintHandlerBolt11(this._wallet, quoteBolt11, this._keyset);
    }

    public async Task<IMintHandler<PostMintQuoteBolt12Response, List<Proof>>> ProcessAsyncBolt12(
        CancellationToken cts = default)
    {
        await this._wallet._maybeSyncKeys(cts);
            if (this._pubkey == null)
            {
                throw new ArgumentNullException(nameof(_pubkey), "Can't request bolt12 mint quote without pubkey!");
            }
            
            if (this._keyset == null)
            {
                this._keyset = await this._wallet.GetKeys(this._keysetId, false, cts) ??
                               throw new ArgumentException($"Cant fetch keys for keysetId: {_keysetId}");
            }

            var req = new PostMintQuoteBolt12Request()
            {
                Amount = this._amount.Value,
                Unit = this._unit,
                Pubkey = this._pubkey,
                Description = this._description,
            };
            var mintQuote =
                await (await _wallet.GetMintApi())
                    .CreateMintQuote<PostMintQuoteBolt12Response, PostMintQuoteBolt12Request>("bolt12", req,
                        cts);
            return new MintHandlerBolt12(this._wallet, mintQuote, this._keyset);

    }
}