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
    
    /// <summary>
    /// Optional, allows to set counter which will hold the state after restore.
    /// If not set, defaults to InMemoryCounter that is not returned.
    /// </summary>
    /// <param name="counter"></param>
    /// <returns></returns>
    public IRestoreBuilder WithCounter(ICounter counter);
    
    Task<IEnumerable<Proof>> ProcessAsync(CancellationToken ct = default);
}
