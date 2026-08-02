using DotNut.NBitcoin.BIP39;
using DotNut.NUT13;
using NBip32Fast;
using NBitcoin.Secp256k1;

namespace DotNut;

public static class MintQuoteKeyDerivation
{
    private const uint HardenedOffset = 0x80000000;

    /// <summary>
    /// Derives a key to lock a mint quote to, using the NUT-20 path
    /// <c>m/129373'/20'/0'/0'/{counter}</c>. Deriving it from the seed means the key can be
    /// recovered during a restore, so quotes that were locked but not yet minted are not lost.
    /// The counter is independent from the NUT-13 keyset counters.
    /// </summary>
    public static PrivKey DeriveMintQuotePrivkey(this Mnemonic mnemonic, uint counter)
    {
        var seed = mnemonic.DeriveSeed();
        return seed.DeriveMintQuotePrivkey(counter);
    }

    /// <inheritdoc cref="DeriveMintQuotePrivkey(Mnemonic, uint)"/>
    public static PrivKey DeriveMintQuotePrivkey(this byte[] seed, uint counter)
    {
        // The counter is a non-hardened child index, so it must stay below 2^31.
        // KeyPath would otherwise silently parse it as a hardened index.
        ArgumentOutOfRangeException.ThrowIfGreaterThan(counter, HardenedOffset - 1, nameof(counter));

        var path = (KeyPath)KeyPath.Parse($"m/129373'/20'/0'/0'/{counter}")!;
        var pkBytes = BIP32.Instance.DerivePath(path, seed).PrivateKey;

        return ECPrivKey.Create(pkBytes);
    }
}
