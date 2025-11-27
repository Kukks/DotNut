namespace DotNut.Abstractions;

public interface ICounter
{
    public Task<int> GetCounterForId(KeysetId keysetId, CancellationToken ct = default);
    public Task<int> IncrementCounter(KeysetId keysetId, int bumpBy = 1, CancellationToken ct = default);
    public Task SetCounter(KeysetId keysetId, int counter, CancellationToken ct = default);
}