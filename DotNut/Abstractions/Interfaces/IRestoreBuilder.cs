namespace DotNut.Abstractions;

/// <summary>
/// Restore operation builder
/// </summary>
public interface IRestoreBuilder
{
    IRestoreBuilder ForKeysetIds(IEnumerable<KeysetId> keysetIds);
    Task<IEnumerable<Proof>> ProcessAsync(CancellationToken ct = default);
}
