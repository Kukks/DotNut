using System.Text;
using DotNut.ApiModels;
using NBitcoin.Secp256k1;
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
        var msg = GetMessageToSign(quote, blindedMessages);
        var hash = SHA256.HashData(msg);
        return pk.Key.SignBIP340(hash).ToHex();
    }

    internal static byte[] GetMessageToSign(string quote, IEnumerable<BlindedMessage> messages)
    {
        var sb = new StringBuilder();
        sb.Append(quote);
        var msgs = messages as IReadOnlyList<BlindedMessage> ?? messages.ToList();
        foreach (var blindedMessage in msgs)
        {
            sb.Append(blindedMessage.B_);
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public static bool VerifySignature(this PostMintRequest quote, PubKey pk)
    {
        ArgumentNullException.ThrowIfNull(quote.Signature,  nameof(quote.Signature));
        var msg = GetMessageToSign(quote.Quote, quote.Outputs);
        var hash = SHA256.HashData(msg);
        var xonly = pk.Key.ToXOnlyPubKey();
        if (!SecpSchnorrSignature.TryCreate(
                Convert.FromHexString(quote.Signature), out var sig)
            )
        {
            return false;
        }
        return xonly.SigVerifyBIP340(sig, hash);
    }
}
