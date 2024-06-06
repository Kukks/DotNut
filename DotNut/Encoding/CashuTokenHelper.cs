using System.Text;
using System.Text.Json;

namespace DotNut;

public static class CashuTokenHelper
{
    public const string CashuUriScheme = "cashu";
    public const string CashuPrefix = "cashu";

    public static string Encode(this CashuToken token, string version = "A", bool makeUri = true)
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
        return JsonSerializer.Deserialize<CashuToken>(json)!;
    }
}