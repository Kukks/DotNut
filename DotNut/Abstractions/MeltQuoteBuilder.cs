using System.Text.Json;
using DotNut.Abstractions.Handlers;
using DotNut.Abstractions.Interfaces;
using DotNut.ApiModels;
using DotNut.ApiModels.Melt.bolt12;

namespace DotNut.Abstractions;

class MeltQuoteBuilder : IMeltQuoteBuilder
{
    private readonly Wallet _wallet;
    private List<Proof>? _proofs;
    private string? _invoice;
    private OutputData? _blankOutputs;
    private string _unit = "sat";
    
    private List<PrivKey>? _privKeys;
    private string? _htlcPreimage;
    
    public MeltQuoteBuilder(Wallet wallet)
    {
        _wallet = wallet;
    }

    /// <summary>
    /// Mandatory.
    /// Invoice must be provided in order to create (Lightning) MeltQuote. 
    /// </summary>
    /// <param name="invoice"></param>
    /// <returns></returns>
    public IMeltQuoteBuilder WithInvoice(string invoice)
    {
        this._invoice = invoice;
        return this;
    }
    
    /// <summary>
    /// Optional.
    /// If not set, defaults to satoshi. If token has other unit, must be set.
    /// </summary>
    /// <param name="unit"></param>
    /// <returns></returns>
    public IMeltQuoteBuilder WithUnit(string unit)
    {
        this._unit = unit;
        return this;
    }
    
    /// <summary>
    /// Optional. Allows user to specify blank outputs. If not set, these will be generated automatically.
    /// </summary>
    /// <param name="blankOutputs"></param>
    /// <returns></returns>
    public IMeltQuoteBuilder WithBlankOutputs(OutputData blankOutputs)
    {
        this._blankOutputs = blankOutputs;
        return this;
    }
    

    // when proofs were p2pk
    public IMeltQuoteBuilder WithPrivkeys(IEnumerable<PrivKey> privKeys)
    {
        this._privKeys = privKeys.ToList();
        return this;
    }

    public IMeltQuoteBuilder WithHTLCPreimage(string preimage)
    {
        this._htlcPreimage = preimage;
        return this;
    }

    
    public async Task<IMeltHandler<PostMeltQuoteBolt11Response, List<Proof>>> ProcessAsyncBolt11(CancellationToken cts = default)
    {
        var mintApi = await _wallet.GetMintApi();
        await _wallet._maybeSyncKeys(cts);
        ArgumentNullException.ThrowIfNull(this._invoice);
        
        var req = new PostMeltQuoteBolt11Request
        {
            Request = this._invoice,
            Unit = this._unit,
        };

        var quote =
            await mintApi.CreateMeltQuote<PostMeltQuoteBolt11Response, PostMeltQuoteBolt11Request>("bolt11", req, cts);
        
        
        if (_blankOutputs == null)
        {
            var outputsAmount = CashuUtils.CalculateNumberOfBlankOutputs((ulong)quote.FeeReserve);
            var amounts = Enumerable.Repeat(1UL, outputsAmount).ToList();
            this._blankOutputs = await this._wallet.CreateOutputs(amounts, this._unit, cts);
        }

        await _maybeProcessP2PkHTLC(quote.Quote);

        return new MeltHandlerBolt11(_wallet, quote, _blankOutputs);
    }
    
    public async Task<IMeltHandler<PostMeltQuoteBolt12Response, List<Proof>>> ProcessAsyncBolt12(
        CancellationToken cts = default)
    {
        throw new NotImplementedException();
    }
    
    private async Task _maybeProcessP2PkHTLC(string quoteId)
    {
        if (_privKeys == null || _privKeys.Count == 0)
        {
            return;
        }
        
        if (_proofs == null)
        {
            throw new ArgumentNullException(nameof(_proofs), "No proofs to melt!");
        }
        
        var sigAllHandler = new SigAllHandler
        {
            Proofs = this._proofs,
            BlindedMessages = this._blankOutputs?.BlindedMessages ?? [],
            MeltQuoteId = quoteId,
            HTLCPreimage = this._htlcPreimage,
        };

        if (sigAllHandler.TrySign(out P2PKWitness? witness))
        {
            if (witness == null)
            {
                throw new ArgumentNullException(nameof(witness), "sig_all input was correct, but couldn't create a witness signature!");
            }
            this._proofs[0].Witness = JsonSerializer.Serialize(witness);
        }

        foreach (var proof in _proofs)
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
    
}

