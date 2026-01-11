using System.Text;
using SHA256 = System.Security.Cryptography.SHA256;

namespace DotNut;

public static class MintQuoteSigner
{
    public static string SignMintQuote(
        this PrivKey pk,
        string quote,
        List<BlindedMessage> blindedMessages
    )
    {
        var sb = new StringBuilder();
        sb.Append(quote);
        foreach (var blindedMessage in blindedMessages)
        {
            sb.Append(blindedMessage.B_);
        }
        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var hash = SHA256.HashData(bytes);
        return pk.Key.SignBIP340(hash).ToHex();
    }
}
