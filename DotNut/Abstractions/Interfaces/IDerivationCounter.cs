namespace DotNut.Abstractions;

/// <summary>
/// Counters for derivations that are not tied to a keyset, such as NUT-13 P2PK keys.
/// Kept separate from <see cref="ICounter"/> so that existing implementations keep compiling;
/// an <see cref="ICounter"/> that does not also implement this only lacks those derivations.
/// </summary>
public interface IDerivationCounter
{
    /// <summary>
    /// Gets the counter for a derivation purpose. Like the keyset counters, this is the value to
    /// use for the next derivation, so keep it at last used + 1.
    /// </summary>
    public Task<uint> GetCounter(DerivationPurpose purpose, CancellationToken ct = default);

    /// <inheritdoc cref="GetCounter"/>
    public Task<(uint oldValue, uint newValue)> FetchAndIncrement(
        DerivationPurpose purpose,
        uint bumpBy = 1,
        CancellationToken ct = default
    );

    /// <inheritdoc cref="GetCounter"/>
    public Task SetCounter(
        DerivationPurpose purpose,
        uint counter,
        CancellationToken ct = default
    );
}
