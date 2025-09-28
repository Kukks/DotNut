namespace DotNut.Abstractions.Interfaces;

public interface ICounter
{
    public Task<int> GetCounterForId(KeysetId keysetId);
    public Task<int> IncrementCounter(KeysetId keysetId, int bumpBy = 1);
    public Task SetCounter(KeysetId keysetId, int counter);
}