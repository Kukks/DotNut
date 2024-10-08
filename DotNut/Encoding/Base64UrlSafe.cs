namespace DotNut;

public static class Base64UrlSafe
{
    static readonly char[] padding = {'='};

    //(base64 encoding with / replaced by _ and + by -)
    public static string Encode(byte[] data)
    {
        return System.Convert.ToBase64String(data)
            .TrimEnd(padding).Replace('+', '-').Replace('/', '_').TrimEnd(padding);
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