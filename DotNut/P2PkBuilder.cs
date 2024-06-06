using System.Security.Cryptography;
using NBitcoin.Secp256k1;

namespace DotNut;

public class P2PkBuilder
{
    public DateTimeOffset? Lock { get; set; }
    public ECPubKey[]? RefundPubkeys { get; set; }
    public int SignatureThreshold { get; set; } = 1;
    public ECPubKey[] Pubkeys { get; set; }
    //SIG_INPUTS, SIG_ALL 
    public string? SigFlag { get; set; }

    public string? Nonce { get; set; }

    public P2PKProofSecret Build()
    {
        var tags = new List<string[]>();
        if(Pubkeys.Length > 1)
        {
            tags.Add(new[] {"pubkeys"}.Concat(Pubkeys.Skip(1).Select(p => p.ToHex())).ToArray());
        }
        if (!string.IsNullOrEmpty(SigFlag))
        {
            tags.Add(new[] {"sigflag", SigFlag});
        }

        if (Lock.HasValue)
        {
            tags.Add(new[] {"locktime", Lock.Value.ToUnixTimeSeconds().ToString()});
            if (RefundPubkeys?.Any() is true)
            {
                tags.Add(new[] {"refund"}.Concat(RefundPubkeys.Select(p => p.ToHex()))
                    .ToArray());
            }
        }

        if (SignatureThreshold > 1 && Pubkeys.Length >= SignatureThreshold)
        {
            tags.Add(new[] {"n_sigs", SignatureThreshold.ToString()});
        }
       

        return new P2PKProofSecret()
        {
            Data = Pubkeys.First().ToHex(),
            Nonce = Nonce?? RandomNumberGenerator.GetHexString(32, true),
            Tags = tags.ToArray()
        };
    }

    public static P2PkBuilder Load(P2PKProofSecret proofSecret)
    {
        var builder = new P2PkBuilder();
        var primaryPubkey = proofSecret.Data.ToPubKey();
        var pubkeys = proofSecret.Tags?.FirstOrDefault(strings => strings.FirstOrDefault() == "pubkeys");
        if (pubkeys is not null && pubkeys.Length > 1)
        {
            builder.Pubkeys = pubkeys.Skip(1).Select(s => s.ToPubKey()).Prepend(primaryPubkey).ToArray();
        }
        else
        {
            builder.Pubkeys = [primaryPubkey];
        }
        var rawUnixTs = proofSecret.Tags?.FirstOrDefault(strings => strings.FirstOrDefault() == "locktime")?.Skip(1)
            ?.FirstOrDefault();
        builder.Lock = rawUnixTs is not null && long.TryParse(rawUnixTs, out var unixTs)
            ? DateTimeOffset.FromUnixTimeSeconds(unixTs)
            : null;

        var refund = proofSecret.Tags?.FirstOrDefault(strings => strings.FirstOrDefault() == "refund");
        if (refund is not null && refund.Length > 1)
        {
            builder.RefundPubkeys = refund.Skip(1).Select(s => s.ToPubKey()).ToArray();
        }

        var sigFlag = proofSecret.Tags?.FirstOrDefault(strings => strings.FirstOrDefault() == "sigflag")?.Skip(1)
            ?.FirstOrDefault();
        if (!string.IsNullOrEmpty(sigFlag))
        {
            builder.SigFlag = sigFlag;
        }

        var nSigs = proofSecret.Tags?.FirstOrDefault(strings => strings.FirstOrDefault() == "n_sigs")?.Skip(1)
            ?.FirstOrDefault();
        if (!string.IsNullOrEmpty(nSigs) && int.TryParse(nSigs, out var nSigsValue))
        {
            builder.SignatureThreshold = nSigsValue;
        }
        builder.Nonce = proofSecret.Nonce;

        return builder;
    }
}