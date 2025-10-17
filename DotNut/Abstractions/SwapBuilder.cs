using System.Text.Json;
using DotNut.Abstractions.Interfaces;
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
    
    private OutputData? _outputs;
    private List<ulong>? _amounts;
    private KeysetId? _keysetId;
    
    
    private string _unit = "sat";
    private bool _verifyDLEQ = true;

    private bool _includeFees = true;

    //p2pk stuff
    private List<PrivKey>? _privKeys;
    private P2PkBuilder? _builder;

    private string? _htlcPreimage;
    
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
    
    /// <summary>
    /// Optional. Base unit of wallet instance. If not set defaults to "SAT".
    /// </summary>
    /// <param name="unit"></param>
    public ISwapBuilder WithUnit(string unit)
    {
        this._unit = unit;
        return this;
    }

    /// <summary>
    /// Provide inputs for a swap. 
    /// </summary>
    /// <param name="proofs"></param>
    /// <returns></returns>
    public ISwapBuilder FromInputs(IEnumerable<Proof> proofs)
    {
        this._proofsToSwap = proofs.ToList();
        return this;
    }

    public ISwapBuilder ForOutputs(OutputData outputs)
    {
        this._outputs = outputs;
        return this;
    }

    /// <summary>
    /// Optional.
    /// True by default, allows user to turn off DLEQ verification (not advised)
    /// </summary>
    /// <param name="verify"></param>
    /// <returns></returns>
    public ISwapBuilder WithDLEQVerification(bool verify = true)
    {
        _verifyDLEQ = verify;
        return this;
    }

    /// <summary>
    /// Optional.
    /// Allows user to turn off fee calculation. By default, it will calculate and generate smaller set of outputs.
    /// </summary>
    /// <param name="includeFees"></param>
    /// <returns></returns>
    public ISwapBuilder WithFeeCalculation(bool includeFees = true)
    {
        this._includeFees = includeFees;
        return this;
    }

    /// <summary>
    /// Optional. Allows user to choose amounts he wants to get.
    /// If sum of amounts smaller than input size, all proofs will be swapped, but rest of proofs will get
    /// standard outputs amounts (biggest proof size possible)
    /// </summary>
    /// <param name="amounts"></param>
    /// <returns></returns>
    public ISwapBuilder WithAmounts(IEnumerable<ulong> amounts)
    {
        _amounts = amounts.ToList();
        return this;
    }
    
    /// <summary>
    /// Optional. Allows user to choose destination keysetId
    /// </summary>
    /// <param name="keysetId"></param>
    /// <returns></returns>
    public ISwapBuilder ForKeyset(KeysetId keysetId)
    {
        _keysetId = keysetId;
        return this;
    }

    // when proofs were p2pk
    public ISwapBuilder WithPrivkeys(IEnumerable<PrivKey> privKeys)
    {
        this._privKeys = privKeys.ToList();
        return this;
    }

    /// <summary>
    /// Optional.
    /// If provided, every proof will be generated with random nonce.
    /// P2Pk tokens aren't deterministic. if lost - ¯\_(ツ)_/¯
    /// </summary>
    /// <param name="inputData"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
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
    
    public async Task<List<Proof>> ProcessAsync(CancellationToken ct = default)
    {
        var mintApi = await _wallet.GetMintApi(ct);
        
        var swapInputs = await _getSwapProofs(ct);
        if (swapInputs == null || swapInputs.Count == 0)
        {
            throw new ArgumentException("Nothing to swap!");
        }
        
        // if there's no keysetId specified - let's choose it. 
        if (_keysetId == null)
        {
            _keysetId = await _wallet.GetActiveKeysetId(this._unit, ct) ??
                        throw new InvalidOperationException("Could not fetch Keyset ID");
        }
        var keys = await _wallet.GetKeys(false, ct);
        var keysForCurrentId = keys.Single(k=>k.Id == _keysetId);
        
        if (_verifyDLEQ)
        {
            foreach (var proof in swapInputs!)
            {
               var keyset = keys.Single(k => k.Id == proof.Id);
               if (!keyset.Keys.TryGetValue(proof.Amount, out var key))
               {
                   throw new InvalidOperationException($"Can't find key for amount {proof.Amount} in keyset {keyset.Id}");
               }
               var isValid = proof.Verify(key);
                if (!isValid)
                {
                    throw new InvalidOperationException($"Invalid proof signature for amount {proof.Amount}");
                }
            }
        }

        var fee = 0UL;
        if (_includeFees)
        {
            var keysetsFees = (await _wallet.GetKeysets(false, ct)).ToDictionary(k=>k.Id, k=>k.InputFee??0);
            fee = swapInputs.ComputeFee(keysetsFees);
        }
        
        
        var total = CashuUtils.SumProofs(swapInputs);
        
        // Swap received proofs to our keyset
        var outputs = await this._getOutputs(keysForCurrentId.Keys, ct);
        
        var request = new PostSwapRequest()
        {
            Inputs = swapInputs.ToArray(),
            Outputs = outputs.BlindedMessages.ToArray(),
        };

        await _maybeProcessP2Pk();
        
        var swapResponse = await mintApi.Swap(request, ct);

        var swappedProofs =
            CashuUtils.ConstructProofsFromPromises(swapResponse.Signatures.ToList(), this._outputs, keysForCurrentId.Keys);

        return swappedProofs;
    }
    
    private async Task<List<Proof>> _getSwapProofs(CancellationToken ct = default)
    {
        _proofsToSwap ??= new();
        if (_tokenString != null)
        {
            var token = CashuTokenHelper.Decode(this._tokenString, out var v);
            if (v == "A") // todo ensure 
            {
                //if token is v1, ensure everything is from the same mint 
                var mints = token.Tokens.Select(t => t.Mint).ToList();
                if (mints.Count > 1)
                {
                    throw new ArgumentException("Only swap from single mint is allowed");
                }
                
            }
            this._proofsToSwap.AddRange(token.Tokens.SelectMany(t=>t.Proofs));
        }

        if (_token == null)
        {
            return _proofsToSwap;
        }
        
        //if token is v1, ensure everything is from the same mint 
        var tokenMints = _token.Tokens.Select(t => t.Mint).ToList();
        if (tokenMints.Count > 1)
        {
            throw new ArgumentException("Only swap from single mint is allowed");
        }
        this._proofsToSwap.AddRange(_token.Tokens.SelectMany(t=>t.Proofs));
        
        return _proofsToSwap;
    }

    async Task<OutputData> _getOutputs(Keyset keys, CancellationToken ct = default)
    {
        var outputs = new OutputData();

        if (this._outputs != null)
        {
            if (this._builder is not null)
            {
                throw new ArgumentException("Can't create p2pk outputs if outputs provided. Remove either p2pk builder parameter or outputs.");
            }
            return this._outputs;
        }
        
        if (this._amounts is null)
        {
            throw new ArgumentNullException(nameof(_amounts), "Amounts can't be null.");
        }
        
        var createdOutputs = new List<OutputData>();
        if (this._builder is not null)
        {
            // skipped checks for keysetid and keys, since its validated before. make sure to remember about it.
            foreach (var p2pkOutput in _amounts.Select(amount => CashuUtils.CreateP2PkOutput(amount, this._keysetId!, keys, _builder)))
            {
                outputs.BlindingFactors.Add(p2pkOutput.BlindingFactors[0]);
                outputs.BlindedMessages.Add(p2pkOutput.BlindedMessages[0]);
                outputs.Secrets.Add(p2pkOutput.Secrets[0]);
            }

            return outputs;
        }
        
        return await _wallet.CreateOutputs(_amounts, this._keysetId!, ct);
    }
    
    private async Task _maybeProcessP2Pk()
    {
        if (_privKeys == null || _privKeys.Count == 0)
        {
            return;
        }
        
        if (_proofsToSwap == null)
        {
            throw new ArgumentNullException(nameof(_proofsToSwap), "No proofs to swap!");
        }
        
        var sigAllHandler = new SigAllHandler
        {
            Proofs = this._proofsToSwap,
            BlindedMessages = this._outputs?.BlindedMessages ?? [],
            HTLCPreimage = this._htlcPreimage,
        };

        if (sigAllHandler.TrySign(out P2PKWitness? witness))
        {
            if (witness == null)
            {
                throw new ArgumentNullException(nameof(witness), "sig_all input was correct, but couldn't create a witness signature!");
            }
            this._proofsToSwap[0].Witness = JsonSerializer.Serialize(witness);
        }

        foreach (var proof in _proofsToSwap)
        {
            
            if (proof.Secret is not Nut10Secret { ProofSecret: P2PKProofSecret p2pk, Key: { } key }) continue;
            if (proof.Secret is Nut10Secret { ProofSecret: HTLCProofSecret htlc } && _htlcPreimage is {} preimage)
            {
                var w = htlc.GenerateWitness(proof, _privKeys.Select(p=>p.Key).ToArray(), preimage);
                proof.Witness = JsonSerializer.Serialize(w);
                continue;
            }
            var proofWitness = p2pk.GenerateWitness(proof, _privKeys.Select(p => p.Key).ToArray());
            proof.Witness = JsonSerializer.Serialize(proofWitness);
        }
    }

    private async Task<List<ulong>> _getAmounts(ulong total, ulong fee, Keyset keys)
    {
        if (_amounts != null)
        {
            var sum = _amounts.Sum();
            
            if (sum + fee == total)
            {
                return _amounts;
            }
            if (sum + fee < total)
            {
                var underpay = total - fee - sum;
                this._amounts.AddRange(CashuUtils.SplitToProofsAmounts(underpay, keys));
                return this._amounts;
            }

            throw new ArgumentException($"Invalid amounts requested. Sum of amounts: {sum}, total input: {total}, fee:{fee}.");
        }

        this._amounts = CashuUtils.SplitToProofsAmounts(total - fee, keys);
        return this._amounts;
    }

}