using NBitcoin.Secp256k1;

namespace DotNut;

public class StringSecret : ISecret
{
    public StringSecret(string secret)
    {
        Secret = secret;
    }

    public string Secret { get; init; }
    public byte[] GetBytes()
    {
        return System.Text.Encoding.UTF8.GetBytes(Secret);
    }

    public ECPubKey ToCurve()
    {
        return Cashu.HashToCurve(GetBytes());
    }
}