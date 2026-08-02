using System.Collections.Concurrent;

namespace DotNut.Abstractions;

public class InMemoryCounter : ICounter, IDerivationCounter
{
    private readonly ConcurrentDictionary<KeysetId, uint> _counter;
    private readonly ConcurrentDictionary<DerivationPurpose, uint> _purposeCounter = new();

    public InMemoryCounter(IDictionary<KeysetId, uint> counter)
    {
        this._counter = new ConcurrentDictionary<KeysetId, uint>(counter);
    }

    public InMemoryCounter()
    {
        this._counter = new ConcurrentDictionary<KeysetId, uint>();
    }

    public Task<uint> GetCounterForId(KeysetId keysetId, CancellationToken ct = default)
    {
        return Task.FromResult(_counter.GetOrAdd(keysetId, 0));
    }

    public Task<uint> IncrementCounter(
        KeysetId keysetId,
        uint bumpBy = 1,
        CancellationToken ct = default
    )
    {
        var next = _counter.AddOrUpdate(keysetId, bumpBy, (_, current) => current + bumpBy);
        return Task.FromResult(next);
    }

    public Task<(uint oldValue, uint newValue)> FetchAndIncrement(
        KeysetId keysetId,
        uint bumpBy = 1,
        CancellationToken ct = default
    )
    {
        uint oldValue = 0;
        uint newValue = _counter.AddOrUpdate(
            keysetId,
            bumpBy,
            (_, current) =>
            {
                oldValue = current;
                return current + bumpBy;
            }
        );

        return Task.FromResult((oldValue, newValue));
    }

    public Task SetCounter(KeysetId keysetId, uint counter, CancellationToken ct = default)
    {
        _counter[keysetId] = counter;
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyDictionary<KeysetId, uint>> Export()
    {
        return new Dictionary<KeysetId, uint>(_counter);
    }

    public Task<uint> GetCounter(DerivationPurpose purpose, CancellationToken ct = default)
    {
        return Task.FromResult(_purposeCounter.GetOrAdd(purpose, 0u));
    }

    public Task<(uint oldValue, uint newValue)> FetchAndIncrement(
        DerivationPurpose purpose,
        uint bumpBy = 1,
        CancellationToken ct = default
    )
    {
        uint oldValue = 0;
        uint newValue = _purposeCounter.AddOrUpdate(
            purpose,
            bumpBy,
            (_, current) =>
            {
                oldValue = current;
                return current + bumpBy;
            }
        );

        return Task.FromResult((oldValue, newValue));
    }

    public Task SetCounter(
        DerivationPurpose purpose,
        uint counter,
        CancellationToken ct = default
    )
    {
        _purposeCounter[purpose] = counter;
        return Task.CompletedTask;
    }
}
