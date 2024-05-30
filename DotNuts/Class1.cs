using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using DotNuts;
using NBitcoin.Secp256k1;
using SHA256 = System.Security.Cryptography.SHA256;

public class BlindedMessage
{
    [JsonPropertyName("amount")] public int Amount { get; set; }
    [JsonPropertyName("id")] public string Id { get; set; }
    [JsonPropertyName("B_")] public string B_ { get; set; }
}

public class BlindSignature
{
    [JsonPropertyName("amount")] public int Amount { get; set; }
    [JsonPropertyName("id")] public string Id { get; set; }
    [JsonPropertyName("C_")] public string C_ { get; set; }
}

public class Proof
{
    [JsonPropertyName("amount")] public int Amount { get; set; }
    [JsonPropertyName("id")] public string Id { get; set; }
    [JsonPropertyName("secret")] public string Secret { get; set; }
    [JsonPropertyName("C")] public string C { get; set; }
}


public class CashuProtocolError
{
    [JsonPropertyName("detail")] public string Detail { get; set; }
    [JsonPropertyName("code")] public int Code { get; set; }
}

public class CashuProtocolException : Exception
{
    public CashuProtocolException(CashuProtocolError error) : base(error.Detail)
    {
        Error = error;
    }

    public CashuProtocolError Error { get; }
}

public static class Base64UrlSafe
{
    static readonly char[] padding = {'='};

    //(base64 encoding with / replaced by _ and + by -)
    public static string Encode(byte[] data)
    {
        return System.Convert.ToBase64String(data)
            .TrimEnd(padding).Replace('+', '-').Replace('/', '_');
    }

    public static byte[] Decode(string base64)
    {
        string incoming = base64.Replace('_', '/').Replace('-', '+');
        switch (base64.Length % 4)
        {
            case 2:
                incoming += "==";
                break;
            case 3:
                incoming += "=";
                break;
        }

        return System.Convert.FromBase64String(incoming);
    }
}

public static class CashuTokenHelper
{
    public const string CashuUriScheme = "cashu";
    public const string CashuPrefix = "cashu";

    public static string Encode(CashuToken token, string version = "A", bool makeUri = true)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(token);
        var result = CashuPrefix + version + Base64UrlSafe.Encode(Encoding.UTF8.GetBytes(json));
        if (makeUri)
        {
            return CashuUriScheme + result;
        }

        return result;
    }

    public static CashuToken Decode(string token, out string? version)
    {
        version = null;
        if (Uri.IsWellFormedUriString(token, UriKind.Absolute))
        {
            token = token.Replace(CashuUriScheme + ":", "");
        }

        if (!token.StartsWith(CashuPrefix))
        {
            throw new FormatException("Invalid cashu token");
        }

        token = token.Substring(CashuPrefix.Length);
        version = token[0].ToString();
        token = token.Substring(1);

        var json = Encoding.UTF8.GetString(Base64UrlSafe.Decode(token));
        return System.Text.Json.JsonSerializer.Deserialize<CashuToken>(json);
    }
}


public class CashuToken
{
    public class Token
    {
        [JsonPropertyName("mint")] public string Mint { get; set; }
        [JsonPropertyName("proofs")] public List<Proof> Proofs { get; set; }
    }

    [JsonPropertyName("token")] public List<Token> Tokens { get; set; }

    [JsonPropertyName("unit")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Unit { get; set; }

    [JsonPropertyName("memo")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Memo { get; set; }
}


public static class Nut00
{
    private static readonly byte[] DOMAIN_SEPARATOR = "Secp256k1_HashToCurve_Cashu_"u8.ToArray();
    
        public static string GetKeysetId(this Dictionary<int, ECPubKey> keyset, byte version = 0x00)
        {
            //     1 - sort public keys by their amount in ascending order
            // 2 - concatenate all public keys to one byte array
            // 3 - HASH_SHA256 the concatenated public keys
            // 4 - take the first 14 characters of the hex-encoded hash
            // 5 - prefix it with a keyset ID version byte

            var preimage =  keyset.OrderBy(x => x.Key).Select(pair => pair.Value.ToBytes()).Aggregate((a, b) => a.Concat(b).ToArray());
            using SHA256 sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(preimage);
            return version + Convert.ToHexString(hash).Substring(0, 14).ToLower();
        }
    

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

    public static ECPubKey ToPubkey(this GEJ gej)
    {
        return new ECPubKey(gej.ToGroupElement(), Context.Instance);
    }

    public static ECPubKey ToPubkey(this GE ge)
    {
        return new ECPubKey(ge, Context.Instance);
    }

    public static ECPubKey ComputeB(ECPubKey Y, ECPrivKey r)
    {
        // Create the public key from the private key r (r * G)
        ECPubKey rG = r.CreatePubKey();
        GE rGPoint = rG.Q;

        // Add the points Y and rG using Jacobian coordinates
        GEJ B_Point = Y.Q.ToGroupElementJacobian().Add(rGPoint);
        return B_Point.ToPubkey();
    }

    public static ECPubKey ComputeC_(ECPubKey B_, ECPrivKey k)
    {
        //C_ = kB_
        return (B_.Q * k.sec).ToPubkey();
    }

    public static DLEQProof ComputeProof(ECPubKey B_, ECPrivKey a, ECPrivKey p)
    {
        var r1 = p.CreatePubKey();
        var r2 = (B_.Q * p.sec).ToPubkey();
        var C_ = ComputeC_(B_, a);
        var A = a.CreatePubKey();

        using SHA256 sha256 = SHA256.Create();
        var e = sha256.ComputeHash(Concat(r1.ToBytes(), r2.ToBytes(), A.ToBytes(), C_.ToBytes()));
        var s = p.TweakAdd(a.TweakMul(e).ToBytes());
        return new DLEQProof(s.sec, new Scalar(e));
    }

    public static bool VerifyProof(ECPubKey B_, ECPubKey C_, ECPrivKey e, ECPrivKey s, ECPubKey A)
    {
        var r1 = s.CreatePubKey().Q.ToGroupElementJacobian().Add((A.Q * e.sec.Negate()).ToGroupElement()).ToPubkey();
        var r2 = (B_.Q * s.sec).Add((C_.Q * e.sec.Negate()).ToGroupElement()).ToPubkey();
        using SHA256 sha256 = SHA256.Create();
        var e_ = sha256.ComputeHash(Concat(r1.ToBytes(), r2.ToBytes(), A.ToBytes(), C_.ToBytes()));
        return e.sec.Equals(e_);
    }

    public static bool VerifyProof(string message, ECPrivKey r, ECPubKey C, ECPrivKey e, ECPrivKey s, ECPubKey A)
    {
        var Y = MessageToCurve(message);
        var C_ = C.Q.ToGroupElementJacobian().Add((A.Q * r.sec).ToGroupElement()).ToPubkey();
        var B_ = Y.Q.ToGroupElementJacobian().Add(r.CreatePubKey().Q).ToPubkey();
        return VerifyProof(B_, C_, e, s, A);
    }

    
    
    public static ECPubKey ComputeC(ECPubKey C_Prime, ECPubKey rK)
    {
        // Get the underlying points of C' and rK
        GE C_PrimePoint = C_Prime.Q;
        GE rKPoint = rK.Q;

        // Subtract rK from C' using Jacobian coordinates
        GEJ C_PointJ = C_PrimePoint.ToGroupElementJacobian().Add(rKPoint.Negate());
        GE C_Point = C_PointJ.ToGroupElement();

        // Normalize the point and create a new public key
        C_Point = C_Point.NormalizeYVariable();
        ECPubKey C_ = new ECPubKey(C_Point, Context.Instance);

        return C_;
    }

    public static ECPubKey ComputeCPrime(ECPubKey B_, Scalar k)
    {
        // Get the underlying point of B'
        GE B_Point = B_.Q;

        // Multiply the point B' by the scalar k
        GEJ C_PrimePointJ = B_Point * k;
        GE C_PrimePoint = C_PrimePointJ.ToGroupElement();

        // Normalize the point and create a new public key
        C_PrimePoint = C_PrimePoint.NormalizeYVariable();
        ECPubKey C_Prime = new ECPubKey(C_PrimePoint, Context.Instance);

        return C_Prime;
    }

    public static ECPubKey ComputeRK(ECPubKey K, Scalar r)
    {
        // Get the underlying point of K
        GE KPoint = K.Q;

        // Multiply the point K by the scalar r
        GEJ rKPointJ = KPoint * r;
        GE rKPoint = rKPointJ.ToGroupElement();

        // Normalize the point and create a new public key
        rKPoint = rKPoint.NormalizeYVariable();
        ECPubKey rK = new ECPubKey(rKPoint, Context.Instance);

        return rK;
    }
    
    public static void GenerateProofsFromBlindSignatures(this IEnumerable<BlindSignature> signatures, GetKeysResponse.Keyset keyset)
    {
        foreach (var blindSignature in signatures)
        {
        }
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