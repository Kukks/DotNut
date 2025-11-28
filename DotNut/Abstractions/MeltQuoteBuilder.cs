using DotNut.Abstractions.Handlers;
using DotNut.ApiModels;
using DotNut.ApiModels.Melt.bolt12;

namespace DotNut.Abstractions;

class MeltQuoteBuilder : IMeltQuoteBuilder
{
    private readonly Wallet _wallet;
    private List<Proof>? _proofs;
    private string? _invoice;
    private List<OutputData>? _blankOutputs;
    private string _unit = "sat";
    
    private List<PrivKey>? _privKeys;
    private string? _htlcPreimage;

    private Action<PostMintQuoteBolt11Response>? _callback;
    
    public MeltQuoteBuilder(Wallet wallet)
    {
        _wallet = wallet;
    }

    public IMeltQuoteBuilder WithInvoice(string invoice)
    {
        this._invoice = invoice;
        return this;
    }
    
    public IMeltQuoteBuilder WithUnit(string unit)
    {
        this._unit = unit;
        return this;
    }
    
    public IMeltQuoteBuilder WithBlankOutputs(List<OutputData> blankOutputs)
    {
        this._blankOutputs = blankOutputs;
        return this;
    }
    

    // when proofs were p2pk
    public IMeltQuoteBuilder WithPrivKeys(IEnumerable<PrivKey> privKeys)
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
        var mintApi = await _wallet.GetMintApi(ct);
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
        var mintApi = await _wallet.GetMintApi(ct);
        await _wallet._maybeSyncKeys(ct);
        ArgumentNullException.ThrowIfNull(this._invoice);

        var req = new PostMeltQuoteBolt12Request()
        {
            Request = this._invoice,
            Unit = this._unit,
            // todo melt quote bolt12 options
        };
        var quote =
            await mintApi.CreateMeltQuote<PostMeltQuoteBolt12Response, PostMeltQuoteBolt12Request>("bolt12", req, ct);
        
        
        if (_blankOutputs == null)
        {
            var outputsAmount = Utils.CalculateNumberOfBlankOutputs((ulong)quote.FeeReserve);
            var amounts = Enumerable.Repeat(1UL, outputsAmount).ToList();
            this._blankOutputs = await this._wallet.CreateOutputs(amounts, this._unit, ct);
        }
        return new MeltHandlerBolt12(_wallet, quote, _blankOutputs, _privKeys, _htlcPreimage);
    }
}

