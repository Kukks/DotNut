namespace DotNut.Abstractions.Interfaces;

public interface ICounter
{
    public Task<int> GetCounterForId(KeysetId keysetId, CancellationToken cts = default);
    public Task<int> IncrementCounter(KeysetId keysetId, int bumpBy = 1, CancellationToken cts = default);
    public Task SetCounter(KeysetId keysetId, int counter, CancellationToken cts = default);
}