namespace DotNut.Abstractions;

/// <summary>
/// Restore operation builder
/// </summary>
public interface IRestoreBuilder
{
    /// <summary>
    /// Optional and usually not-advised. Allows to specify keysets that we want to restore.
    /// If not set, every keyset is grinded.
    /// </summary>
    /// <param name="keysetIds"></param>
    /// <returns></returns>
    IRestoreBuilder FromKeysetIds(IEnumerable<KeysetId> keysetIds);

    Task<IEnumerable<Proof>> ProcessAsync(CancellationToken ct = default);
}
