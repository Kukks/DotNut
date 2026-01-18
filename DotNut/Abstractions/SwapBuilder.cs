using System.Security.Cryptography;
using DotNut.ApiModels;

namespace DotNut.Abstractions;

/// <summary>
/// Receive operation builder implementation
/// </summary>
class SwapBuilder : ISwapBuilder
{
    private readonly Wallet _wallet;

    // input
    private readonly string? _tokenString;
    private readonly CashuToken? _token;
    private List<Proof>? _proofsToSwap;

    private List<OutputData>? _outputs;
    private List<ulong>? _amounts;
    private KeysetId? _targetKeysetId;

    private string _unit = "sat";
    private bool _verifyDleq = true;

    private bool _includeFees = true;

    //nut10 stuff
    private List<PrivKey>? _privKeys;
    private P2PkBuilder? _builder;
    private string? _htlcPreimage;
    private bool _shouldBlind = false;

    public SwapBuilder(Wallet wallet, string tokenString)
    {
        _wallet = wallet;
        _tokenString = tokenString;
    }

    public SwapBuilder(Wallet wallet, CashuToken token)
    {
        _wallet = wallet;
        _token = token;
    }

    public SwapBuilder(Wallet wallet)
    {
        _wallet = wallet;
    }

    public ISwapBuilder WithUnit(string unit)
    {
        this._unit = unit;
        return this;
    }

    public ISwapBuilder FromInputs(IEnumerable<Proof> proofs)
    {
        this._proofsToSwap = proofs.DeepCopyList();
        return this;
    }

    public ISwapBuilder ForOutputs(List<OutputData> outputs)
    {
        this._outputs = outputs;
        return this;
    }

    public ISwapBuilder WithDLEQVerification(bool verify = true)
    {
        _verifyDleq = verify;
        return this;
    }

    public ISwapBuilder WithFeeCalculation(bool includeFees = true)
    {
        this._includeFees = includeFees;
        return this;
    }

    public ISwapBuilder WithAmounts(IEnumerable<ulong> amounts)
    {
        _amounts = amounts.ToList();
        return this;
    }

    public ISwapBuilder ForKeyset(KeysetId keysetId)
    {
        _targetKeysetId = keysetId;
        return this;
    }

    // when proofs were p2pk
    public ISwapBuilder WithPrivkeys(IEnumerable<PrivKey> privKeys)
    {
        this._privKeys = privKeys.ToList();
        return this;
    }

    public ISwapBuilder ToP2PK(P2PkBuilder p2pkBuilder)
    {
        this._builder = p2pkBuilder;
        return this;
    }

    public ISwapBuilder WithHtlcPreimage(string preimage)
    {
        this._htlcPreimage = preimage;
        return this;
    }

    public ISwapBuilder ToHTLC(HTLCBuilder htlcBuilder)
    {
        this._builder = htlcBuilder;
        return this;
    }

    // P2Bk should be compatible with both p2pk and HTLC. Not implemented in the second one
    public ISwapBuilder BlindPubkeys(bool withBlinding = true)
    {
        this._shouldBlind = withBlinding;
        return this;
    }

    public async Task<List<Proof>> ProcessAsync(CancellationToken ct = default)
    {
        var mintApi = await _wallet.GetMintApi(ct);

        var swapInputs = _getSwapProofs();
        if (swapInputs == null || swapInputs.Count == 0)
        {
            throw new ArgumentException("Nothing to swap!");
        }

        // if there's no keysetId specified - let's choose it.
        if (_targetKeysetId == null)
        {
            _targetKeysetId =
                await _wallet.GetActiveKeysetId(this._unit, ct)
                ?? throw new InvalidOperationException("Could not fetch Keyset ID");
        }
        var keysForCurrentId = await _wallet.GetKeys(_targetKeysetId, true, false, ct);

        if (keysForCurrentId == null)
        {
            throw new InvalidOperationException($"Can't find keys for keyset {_targetKeysetId}");
        }
        
        if (_verifyDleq)
        {
            foreach (var proof in swapInputs)
            {
                if (proof.DLEQ == null)
                {
                    throw new ArgumentNullException(nameof(proof.DLEQ), "Can't verify non-existent DLEQ proof!");
                }
                // proof may be already inactive - make sure to fetch
                var keyset = await _wallet.GetKeys(proof.Id, true, false, ct);
                if (keyset == null)
                {
                    throw new InvalidOperationException(
                        $"Can't find keys for keyset id ${proof.Id}"
                    );
                }
                
                if (!keyset.Keys.TryGetValue(proof.Amount, out var key))
                {
                    throw new InvalidOperationException(
                        $"Can't find key for amount {proof.Amount} in keyset {keyset.Id}"
                    );
                }
                var isValid = proof.Verify(key);
                if (!isValid)
                {
                    throw new InvalidOperationException(
                        $"Invalid proof signature for amount {proof.Amount}"
                    );
                }
            }
        }

        var fee = 0UL;
        if (_includeFees)
        {
            // returns also non-active keysets.
            var keysetsFees = (await _wallet.GetKeysets(false, ct)).ToDictionary(
                k => k.Id,
                k => k.InputFee ?? 0
            );
            fee = swapInputs.ComputeFee(keysetsFees);
        }

        var total = Utils.SumProofs(swapInputs);

        this._amounts ??= this._getAmounts(total, fee, keysForCurrentId.Keys);

        // Swap received proofs to our keyset
        var outputs = await this._getOutputs(keysForCurrentId.Keys, ct);

        Nut10Helper.MaybeProcessNut10(_privKeys ?? [], swapInputs, outputs, _htlcPreimage);
        swapInputs.ForEach(i => i.StripFingerprints());
        var request = new PostSwapRequest()
        {
            Inputs = swapInputs.ToArray(),
            Outputs = outputs.Select(o => o.BlindedMessage).ToArray(),
        };

        var swapResponse = await mintApi.Swap(request, ct);

        var swappedProofs = Utils.ConstructProofsFromPromises(
            swapResponse.Signatures.ToList(),
            outputs,
            keysForCurrentId.Keys
        );

        return swappedProofs;
    }

    private List<Proof> _getSwapProofs()
    {
        _proofsToSwap ??= new();

        if (_tokenString != null)
        {
            var token = CashuTokenHelper.Decode(this._tokenString, out var v);
            ValidateSingleMint(token);
            this._proofsToSwap.AddRange(token.Tokens.SelectMany(t => t.Proofs));
        }

        if (_token != null)
        {
            ValidateSingleMint(_token);
            this._proofsToSwap.AddRange(_token.Tokens.SelectMany(t => t.Proofs));
        }

        return _proofsToSwap;
    }

    private async Task<List<OutputData>> _getOutputs(Keyset keys, CancellationToken ct = default)
    {
        if (this._outputs != null)
        {
            if (this._builder is not null)
            {
                throw new ArgumentException(
                    "Can't create nut10 outputs by builder if outputs provided. Remove either p2pk builder parameter or outputs."
                );
            }
            return this._outputs;
        }

        if (this._amounts is null)
        {
            throw new ArgumentNullException(nameof(_amounts), "Amounts can't be null.");
        }

        var outputs = new List<OutputData>();
        if (this._builder is not null)
        {
            if (this._shouldBlind)
            {
                if (this._builder.SigFlag == "SIG_ALL")
                {
                    var e = new PrivKey(RandomNumberGenerator.GetHexString(64));
                    foreach (var amount in _amounts)
                    {
                        var builder = _builder.Clone();
                        outputs.Add(
                            Utils.CreateNut10BlindedOutput(
                                amount,
                                this._targetKeysetId!,
                                builder,
                                e
                            )
                        );
                    }
                    return outputs;
                }
                foreach (var amount in _amounts)
                {
                    var builder = _builder.Clone();
                    outputs.Add(
                        Utils.CreateNut10BlindedOutput(amount, this._targetKeysetId!, builder)
                    );
                }
                return outputs;
            }
            // skipped checks for keysetid and keys, since its validated before. make sure to remember about it.
            foreach (var amount in _amounts)
            {
                var builder = _builder.Clone();
                outputs.Add(Utils.CreateNut10Output(amount, this._targetKeysetId!, builder));
            }
            return outputs;
        }

        return await _wallet.CreateOutputs(_amounts, this._targetKeysetId!, ct);
    }

    private List<ulong> _getAmounts(ulong total, ulong fee, Keyset keys)
    {
        if (_amounts != null)
        {
            var sum = checked(_amounts.Aggregate(0UL, (acc, val) => acc + val));

            if (checked(sum + fee) == total)
            {
                return _amounts;
            }
            if (sum + fee < total)
            {
                var underpay = checked(total - fee - sum);
                this._amounts.AddRange(Utils.SplitToProofsAmounts(underpay, keys));
                return this._amounts;
            }

            throw new ArgumentException(
                $"Invalid amounts requested. Sum of amounts: {sum}, total input: {total}, fee:{fee}."
            );
        }

        this._amounts = Utils.SplitToProofsAmounts(checked(total - fee), keys);
        return this._amounts;
    }

    private static void ValidateSingleMint(CashuToken token)
    {
        var distinctMints = token.Tokens.Select(t => t.Mint).Distinct().ToList();
        if (distinctMints.Count > 1)
        {
            throw new ArgumentException("Only swap from single mint is allowed");
        }
    }
}
