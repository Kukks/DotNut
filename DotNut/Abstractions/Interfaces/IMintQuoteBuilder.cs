using DotNut.ApiModels;
using DotNut.ApiModels.Mint.bolt12;

namespace DotNut.Abstractions;

/// <summary>
/// Mint operation builder (receive from invoice)
/// </summary>
public interface IMintQuoteBuilder
{
    /// <summary>
    /// Optional. Sets unit of tokens being minted; defaults to satoshi.
    /// </summary>
    IMintQuoteBuilder WithUnit(string unit);

    /// <summary>
    /// Mandatory. Amount of tokens to mint in the current unit.
    /// </summary>
    IMintQuoteBuilder WithAmount(ulong amount);

    /// <summary>
    /// Optional. Provide precomputed outputs so blinding factors and secrets are reused safely.
    /// </summary>
    IMintQuoteBuilder WithOutputs(List<OutputData> outputs);
    
    /// <summary>
    /// Optional. Provide description for the mint invoice.
    /// </summary>
    IMintQuoteBuilder WithDescription(string description);

    /// <summary>
    /// Optional. Allows providing a P2PK builder when a signature is required for minting.
    /// </summary>
    IMintQuoteBuilder WithP2PkLock(P2PkBuilder p2pkBuilder);

    /// <summary>
    /// Optional. Allows adding HTLC-based outputs.
    /// </summary>
    IMintQuoteBuilder WithHTLCLock(HTLCBuilder htlcBuilder);

    /// <summary>
    /// Creates a bolt11 mint quote and handler.
    /// </summary>
    Task<IMintHandler<PostMintQuoteBolt11Response, List<Proof>>> ProcessAsyncBolt11(CancellationToken ct = default);

    /// <summary>
    /// Creates a bolt12 mint quote and handler.
    /// </summary>
    Task<IMintHandler<PostMintQuoteBolt12Response, List<Proof>>> ProcessAsyncBolt12(CancellationToken ct = default);
    
}