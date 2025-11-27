using DotNut.Abstractions.Handlers;
using DotNut.Api;
using DotNut.ApiModels;
using DotNut.ApiModels.Mint.bolt12;

namespace DotNut.Abstractions;

class MintQuoteBuilder : IMintQuoteBuilder
{
    private readonly Wallet _wallet;
    
    private ulong? _amount;
    private List<ulong>? _amounts;
    private string _unit = "sat";
    private string? _description;
    private List<OutputData>? _outputs;
    private string? _method = "bolt11";

    private string? _pubkey;

    private KeysetId? _keysetId;
    private GetKeysResponse.KeysetItemResponse? _keyset;
    
    //for p2pk
    private P2PkBuilder? _builder;

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

    public IMintQuoteBuilder WithAmount(ulong amount)
    {
        this._amount = amount;
        return this;
    }

    public IMintQuoteBuilder WithUnit(string unit)
    {
        this._unit = unit;
        return this;
    }

    public IMintQuoteBuilder WithPubkey(string pubkey)
    {
        this._pubkey = pubkey;
        return this;
    }

    public IMintQuoteBuilder WithKeyset(KeysetId keysetId)
    {
        this._keysetId = keysetId;
        return this;
    }


    public IMintQuoteBuilder WithOutputs(List<OutputData> outputs)
    {
        this._outputs = outputs;
        return this;
    }

    public IMintQuoteBuilder WithP2PkLock(P2PkBuilder p2pkBuilder)
    {
        this._builder = p2pkBuilder;
        return this;
    }

    public IMintQuoteBuilder WithHTLCLock(HTLCBuilder htlcBuilder)
    {
        this._builder = htlcBuilder;
        return this;
    }

    public IMintQuoteBuilder WithDescription(string description)
    {
        this._description = description;
        return this;
    }
    
    public async Task<IMintHandler<PostMintQuoteBolt11Response, List<Proof>>> ProcessAsyncBolt11(
        CancellationToken ct = default)
    {
        //todo implement info 

        await this._wallet._maybeSyncKeys(ct);
        if (_amount == null)
        {
            throw new ArgumentNullException(nameof(_amount), "can't create melt quote without amount!");
        }

        var api = await this._wallet.GetMintApi();
        if (api is null)
        {
            throw new ArgumentNullException(nameof(ICashuApi), "Can't request mint quote without mint API");
        }

        this._keysetId ??= await this._wallet.GetActiveKeysetId(this._unit, ct) ??
                           throw new ArgumentException($"Can't get active keyset ID for unit: {_unit}");

        this._keyset ??= await this._wallet.GetKeys(this._keysetId, false, ct) ??
                         throw new ArgumentException($"Cant get keys for keysetId: {_keysetId}");

        var outputs = await this._createOutputs();
        

        var reqBolt11 = new PostMintQuoteBolt11Request()
        {
            Amount = this._amount.Value,
            Unit = this._unit,
            Description = this._description,
            Pubkey = this._pubkey??null,
        };
        var quoteBolt11 =
            await api.CreateMintQuote<PostMintQuoteBolt11Response, PostMintQuoteBolt11Request>("bolt11", reqBolt11,
                    ct);
        return new MintHandlerBolt11(this._wallet, quoteBolt11, this._keyset, outputs);
    }

    public async Task<IMintHandler<PostMintQuoteBolt12Response, List<Proof>>> ProcessAsyncBolt12(
        CancellationToken ct = default)
    {
        await this._wallet._maybeSyncKeys(ct);
        if (this._pubkey == null)
        {
            throw new ArgumentNullException(nameof(_pubkey), "Can't request bolt12 mint quote without pubkey!");
        }
            
        if (this._keyset == null)
        {
            this._keyset = await this._wallet.GetKeys(this._keysetId, false, ct) ??
                           throw new ArgumentException($"Cant fetch keys for keysetId: {_keysetId}");
        }
            
        var outputs = await this._createOutputs();


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
                    ct);
        return new MintHandlerBolt12(this._wallet, mintQuote, this._keyset, outputs);

    }

    // skipped checks for keysetid and keys, since its validated before. make sure to remember about it.
    async Task<List<OutputData>> _createOutputs()
    {
        var outputs = new List<OutputData>();
        
        if (this._outputs != null)
        {
            if (this._builder is not null)
            {
                throw new ArgumentException("Can't create p2pk outputs if outputs provided. Remove either p2pk builder parameter or outputs.");
            }
            return this._outputs;
        }
        
        if (this._amount is null && this._amounts is null)
        {
            throw new ArgumentNullException(nameof(_amount), "Amount can't be determined. Make sure to include amount, or amounts parameter!");
        }
        _amounts ??=  Utils.SplitToProofsAmounts(_amount.Value, _keyset!.Keys);
        
        if (this._builder is null)
        {
            return await _wallet.CreateOutputs(_amounts, this._keysetId!);
        }
        
        
        foreach (var amount in _amounts)
        {
            var p2pkOutput = Utils.CreateNut10Output(amount, this._keysetId!, _builder);
            outputs.Add(p2pkOutput);
        }
        return outputs;

    }
}