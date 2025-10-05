using DotNut.ApiModels;
using DotNut.ApiModels.Melt.bolt12;

namespace DotNut.Abstractions.Interfaces;

/// <summary>
/// Melt operation builder (pay invoices)
/// </summary>
public interface IMeltQuoteBuilder
{
    IMeltQuoteBuilder WithUnit(string unit);
    IMeltQuoteBuilder WithInvoice(string bolt11Invoice);
    Task<IMeltHandler<PostMeltQuoteBolt11Response, List<Proof>>> ProcessAsyncBolt11(CancellationToken cancellationToken = default);
    Task<IMeltHandler<PostMeltQuoteBolt12Response, List<Proof>>> ProcessAsyncBolt12(CancellationToken cancellationToken = default);

}