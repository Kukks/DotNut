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
    Task<List<Proof>> ProcessAsync(CancellationToken cancellationToken = default);
}
