using DotNut.Abstractions.Interfaces;

namespace DotNut.Abstractions;

public class InMemoryCounter : ICounter
{
    private IDictionary<KeysetId, int> _counter;

    public InMemoryCounter(IDictionary<KeysetId, int> counter)
    {
        this._counter = counter;
    }

    public InMemoryCounter()
    {
        this._counter = new Dictionary<KeysetId, int>();
    }

    public async Task<int> GetCounterForId(KeysetId keysetId, CancellationToken cts = default)
    {
        if (_counter.TryGetValue(keysetId, out var counter))
            return counter;

        return _counter[keysetId] = 0;
    }

    public async Task<int> IncrementCounter(KeysetId keysetId, int bumpBy = 1, CancellationToken cts = default)
    {
        var current = await GetCounterForId(keysetId, cts);
        var next = current + bumpBy;
        _counter[keysetId] = next;
        return next;
    }

    public async Task SetCounter(KeysetId keysetId, int counter, CancellationToken cts = default) => _counter[keysetId] = counter;

}