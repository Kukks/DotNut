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

    private Action<PostMintQuoteBolt11Response>? _callback;
    
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

    public IMeltQuoteBuilder OnQuoteStateChanged(Action<PostMintQuoteBolt11Response> callback)
    {
        this._callback = callback;
        return this;
    }

    public async Task<IMeltHandler<PostMeltQuoteBolt11Response, List<Proof>>> ProcessAsyncBolt11(CancellationToken ct = default)
    {
        var mintApi = await _wallet.GetMintApi();
        await _wallet._maybeSyncKeys(ct);
        ArgumentNullException.ThrowIfNull(this._invoice);
        
        var req = new PostMeltQuoteBolt11Request
        {
            Request = this._invoice,
            Unit = this._unit,
        };

        var quote =
            await mintApi.CreateMeltQuote<PostMeltQuoteBolt11Response, PostMeltQuoteBolt11Request>("bolt11", req, ct);
        
        
        if (_blankOutputs == null)
        {
            var outputsAmount = Utils.CalculateNumberOfBlankOutputs((ulong)quote.FeeReserve);
            var amounts = Enumerable.Repeat(1UL, outputsAmount).ToList();
            this._blankOutputs = await this._wallet.CreateOutputs(amounts, this._unit, ct);
        }
        return new MeltHandlerBolt11(_wallet, quote, _blankOutputs, _privKeys, _htlcPreimage);
    }
    
    public async Task<IMeltHandler<PostMeltQuoteBolt12Response, List<Proof>>> ProcessAsyncBolt12(
        CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
    
    
}

