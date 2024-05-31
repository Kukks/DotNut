using NBitcoin.Secp256k1;


public static class ConvertUtils
{
    public static ECPubKey ToPubKey(this string hex)
    {
        return ECPubKey.Create(global::System.Convert.FromHexString(hex));
    }

    public static ECPrivKey ToPrivKey(this string hex)
    {
        return ECPrivKey.Create(global::System.Convert.FromHexString(hex));
    }
}