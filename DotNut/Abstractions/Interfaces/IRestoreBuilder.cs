namespace DotNut.Abstractions.Interfaces;

/// <summary>
/// Restore operation builder
/// </summary>
public interface IRestoreBuilder
{
    RestoreBuilder ForKeysetIds(IEnumerable<KeysetId> keysetIds);
    IRestoreBuilder WithSwap(bool shouldSwap = true);
    Task<IEnumerable<Proof>> ProcessAsync(CancellationToken cancellationToken = default);
}
