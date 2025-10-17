namespace DotNut.Abstractions.Interfaces;

/// <summary>
/// Swap operation builder
/// </summary>
public interface ISwapBuilder
{
    ISwapBuilder WithUnit(string unit);
    ISwapBuilder ForKeyset(KeysetId targetKeysetId);
    ISwapBuilder FromInputs(IEnumerable<Proof> inputs);
    ISwapBuilder ForOutputs(OutputData outputs);
    ISwapBuilder WithDLEQVerification(bool verify = true);
    ISwapBuilder WithFeeCalculation(bool includeFees = true);
    ISwapBuilder WithAmounts(IEnumerable<ulong> amounts);
    ISwapBuilder WithPrivkeys(IEnumerable<PrivKey> privKeys);
    ISwapBuilder ToP2PK(P2PkBuilder p2pkBuilder);
    ISwapBuilder WithHtlcPreimage(string preimage);
    ISwapBuilder ToHTLC(HTLCBuilder htlcBuilder);
    Task<List<Proof>> ProcessAsync(CancellationToken ct = default);
}
