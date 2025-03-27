using System.Text;
using NBitcoin.Secp256k1;
using SHA256 = System.Security.Cryptography.SHA256;

namespace DotNut;

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
        var msgHash = SHA256.HashData(Concat(DOMAIN_SEPARATOR, x));
        for (uint counter = 0;; counter++)
        {
            var counterBytes = BitConverter.GetBytes(counter);
            var publicKeyBytes = Concat([0x02], SHA256.HashData(Concat(msgHash, counterBytes)));
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

    public static ECPubKey UnblindC(ECPubKey C_, ECPrivKey r, ECPubKey A)
    {
        var rA = A.Q * r.sec; // r * A
        var negatedRA = rA.Negate(); // -r * A
        return C_.Q.ToGroupElementJacobian().Add((negatedRA).ToGroupElement()).ToPubkey(); // C_ - r * A
    }


    public static (ECPrivKey e, ECPrivKey s) ComputeProof(ECPubKey B_, ECPrivKey a, ECPrivKey p)
    {
        //C_ - rK = kY + krG - krG = kY = C
        var r1 = p.CreatePubKey();
        var r2 = (B_.Q * p.sec).ToPubkey();
        var C_ = ComputeC_(B_, a);
        var A = a.CreatePubKey();

        var e = ComputeE(r1, r2, A, C_);
        var s = p.TweakAdd(a.TweakMul(e.ToBytes()).ToBytes());
        return (e.ToPrivateKey(), s);
    }
    
    public static Scalar ComputeE(ECPubKey R1, ECPubKey R2, ECPubKey K, ECPubKey C_)
    {
        byte[] eBytes = Encoding.UTF8.GetBytes(string.Concat(new[] {R1, R2, K, C_}.Select(pk => pk.ToHex(false))));
        return new Scalar(SHA256.HashData(eBytes));
    }

    public static bool Verify(this Proof proof, ECPubKey A)
    {
        return VerifyProof(proof.Secret.ToCurve(),proof.DLEQ.R, proof.C, proof.DLEQ.E, proof.DLEQ.S, A);
    }
    public static bool Verify(this BlindSignature blindSig, ECPubKey A, ECPubKey B_)
    {
        return  Cashu.VerifyProof(B_, blindSig.C_, blindSig.DLEQ.E, blindSig.DLEQ.S, A);
    }
    
    public static bool VerifyProof(ECPubKey B_, ECPubKey C_, ECPrivKey e, ECPrivKey s, ECPubKey A)
    {

        var r1 = s.CreatePubKey().Q.ToGroupElementJacobian().Add((A.Q * e.sec.Negate()).ToGroupElement()).ToPubkey();
        var r2 = (B_.Q * s.sec).Add((C_.Q * e.sec.Negate()).ToGroupElement()).ToPubkey();
        var e_ = ComputeE(r1, r2, A, C_);
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
        return Convert.ToHexString(key.ToBytes()).ToLower();
    }

    public static byte[] ToBytes(this ECPrivKey key)
    {
        Span<byte> output = stackalloc byte[32];
        key.WriteToSpan(output);
        return output.ToArray();
    }

    public static byte[] ToUncompressedBytes(this ECPubKey key)
    {
        Span<byte> output = stackalloc byte[65];
        key.WriteToSpan(false, output,  out _);
        return output.ToArray();
    }
    public static string ToHex(this ECPubKey key, bool compressed = true)
    {
        return compressed ? Convert.ToHexString(key.ToBytes(true)).ToLower() : Convert.ToHexString(key.ToUncompressedBytes()).ToLower();
    }
    public static string ToHex(this Scalar scalar)
    {
        return Convert.ToHexString(scalar.ToBytes()).ToLower();
    }
    public static string ToHex(this SecpSchnorrSignature sig)
    {
        return Convert.ToHexString(sig.ToBytes()).ToLower();
    }
}