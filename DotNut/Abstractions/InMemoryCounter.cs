using System.Collections.Concurrent;

namespace DotNut.Abstractions;

public class InMemoryCounter : ICounter
{
    private readonly ConcurrentDictionary<KeysetId, int> _counter;
    public InMemoryCounter(IDictionary<KeysetId, int> counter)
    {
        this._counter = new ConcurrentDictionary<KeysetId, int>(counter);
    }

    public InMemoryCounter()
    {
        this._counter = new ConcurrentDictionary<KeysetId, int>();
    }

    public Task<int> GetCounterForId(KeysetId keysetId, CancellationToken ct = default)
    {
        return Task.FromResult(_counter.GetOrAdd(keysetId, 0));
    }

    public Task<int> IncrementCounter(KeysetId keysetId, int bumpBy = 1, CancellationToken ct = default)
    {
        var next = _counter.AddOrUpdate(keysetId, bumpBy, (_, current) => current + bumpBy);
        return Task.FromResult(next);
    }

    public Task SetCounter(KeysetId keysetId, int counter, CancellationToken ct = default) {
        _counter[keysetId] = counter;
        return Task.CompletedTask;
    }
    
    public async Task<IReadOnlyDictionary<KeysetId, int>> Export()
    {
        return new Dictionary<KeysetId, int>(_counter);
    }

}