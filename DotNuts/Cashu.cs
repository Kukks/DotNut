using System.Text;
using DotNuts;
using NBitcoin.Secp256k1;
using SHA256 = System.Security.Cryptography.SHA256;

public static class Cashu
{
    private static readonly byte[] DOMAIN_SEPARATOR = "Secp256k1_HashToCurve_Cashu_"u8.ToArray();
    
    
    


    public static ECPubKey MessageToCurve(string message)
    {
        var hash = Encoding.UTF8.GetBytes(message);
        return HashToCurve(hash);
    }

    public static ECPubKey HexToCurve(string hex)
    {
        var bytes = Convert.FromHexString(hex);
        return HashToCurve(bytes);
    }
    public static ECPubKey HashToCurve(byte[] x)
    {
        using SHA256 sha256 = SHA256.Create();
        var msg_hash = sha256.ComputeHash(Concat(DOMAIN_SEPARATOR, x));
        for (uint counter = 0;; counter++)
        {
            var counterBytes = BitConverter.GetBytes(counter);
            var publicKeyBytes = Concat([0x02], sha256.ComputeHash(Concat(msg_hash, counterBytes)));
            try
            {
                return ECPubKey.Create(publicKeyBytes);
            }
            catch (FormatException)
            {
            }
        }
    }

    public static GE ToGE(this Scalar scalar)
    {
        // Multiply the scalar by the generator point to get the group element
        GEJ gej = Context.Instance.EcMultGenContext.MultGen(scalar);
        return gej.ToGroupElement();
    }

    public static ECPubKey ToPubkey(this Scalar scalar)
    {
        return new ECPubKey(scalar.ToGE(), Context.Instance);
    }

    public static ECPrivKey ToPrivateKey(this Scalar scalar)
    {
        return ECPrivKey.TryCreate(scalar, out var key) ? key : throw new InvalidOperationException();
    }

    public static ECPubKey ToPubkey(this GEJ gej)
    {
        return new ECPubKey(gej.ToGroupElement(), Context.Instance);
    }

    public static ECPubKey ToPubkey(this GE ge)
    {
        return new ECPubKey(ge, Context.Instance);
    }

    public static ECPubKey ComputeB_(ECPubKey Y, ECPrivKey r)
    {
        //B_ = Y + rG
        return Y.Q.ToGroupElementJacobian().Add(r.CreatePubKey().Q).ToPubkey();
    }

    public static ECPubKey ComputeC_(ECPubKey B_, ECPrivKey k)
    {
        //C_ = kB_
        return (B_.Q * k.sec).ToPubkey();
    }
    

    public static (ECPrivKey e, ECPrivKey s) ComputeProof(ECPubKey B_, ECPrivKey a, ECPrivKey p)
    {
        //C_ - rK = kY + krG - krG = kY = C
        var r1 = p.CreatePubKey();
        var r2 = (B_.Q * p.sec).ToPubkey();
        var C_ = ComputeC_(B_, a);
        var A = a.CreatePubKey();

        using SHA256 sha256 = SHA256.Create();
        var e = sha256.ComputeHash(Concat(r1.ToBytes(), r2.ToBytes(), A.ToBytes(), C_.ToBytes()));
        var s = p.TweakAdd(a.TweakMul(e).ToBytes());
        return (new Scalar(e).ToPrivateKey(), s);
    }

    public static bool VerifyProof(ECPubKey B_, ECPubKey C_, ECPrivKey e, ECPrivKey s, ECPubKey A)
    {
        var r1 = s.CreatePubKey().Q.ToGroupElementJacobian().Add((A.Q * e.sec.Negate()).ToGroupElement()).ToPubkey();
        var r2 = (B_.Q * s.sec).Add((C_.Q * e.sec.Negate()).ToGroupElement()).ToPubkey();
        using SHA256 sha256 = SHA256.Create();
        var e_ = sha256.ComputeHash(Concat(r1.ToBytes(), r2.ToBytes(), A.ToBytes(), C_.ToBytes()));
        return e.sec.Equals(e_);
    }

    public static bool VerifyProof(ECPubKey Y, ECPrivKey r, ECPubKey C, ECPrivKey e, ECPrivKey s, ECPubKey A)
    {
        var C_ = C.Q.ToGroupElementJacobian().Add((A.Q * r.sec).ToGroupElement()).ToPubkey();
        var B_ = Y.Q.ToGroupElementJacobian().Add(r.CreatePubKey().Q).ToPubkey();
        return VerifyProof(B_, C_, e, s, A);
    }

    public static ECPubKey ComputeC(ECPubKey C_, ECPrivKey r, ECPubKey A)
    {
       return C_.Q.ToGroupElementJacobian().Add((A.Q * r.sec).ToGroupElement()).ToPubkey();
    }

    private static byte[] Concat(params byte[][] arrays)
    {
        return arrays.Aggregate((a, b) => a.Concat(b).ToArray());
    }

    public static string ToHex(this ECPrivKey key)
    {
        Span<byte> output = stackalloc byte[32];
        key.WriteToSpan(output);
        return Convert.ToHexString(output);
    }

    public static byte[] ToBytes(this ECPrivKey key)
    {
        Span<byte> output = stackalloc byte[32];
        key.WriteToSpan(output);
        return output.ToArray();
    }
}