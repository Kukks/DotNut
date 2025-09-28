using DotNut;
using DotNut.Abstractions.Interfaces;

public class Counter : ICounter
{
    private Dictionary<KeysetId, int> _counter;
    public Counter(IDictionary<KeysetId, int> dictionary){ }
    public async Task<int> GetCounterForId(KeysetId keysetId)
    {
        if (_counter.TryGetValue(keysetId, out var counter))
            return counter;

        return _counter[keysetId] = 0;
    }

    public async Task<int> IncrementCounter(KeysetId keysetId, int bumpBy = 1)
    {
        var current = await GetCounterForId(keysetId);
        var next = current + bumpBy;
        _counter[keysetId] = next;
        return next;
    }

    public async Task SetCounter(KeysetId keysetId, int counter) => _counter[keysetId] = counter;
}