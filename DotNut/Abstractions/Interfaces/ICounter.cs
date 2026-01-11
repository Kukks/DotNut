namespace DotNut.Abstractions;

public interface ICounter
{
    /// <summary>
    /// Gets counter for current keysetID. This counter will be used for next proof generation, so make sure it's
    /// always set to last used proof + 1
    /// </summary>
    /// <param name="keysetId"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public Task<uint> GetCounterForId(KeysetId keysetId, CancellationToken ct = default);
    public Task<uint> IncrementCounter(KeysetId keysetId, uint bumpBy = 1, CancellationToken ct = default);
    public Task SetCounter(KeysetId keysetId, uint counter, CancellationToken ct = default);
    public Task<IReadOnlyDictionary<KeysetId, uint>> Export();
}