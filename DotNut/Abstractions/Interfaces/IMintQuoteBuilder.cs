using DotNut.ApiModels;
using DotNut.ApiModels.Mint.bolt12;

namespace DotNut.Abstractions.Interfaces;

/// <summary>
/// Mint operation builder (receive from invoice)
/// </summary>
public interface IMintQuoteBuilder
{
    IMintQuoteBuilder WithUnit(string unit);
    IMintQuoteBuilder WithAmount(ulong amount);
    IMintQuoteBuilder WithOutputs(OutputData outputs);
    
    IMintQuoteBuilder WithDescription(string description);

    IMintQuoteBuilder WithP2PkLock(P2PkBuilder p2pkBuilder);
    Task<IMintHandler<PostMintQuoteBolt11Response, List<Proof>>> ProcessAsyncBolt11(CancellationToken ct = default);
    Task<IMintHandler<PostMintQuoteBolt12Response, List<Proof>>> ProcessAsyncBolt12(CancellationToken ct = default);
    
}