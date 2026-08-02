using DotNut.NBitcoin.BIP39;
using DotNut.NUT13;

namespace DotNut.Tests.Unit;

public class Nut20DerivationTests
{
    private const string Mnemonic =
        "half depart obvious quality work element tank gorilla view sugar picture humble";

    [Fact]
    public void QuoteKeyDerivationTests()
    {
        // Test vectors from tests/20-test.md, "Deterministic quote locking key derivation"
        // (m/129373'/20'/0'/0'/{counter})
        var mnemonic = new Mnemonic(Mnemonic);
        string[] keys =
        [
            "03062837166e56114b59a4d1fd3a5a812bf7aadc1dde758428cf943d80acd41539",
            "02b47d9d41725f5ce6f08c874835cef25376cb1e95f6cb073fef52ca8fd986cf15",
            "029acbd3a46fd75bc05ba0226d0b4d909b2fb6e96c80544a094a1a3567737e44d3",
            "0373e4a42fbe0a4e18aadb57cf500b655f2446b4071ee579121d2ed8905bcc49c2",
            "02b8709bfce17c10f1864f5218844533ae60930d52089669b317d8b5f474eec071",
        ];
        for (var i = 0u; i < (uint)keys.Length; i++)
        {
            var privkey = mnemonic.DeriveMintQuotePrivkey(i);
            Assert.Equal(new PubKey(keys[i]), (PubKey)privkey.Key.CreatePubKey());
        }
    }

    [Fact]
    public void QuoteKeysAreIndependentFromP2PkKeys()
    {
        var mnemonic = new Mnemonic(Mnemonic);

        // Same seed and counter, different account index, so the two must never collide.
        Assert.NotEqual(
            mnemonic.DeriveMintQuotePrivkey(0).Key.CreatePubKey().ToHex(),
            mnemonic.DeriveP2PkPrivkey(0).Key.CreatePubKey().ToHex()
        );
    }

    [Fact]
    public void QuoteKeyRejectsHardenedCounter()
    {
        var mnemonic = new Mnemonic(Mnemonic);

        Assert.NotNull(mnemonic.DeriveMintQuotePrivkey(int.MaxValue));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            mnemonic.DeriveMintQuotePrivkey((uint)int.MaxValue + 1)
        );
    }
}
