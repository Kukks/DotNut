namespace DotNut.Abstractions;

/// <summary>
/// A wallet-level derivation counter that is not tied to a keyset. Each purpose has its own
/// derivation path and its own counter, independent from the per-keyset NUT-13 counters.
/// </summary>
public enum DerivationPurpose
{
    /// <summary>
    /// NUT-13 P2PK keys to lock proofs to: <c>m/129373'/10'/0'/0'/{counter}</c>.
    /// </summary>
    P2Pk,

    /// <summary>
    /// NUT-20 mint quote locking keys: <c>m/129373'/20'/0'/0'/{counter}</c>.
    /// </summary>
    MintQuoteLock,
}
