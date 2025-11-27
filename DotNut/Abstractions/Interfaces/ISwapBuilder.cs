namespace DotNut.Abstractions;

/// <summary>
/// Swap operation builder
/// </summary>
public interface ISwapBuilder
{
    /// <summary>
    /// Optional. Sets wallet unit for the swap; defaults to "sat".
    /// </summary>
    ISwapBuilder WithUnit(string unit);

    /// <summary>
    /// Optional. Choose target keyset for the swapped proofs.
    /// </summary>
    ISwapBuilder ForKeyset(KeysetId targetKeysetId);

    /// <summary>
    /// Provide proofs that will be used as inputs for the swap.
    /// </summary>
    ISwapBuilder FromInputs(IEnumerable<Proof> inputs);

    /// <summary>
    /// Optional. Supply custom blank outputs instead of deriving them automatically.
    /// </summary>
    ISwapBuilder ForOutputs(List<OutputData> outputs);

    /// <summary>
    /// Optional. Toggle DLEQ verification for incoming proofs.
    /// </summary>
    ISwapBuilder WithDLEQVerification(bool verify = true);

    /// <summary>
    /// Optional. Include or skip fee calculations when creating outputs.
    /// </summary>
    ISwapBuilder WithFeeCalculation(bool includeFees = true);

    /// <summary>
    /// Optional. Explicitly select output amounts.
    /// </summary>
    ISwapBuilder WithAmounts(IEnumerable<ulong> amounts);

    /// <summary>
    /// Optional. Provide private keys associated with the proofs.
    /// </summary>
    ISwapBuilder WithPrivkeys(IEnumerable<PrivKey> privKeys);

    /// <summary>
    /// Optional. Generate outputs guarded by P2PK locking.
    /// </summary>
    ISwapBuilder ToP2PK(P2PkBuilder p2pkBuilder);

    /// <summary>
    /// Optional. Blind P2Pk / HTLC proofs.
    /// </summary>
    /// <param name="withBlinding"></param>
    /// <returns></returns>
    ISwapBuilder BlindPubkeys(bool withBlinding = true);

    /// <summary>
    /// Optional. Supply preimage for HTLC-based proofs.
    /// </summary>
    ISwapBuilder WithHtlcPreimage(string preimage);

    /// <summary>
    /// Optional. Generate HTLC outputs.
    /// </summary>
    ISwapBuilder ToHTLC(HTLCBuilder htlcBuilder);

    /// <summary>
    /// Executes the swap flow and returns newly minted proofs.
    /// </summary>
    Task<List<Proof>> ProcessAsync(CancellationToken ct = default);
}
