using System.Text;
using System.Text.Json;

namespace DotNut;

public class CashuTokenV3Encoder : ICashuTokenEncoder
{
    public string Encode(CashuToken token)
    {
        var json = JsonSerializer.Serialize(token);
        return Base64UrlSafe.Encode(Encoding.UTF8.GetBytes(json));
    }

    public CashuToken Decode(string token)
    {
        var json = Encoding.UTF8.GetString(Base64UrlSafe.Decode(token));
        return JsonSerializer.Deserialize<CashuToken>(json)!;
    }
}
