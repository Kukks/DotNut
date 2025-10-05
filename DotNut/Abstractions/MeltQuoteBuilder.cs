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
    private ulong? _amount;
    private string _unit = "sat";
    
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
    
    /// <summary>
    /// Mandatory.
    /// User needs to specify the amount to be received. It MUST correspond to invoice amount.
    /// </summary>
    /// <param name="amount"></param>
    /// <returns></returns>
    public IMeltQuoteBuilder WithAmount(ulong amount)
    {
        this._amount = amount;
        return this;
    }
    
    public async Task<IMeltHandler<PostMeltQuoteBolt11Response, List<Proof>>> ProcessAsyncBolt11(CancellationToken cts = default)
    {
        var mintApi = await _wallet.GetMintApi();
        await _wallet._maybeSyncKeys(cts);
        // ArgumentNullException.ThrowIfNull(this._amount);
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

        return new MeltHandlerBolt11(_wallet, quote, _blankOutputs);
    }


    public async Task<IMeltHandler<PostMeltQuoteBolt12Response, List<Proof>>> ProcessAsyncBolt12(
        CancellationToken cts = default)
    {
        throw new NotImplementedException();
    }
}

