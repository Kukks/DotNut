using DotNut.ApiModels;
using DotNut.ApiModels.Melt.bolt12;
using DotNut.ApiModels.Melt.Onchain;

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
    /// Mandatory for bolt11/bolt12. A lightning invoice to create a melt quote.
    /// </summary>
    IMeltQuoteBuilder WithInvoice(string bolt11Invoice);

    /// <summary>
    /// Mandatory for onchain. The Bitcoin address to send funds to.
    /// </summary>
    IMeltQuoteBuilder WithAddress(string address);

    /// <summary>
    /// Optional. Supply previously generated blank outputs instead of deriving them.
    /// </summary>
    IMeltQuoteBuilder WithBlankOutputs(IEnumerable<OutputData> blankOutputs);

    /// <summary>
    /// Optional. Provide private keys for P2PK proofs associated with the inputs.
    /// </summary>
    IMeltQuoteBuilder WithPrivKeys(IEnumerable<PrivKey> privKeys);

    /// <summary>
    /// Optional and mandatory if amountless invoice provided. For onchain, the amount to send in the base unit.
    /// </summary>
    IMeltQuoteBuilder WithAmount(ulong amount);

    /// <summary>
    /// Optional. Supply HTLC preimage to sign HTLC-based proofs.
    /// </summary>
    IMeltQuoteBuilder WithHTLCPreimage(string preimage);

    /// <summary>
    /// Optional for onchain melt. Selects the fee option by fee_index from the quote's fee_options array; defaults to the first option.
    /// </summary>
    IMeltQuoteBuilder WithFeeIndex(ulong feeIndex);

    /// <summary>
    /// Create a bolt11 melt handler.
    /// </summary>
    Task<IMeltHandler<PostMeltQuoteBolt11Response, List<Proof>>> ProcessAsyncBolt11(
        CancellationToken ct = default
    );

    /// <summary>
    /// Create a bolt12 melt handler.
    /// </summary>
    Task<IMeltHandler<PostMeltQuoteBolt12Response, List<Proof>>> ProcessAsyncBolt12(
        CancellationToken ct = default
    );

    /// <summary>
    /// Create an onchain melt handler. Requires WithAddress and WithAmount.
    /// </summary>
    Task<IMeltHandler<PostMeltQuoteOnchainResponse, List<Proof>>> ProcessAsyncOnchain(
        CancellationToken ct = default
    );
}
