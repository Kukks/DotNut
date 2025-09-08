using DotNut.NBitcoin.BIP39;
using NBip32Fast;
using NBitcoin;

namespace DotNut.NUT13;

public static class Nut13
{
    public const string Purpose = "129372'";

    public static KeyPath GetNut13DerivationPath(KeysetId keysetId, int counter, bool secretOrr)
    {
       return  (KeyPath) KeyPath.Parse($"m/{Purpose}/0'/{GetKeysetIdInt(keysetId)}'/{counter}'/{(secretOrr?0:1)}")!;
    }
    
    public static long  GetKeysetIdInt(KeysetId keysetId)
    {
        var  keysetIdInt = long .Parse("0" + keysetId, System.Globalization.NumberStyles.HexNumber);
        var  mod = (long )Math.Pow(2, 31) - 1;
        return keysetIdInt % mod;
    }

    public static byte[] DeriveBlindingFactor(this Mnemonic mnemonic, KeysetId keysetId, int counter) =>
        DeriveBlindingFactor(mnemonic.DeriveSeed(), keysetId, counter);

    public static StringSecret DeriveSecret(this Mnemonic mnemonic, KeysetId keysetId, int counter) =>
        DeriveSecret(mnemonic.DeriveSeed(), keysetId, counter);

    public static byte[] DeriveBlindingFactor(this byte[] seed, KeysetId keysetId, int counter)
    {
        return BIP32.Instance.DerivePath(GetNut13DerivationPath(keysetId, counter, false), seed).PrivateKey
            .ToArray();
    }

    public static StringSecret DeriveSecret(this byte[] seed, KeysetId keysetId, int counter)
    {
        var key = BIP32.Instance.DerivePath(GetNut13DerivationPath(keysetId, counter, true), seed).PrivateKey;
        return new StringSecret(Convert.ToHexString(key).ToLower());
    }
}