using System.Buffers.Binary;
using System.Security.Cryptography;
using DotNut.BLS12_381;
using DotNut.BLS12_381.Curve.G1;
using DotNut.BLS12_381.Curve.G2;
using DotNut.BLS12_381.HashToCurve;
using DotNut.BLS12_381.Pairing;

namespace DotNut.Crypto;

public static class BlsCashu
{
    public static readonly byte[] HashToCurveDst =
        "CASHU_BLS12_381_G1_XMD:SHA-256_SSWU_RO_"u8.ToArray();

    public static readonly G2Affine G2Generator = G2Affine.Generator;

    private static readonly byte[] BatchDst = "Cashu_BLS_Batch_v1"u8.ToArray();

    public static G1Affine HashToCurveG1(byte[] message) =>
        HashToCurve.HashToG1(message, HashToCurveDst);

    /// <summary>
    /// Multiplicative blinding for v3 keysets: B_ = Y · r.
    /// </summary>
    public static G1Affine BlindMessage(byte[] secret, byte[] r)
    {
        var Y = HashToCurveG1(secret);
        return (Y.ToProjective() * Scalar.FromBytesBigEndian(r)).ToAffine();
    }

    /// <summary>
    /// Wallet-side unblinding: C = C_ · r⁻¹.
    /// </summary>
    public static G1Affine UnblindSignature(G1Affine C_, byte[] r)
    {
        var rScalar = Scalar.FromBytesBigEndian(r);
        if (Scalar.IsZero(rScalar))
            throw new ArgumentException("Blinding factor reduces to zero mod Fr");
        var rInv = Scalar.Invert(rScalar);
        return (C_.ToProjective() * rInv).ToAffine();
    }

    /// <summary>
    /// Mint-side blind signing: C_ = B_ · a.
    /// </summary>
    public static G1Affine CreateBlindSignature(G1Affine B_, byte[] privateKey)
    {
        var a = ScalarFromKeyBytes(privateKey);
        if (Scalar.IsZero(a))
            throw new ArgumentException("Mint scalar must be non-zero");
        return (B_.ToProjective() * a).ToAffine();
    }

    /// <summary>
    /// V3 mint public key: K2 = a · G2_gen (compressed 96 bytes).
    /// </summary>
    public static G2Affine GetG2PubKeyFromPrivKey(byte[] privateKey)
    {
        var a = ScalarFromKeyBytes(privateKey);
        if (Scalar.IsZero(a))
            throw new ArgumentException("Mint scalar must be non-zero");
        return (G2Projective.Generator * a).ToAffine();
    }

    /// <summary>
    /// Wallet-side verification: e(C, G2_gen) == e(Y, K2).
    /// Implemented as e(-C, G2_gen) · e(Y, K2) == 1 via a single multi-pairing.
    /// </summary>
    public static bool VerifySignature(G2Affine K2, G1Affine C, byte[] secret)
    {
        if (C.IsInfinity || K2.IsInfinity) return false;
        var Y = HashToCurveG1(secret);
        var result = Bls12Pairing.MultiMillerLoop([
            (-C, G2Prepared.From(G2Generator)),
            (Y, G2Prepared.From(K2)),
        ]).FinalExponentiation();
        return Gt.Equal(result, Gt.Identity);
    }

    /// <summary>
    /// Batch verify many v3 proofs via a single multi-pairing with Fiat-Shamir weights.
    /// Safe against aggregation attacks: each proof gets an independent random weight.
    /// </summary>
    public static bool BatchVerifySignatures(
        IReadOnlyList<(G2Affine K2, G1Affine C, byte[] Secret)> items)
    {
        if (items.Count == 0)
        {
            return true;
        }
        foreach (var it in items)
        {
            if (it.C.IsInfinity || it.K2.IsInfinity)
            {
                return false;
            }
        }

        var rs = DeriveBatchWeights(items);

        // Left: Σ rᵢ·Cᵢ, then pair against G2
        var sumC = items[0].C.ToProjective() * rs[0];
        for (int i = 1; i < items.Count; i++)
        {
            sumC = sumC + (items[i].C.ToProjective() * rs[i]);
        }

        // Right: group rᵢ·Yᵢ by K2
        var grouped = new Dictionary<string, (G2Affine K2, G1Projective SumY)>();
        for (int i = 0; i < items.Count; i++)
        {
            var Y = HashToCurveG1(items[i].Secret);
            var term = Y.ToProjective() * rs[i];
            var key = Convert.ToHexString(items[i].K2.ToCompressed()).ToLower();
            if (grouped.TryGetValue(key, out var existing))
            {
                grouped[key] = (existing.K2, existing.SumY + term);
            }
            else
            {
                grouped[key] = (items[i].K2, term);
            }
        }

        var pairs = new List<(G1Affine, G2Prepared)>
        {
            (-sumC.ToAffine(), G2Prepared.From(G2Generator))
        };
        foreach (var (_, (k2, sumY)) in grouped)
        {
            pairs.Add((sumY.ToAffine(), G2Prepared.From(k2)));
        }

        var result = Bls12Pairing.MultiMillerLoop(pairs).FinalExponentiation();
        return Gt.Equal(result, Gt.Identity);
    }

    /// <summary>
    /// Derive deterministic batch weights via Fiat-Shamir over the full batch transcript.
    /// </summary>
    internal static Scalar[] DeriveBatchWeights(
        IReadOnlyList<(G2Affine K2, G1Affine C, byte[] Secret)> items)
    {
        // Transcript: BatchDst || (C48 || K296 || len32(secret) || secret) per item
        using var ms = new MemoryStream();
        ms.Write(BatchDst);
        Span<byte> lenBuf = stackalloc byte[4];
        foreach (var it in items)
        {
            ms.Write(it.C.ToCompressed());
            ms.Write(it.K2.ToCompressed());
            BinaryPrimitives.WriteInt32BigEndian(lenBuf, it.Secret.Length);
            ms.Write(lenBuf);
            ms.Write(it.Secret);
        }
        var challenge = SHA256.HashData(ms.ToArray());

        var rs = new Scalar[items.Count];
        Span<byte> iBuf = stackalloc byte[4];
        Span<byte> wide = stackalloc byte[64];
        for (int i = 0; i < items.Count; i++)
        {
            BinaryPrimitives.WriteInt32BigEndian(iBuf, i);
            bool found = false;
            for (int ctr = 0; ctr < 256; ctr++)
            {
                var h = SHA256.HashData([..challenge, ..iBuf, (byte)ctr]);
                // Place the 32-byte hash in the lo word of a 64-byte LE buffer for wide reduction.
                wide.Clear();
                for (int b = 0; b < 32; b++)
                {
                    wide[b] = h[31 - b];
                } // big-endian → LE
                var s = Scalar.FromBytesWide(wide);
                if (!Scalar.IsZero(s)) { rs[i] = s; found = true; break; }
            }
            if (!found)
            {
                throw new InvalidOperationException("BLS batch weight derivation failed");
            }
        }
        return rs;
    }

    /// <summary>
    /// Generates a random non-zero BLS12-381 Fr scalar, returned as 32 big-endian bytes.
    /// </summary>
    public static byte[] GenerateRandomScalar()
    {
        Span<byte> buf = stackalloc byte[32];
        while (true)
        {
            RandomNumberGenerator.Fill(buf);
            if (Scalar.TryFromBytesBigEndian(buf, out var s) && !Scalar.IsZero(s))
            {
                var result = new byte[32];
                Scalar.ToBytesBigEndian(s, result);
                return result;
            }
        }
    }

    /// <summary>
    /// Reduces a 32-byte big-endian value mod Fr using wide reduction (safe for HMAC output).
    /// Returns 32 big-endian bytes of the result. Throws if result is zero.
    /// </summary>
    internal static byte[] ReduceHmacToScalarBytes(ReadOnlySpan<byte> hmac32)
    {
        if (hmac32.Length != 32)
        {
            throw new ArgumentException("Expected 32 bytes", nameof(hmac32));
        }
        // FromBytesWide takes 64 LE bytes: lo=bytes[0..32], hi=bytes[32..64].
        // To place hmac in the lo word: reverse to LE then pad hi with zeros.
        Span<byte> wide = stackalloc byte[64];
        for (int i = 0; i < 32; i++)
        {
            wide[i] = hmac32[31 - i]; // big-endian → little-endian in lo word
        }

        var s = Scalar.FromBytesWide(wide);
        if (Scalar.IsZero(s))
        {
            throw new InvalidOperationException("HMAC-derived BLS scalar is zero");
        }
        var result = new byte[32];
        Scalar.ToBytesBigEndian(s, result);
        return result;
    }

    // Converts a big-endian 32-byte private key to a Scalar.
    // Private keys are canonical (< r) by construction, so FromBytesBigEndian is safe.
    private static Scalar ScalarFromKeyBytes(byte[] keyBytes) =>
        Scalar.FromBytesBigEndian(keyBytes);
}
