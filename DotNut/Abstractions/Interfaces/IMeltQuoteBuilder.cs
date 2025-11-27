using DotNut.ApiModels;
using DotNut.ApiModels.Melt.bolt12;

namespace DotNut.Abstractions;

/// <summary>
/// Melt operation builder (pay invoices)
/// </summary>
public interface IMeltQuoteBuilder
{
    /// <summary>
    /// Optional. Sets the base unit for the quote; defaults to "sat".
    /// </summary>
    IMeltQuoteBuilder WithUnit(string unit);

    /// <summary>
    /// Mandatory. A bolt11 invoice is required to create a melt quote.
    /// </summary>
    IMeltQuoteBuilder WithInvoice(string bolt11Invoice);

    /// <summary>
    /// Optional. Supply previously generated blank outputs instead of deriving them.
    /// </summary>
    IMeltQuoteBuilder WithBlankOutputs(List<OutputData> blankOutputs);

    /// <summary>
    /// Optional. Provide private keys for P2PK proofs associated with the inputs.
    /// </summary>
    IMeltQuoteBuilder WithPrivKeys(IEnumerable<PrivKey> privKeys);

    /// <summary>
    /// Optional. Supply HTLC preimage to sign HTLC-based proofs.
    /// </summary>
    IMeltQuoteBuilder WithHTLCPreimage(string preimage);

    /// <summary>
    /// Create a bolt11 melt handler.
    /// </summary>
    Task<IMeltHandler<PostMeltQuoteBolt11Response, List<Proof>>> ProcessAsyncBolt11(CancellationToken ct = default);

    /// <summary>
    /// Create a bolt12 melt handler.
    /// </summary>
    Task<IMeltHandler<PostMeltQuoteBolt12Response, List<Proof>>> ProcessAsyncBolt12(CancellationToken ct = default);

}