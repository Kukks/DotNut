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
    /// Optional for bolt11 and mandatory for bolt12.
    /// </summary>
    /// <param name="pubkey"></param>
    /// <returns></returns>
    IMintQuoteBuilder WithPubkey(string pubkey);

    /// <summary>
    /// Optional for bolt11 and mandatory for bolt12.
    /// </summary>
    IMintQuoteBuilder WithPubkey(PubKey pubkey);

    /// <summary>
    /// Optional. Provide precomputed outputs so blinding factors and secrets are reused safely.
    /// </summary>
    IMintQuoteBuilder WithOutputs(IEnumerable<OutputData> outputs);

    /// <summary>
    /// Optional. Provide description for the mint invoice.
    /// </summary>
    IMintQuoteBuilder WithDescription(string description);

    /// <summary>
    /// Optional. Allows providing a P2PK builder when a signature is required for minting.
    /// </summary>
    /// <summary>
    /// Optional. Locks the quote to a key derived from the wallet seed at the next NUT-20
    /// counter, instead of one supplied through <see cref="WithPubkey(PubKey)"/>. The quote is
    /// then signed automatically when minting, and the key survives a restore. Requires a
    /// mnemonic and a counter implementing <see cref="IDerivationCounter"/>.
    /// </summary>
    IMintQuoteBuilder WithDeterministicPubkey();

    IMintQuoteBuilder WithP2PkLock(P2PkBuilder p2pkBuilder);

    /// <summary>
    /// Optional. Like <see cref="WithP2PkLock"/>, but the key to lock to is derived from the
    /// wallet seed at the next NUT-13 P2PK counter, so it can be recovered during a restore.
    /// The derived key becomes the primary one; any pubkeys already on the builder are kept
    /// after it. Requires a mnemonic and a counter implementing <see cref="IDerivationCounter"/>.
    /// </summary>
    IMintQuoteBuilder WithDeterministicP2PkLock(P2PkBuilder? p2pkBuilder = null);

    /// <summary>
    /// Optional. When minting P2Pk / HTLC Proofs allows to blind the pubkeys.
    /// </summary>
    /// <param name="withBlinding"></param>
    /// <returns></returns>
    IMintQuoteBuilder BlindPubkeys(bool withBlinding = true);

    /// <summary>
    /// Optional. Allows adding HTLC-based outputs.
    /// </summary>
    IMintQuoteBuilder WithHTLCLock(HTLCBuilder htlcBuilder);

    /// <summary>
    /// Creates a bolt11 mint quote and handler.
    /// </summary>
    Task<IMintHandler<PostMintQuoteBolt11Response, List<Proof>>> ProcessAsyncBolt11(
        CancellationToken ct = default
    );

    /// <summary>
    /// Creates a bolt12 mint quote and handler.
    /// </summary>
    Task<IMintHandler<PostMintQuoteBolt12Response, List<Proof>>> ProcessAsyncBolt12(
        CancellationToken ct = default
    );
}
