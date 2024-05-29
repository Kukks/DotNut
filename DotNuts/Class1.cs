using NBitcoin;
using NBitcoin.DataEncoders;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

public class BlindedMessage
{
    [JsonPropertyName("amount")]
    public int Amount { get; set; }
    [JsonPropertyName("id")]
    public string Id { get; set; }
    [JsonPropertyName("B_")]
    public string B_ { get; set; }
}

public class BlindSignature
{
    [JsonPropertyName("amount")]
    public int Amount { get; set; }
    [JsonPropertyName("id")]
    public string Id { get; set; }
    [JsonPropertyName("C_")]
    public string C_ { get; set; }
}

public class Proof
{
    [JsonPropertyName("amount")]
    public int Amount { get; set; }
    [JsonPropertyName("id")]
    public string Id { get; set; }
    [JsonPropertyName("secret")]
    public string Secret { get; set; }
    [JsonPropertyName("C")]
    public string C { get; set; }
}


public class CashuProtocolError
{
    [JsonPropertyName("detail")]
    public string Detail { get; set; }
    [JsonPropertyName("code")]
    public int Code { get; set; }
}

public static class Base64UrlSafe
{
    static readonly char[] padding = { '=' };
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
            case 2: incoming += "=="; break;
            case 3: incoming += "="; break;
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
        if(Uri.IsWellFormedUriString(token, UriKind.Absolute))
        {
            token = token.Replace(CashuUriScheme+":", "");
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
        [JsonPropertyName("mint")]
        public string Mint { get; set; }
        [JsonPropertyName("proofs")]
        public List<Proof> Proofs { get; set; }
    }
    
    // {
    //     "token": [
    //     {
    //         "mint": str,
    //         "proofs": Proofs
    //     },
    //     ...
    //         ],
    //     "unit": str <optional>,
    //     "memo": str <optional>
    // }
    [JsonPropertyName("token")] public List<Token> Tokens { get; set; }

    [JsonPropertyName("unit")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Unit { get; set; }
    [JsonPropertyName("memo")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Memo { get; set; }
    
}


public class BDHKE
{
    private static readonly byte[] DOMAIN_SEPARATOR = "Secp256k1_HashToCurve_Cashu_"u8.ToArray();

    public static PubKey HashToCurve(byte[] x)
    {
        using SHA256 sha256 = SHA256.Create();
        var msg_hash = sha256.ComputeHash(Concat(DOMAIN_SEPARATOR, x));
        for (uint counter = 0; ; counter++)
        {
            byte[] counterBytes = BitConverter.GetBytes(counter);
            byte[] publicKeyBytes = Concat(new byte[] { 0x02 }, sha256.ComputeHash(Concat(msg_hash, counterBytes)));
            try
            {
                return new PubKey(publicKeyBytes);
            }
            catch (FormatException)
            {
                continue;
            }
        }
    }

    private static byte[] Concat(params byte[][] arrays)
    {
        return arrays.Aggregate((a, b) => a.Concat(b).ToArray());
    }
}

