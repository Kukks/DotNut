using System.Security.Cryptography;
using DotNut.Abstractions.Handlers;
using DotNut.Api;
using DotNut.ApiModels;
using DotNut.ApiModels.Mint.bolt12;
using DotNut.ApiModels.Onchain;

namespace DotNut.Abstractions;

class MintQuoteBuilder : IMintQuoteBuilder
{
    private readonly Wallet _wallet;

    private ulong? _amount;
    private List<ulong>? _amounts;
    private string _unit = "sat";
    private string? _description;
    private List<OutputData>? _outputs;

    private string? _pubkey;

    private KeysetId? _keysetId;
    private GetKeysResponse.KeysetItemResponse? _keyset;

    //for p2pk
    private P2PkBuilder? _builder;
    private bool _shouldBlind = false;

    public MintQuoteBuilder(Wallet wallet)
    {
        this._wallet = wallet;
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

    public IMintQuoteBuilder WithPubkey(PubKey pubkey)
    {
        this._pubkey = pubkey.ToString();
        return this;
    }

    public IMintQuoteBuilder WithKeyset(KeysetId keysetId)
    {
        this._keysetId = keysetId;
        return this;
    }

    public IMintQuoteBuilder WithOutputs(IEnumerable<OutputData> outputs)
    {
        var os = outputs as List<OutputData> ?? outputs.ToList();
        if (os.Count == 0)
        {
            throw new ArgumentException("Outputs collection cannot be empty.");
        }
        if (os.Any(o => o.BlindedMessage.Id != os[0].BlindedMessage.Id))
        {
            throw new ArgumentException("Every output must have the same keyset id!");
        }

        this._outputs = os;
        return this;
    }

    public IMintQuoteBuilder WithP2PkLock(P2PkBuilder p2pkBuilder)
    {
        this._builder = p2pkBuilder;
        return this;
    }

    public IMintQuoteBuilder BlindPubkeys(bool withBlinding = true)
    {
        this._shouldBlind = withBlinding;
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
        CancellationToken ct = default
    )
    {
        //todo implement info

        await this._wallet._maybeSyncKeys(ct);
        if (_amount == null)
        {
            throw new ArgumentNullException(
                nameof(_amount),
                "can't create mint quote without amount!"
            );
        }

        var api = await this._wallet.GetMintApi(ct);
        if (api is null)
        {
            throw new ArgumentNullException(
                nameof(ICashuApi),
                "Can't request mint quote without mint API"
            );
        }

        this._keysetId ??=
            await this._wallet.GetActiveKeysetId(this._unit, ct)
            ?? throw new ArgumentException($"Can't get active keyset ID for unit: {_unit}");

        this._keyset ??=
            await this._wallet.GetKeys(this._keysetId, true, false, ct)
            ?? throw new ArgumentException($"Cant get keys for keysetId: {_keysetId}");

        var outputs = await this._createOutputs();

        var reqBolt11 = new PostMintQuoteBolt11Request()
        {
            Amount = this._amount.Value,
            Unit = this._unit,
            Description = this._description,
            Pubkey = this._pubkey,
        };
        var quoteBolt11 = await api.CreateMintQuote<
            PostMintQuoteBolt11Response,
            PostMintQuoteBolt11Request
        >("bolt11", reqBolt11, ct);
        return new MintHandlerBolt11(this._wallet, quoteBolt11, this._keyset, outputs);
    }

    public async Task<MintHandlerBolt12> ProcessAsyncBolt12(
        CancellationToken ct = default
    )
    {
        await this._wallet._maybeSyncKeys(ct);

        var api = await this._wallet.GetMintApi(ct);
        if (api is null)
        {
            throw new ArgumentNullException(
                nameof(ICashuApi),
                "Can't request mint quote without mint API"
            );
        }

        if (this._pubkey == null)
        {
            throw new ArgumentNullException(
                nameof(_pubkey),
                "Can't request bolt12 mint quote without pubkey!"
            );
        }
        this._keysetId ??=
            await this._wallet.GetActiveKeysetId(this._unit, ct)
            ?? throw new ArgumentException($"Can't get active keyset ID for unit: {_unit}");

        if (this._keyset == null)
        {
            this._keyset =
                await this._wallet.GetKeys(this._keysetId, true, false, ct)
                ?? throw new ArgumentException($"Cant fetch keys for keysetId: {_keysetId}");
        }

        var outputs = (_outputs != null || _amount != null || _amounts != null)
            ? await this._createOutputs()
            : [];

        var req = new PostMintQuoteBolt12Request()
        {
            Amount = this._amount,
            Unit = this._unit,
            Pubkey = this._pubkey,
            Description = this._description,
        };
        var mintQuote = await api.CreateMintQuote<
            PostMintQuoteBolt12Response,
            PostMintQuoteBolt12Request
        >("bolt12", req, ct);
        return new MintHandlerBolt12(this._wallet, mintQuote, this._keyset, outputs);
    }

    public async Task<MintHandlerOnchain> ProcessAsyncOnchain(
        CancellationToken ct = default
    )
    {
        await _wallet._maybeSyncKeys(ct);

        if (_pubkey == null)
            throw new ArgumentNullException(nameof(_pubkey), "Pubkey is required for onchain mint quote (NUT-20)");

        var api = await _wallet.GetMintApi(ct);

        _keysetId ??= await _wallet.GetActiveKeysetId(_unit, ct)
            ?? throw new ArgumentException($"Can't get active keyset ID for unit: {_unit}");

        _keyset ??= await _wallet.GetKeys(_keysetId, true, false, ct)
            ?? throw new ArgumentException($"Can't get keys for keysetId: {_keysetId}");

        // Outputs are intentionally deferred for onchain: amount_paid grows as UTXOs confirm,
        // so the wallet sets outputs via WithOutputs before each Mint() call.
        var outputs = (_outputs != null || _amount != null || _amounts != null)
            ? await _createOutputs()
            : [];

        var req = new PostMintQuoteOnchainRequest
        {
            Unit = _unit,
            PublicKey = _pubkey,
        };

        var quote = await api.CreateMintQuote<PostMintQuoteOnchainResponse, PostMintQuoteOnchainRequest>(
            "onchain", req, ct);

        return new MintHandlerOnchain(_wallet, quote, _keyset, outputs);
    }

    // skipped checks for keysetid and keys, since its validated before. make sure to remember about it.
    async Task<List<OutputData>> _createOutputs()
    {
        var outputs = new List<OutputData>();

        if (this._outputs != null)
        {
            if (this._builder is not null)
            {
                throw new ArgumentException(
                    "Can't create p2pk outputs if outputs provided. Remove either p2pk builder parameter or outputs."
                );
            }
            return this._outputs;
        }

        if (this._amount is null && this._amounts is null)
        {
            throw new ArgumentNullException(
                nameof(_amount),
                "Amount can't be determined. Make sure to include amount, or amounts parameter!"
            );
        }
        _amounts ??= Utils.SplitToProofsAmounts(_amount.Value, _keyset!.Keys);

        if (this._builder is null)
        {
            return await _wallet.CreateOutputs(_amounts, this._keysetId!);
        }

        if (this._shouldBlind)
        {
            if (this._builder.SigFlag == "SIG_ALL")
            {
                var e = new PrivKey(RandomNumberGenerator.GetHexString(64));
                foreach (var amount in _amounts)
                {
                    var builder = _builder.Clone();
                    var p2pkOutput = Utils.CreateNut10BlindedOutput(
                        amount,
                        this._keysetId!,
                        builder,
                        e
                    );
                    outputs.Add(p2pkOutput);
                }

                return outputs;
            }

            foreach (var amount in _amounts)
            {
                var builder = _builder.Clone();
                var p2pkOutput = Utils.CreateNut10BlindedOutput(amount, this._keysetId!, builder);
                outputs.Add(p2pkOutput);
            }
            return outputs;
        }

        foreach (var amount in _amounts)
        {
            var builder = _builder.Clone();
            var p2pkOutput = Utils.CreateNut10Output(amount, this._keysetId!, builder);
            outputs.Add(p2pkOutput);
        }
        return outputs;
    }
}
