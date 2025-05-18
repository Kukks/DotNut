namespace DotNut;

public static class CashuTokenHelper
{
    public static Dictionary<string, ICashuTokenEncoder> Encoders { get; } = new();

    static CashuTokenHelper()
    {
        Encoders.Add("A", new CashuTokenV3Encoder());
        Encoders.Add("B", new CashuTokenV4Encoder());
    }

    public const string CashuUriScheme = "cashu:";
    public const string CashuPrefix = "cashu";

    public static string Encode(this CashuToken token, string version = "B", bool makeUri = false)
    {
        if (!Encoders.TryGetValue(version, out var encoder))
        {
            throw new NotSupportedException($"Version {version} is not supported");
        }

        //trim trailing slash from mint url
        foreach (var token2 in token.Tokens.Where(token1 => token1.Mint.EndsWith("/")))
        {
            token2.Mint = token2.Mint.TrimEnd('/');
        }
        
        var encoded = encoder.Encode(token);

        var result = $"{CashuPrefix}{version}{encoded}";

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
            token = token.Replace(CashuUriScheme, "");
        }

        if (!token.StartsWith(CashuPrefix))
        {
            throw new FormatException("Invalid cashu token");
        }

        token = token.Substring(CashuPrefix.Length);
        version = token[0].ToString();

        if (!Encoders.TryGetValue(version, out var encoder))
        {
            throw new NotSupportedException($"Version {version} is not supported");
        }

        token = token.Substring(1);
        return encoder.Decode(token);
    }
}