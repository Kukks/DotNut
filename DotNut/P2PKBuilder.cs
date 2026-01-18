using System.Security.Cryptography;
using NBitcoin.Secp256k1;

namespace DotNut;

public class P2PKBuilder
{
    public DateTimeOffset? Lock { get; set; }
    public ECPubKey[]? RefundPubkeys { get; set; }
    public int SignatureThreshold { get; set; } = 1;

    public ECPubKey[] Pubkeys { get; set; }

    //SIG_INPUTS, SIG_ALL 
    public string? SigFlag { get; set; }
    public string? Nonce { get; set; }
    public int? RefundSignatureThreshold { get; set; }
    
    public P2PKProofSecret Build()
    {
        Validate();
        var tags = new List<string[]>();
        if (Pubkeys.Length > 1)
        {
            tags.Add(new[] { "pubkeys" }.Concat(Pubkeys.Skip(1).Select(p => p.ToHex())).ToArray());
        }

        if (!string.IsNullOrEmpty(SigFlag))
        {
            tags.Add(new[] { "sigflag", SigFlag });
        }

        if (Lock.HasValue)
        {
            tags.Add(new[] { "locktime", Lock.Value.ToUnixTimeSeconds().ToString() });
            if (RefundPubkeys?.Any() is true)
            {
                tags.Add(new[] { "refund" }.Concat(RefundPubkeys.Select(p => p.ToHex()))
                    .ToArray());
                RefundSignatureThreshold ??= 1;

            }
            if (RefundSignatureThreshold is { } refundSignatureThreshold and > 1)
            {
                tags.Add(new[] {"n_sigs_refund", refundSignatureThreshold.ToString() });
            }
        }

        if (SignatureThreshold > 1 && Pubkeys.Length >= SignatureThreshold)
        {
            tags.Add(new[] { "n_sigs", SignatureThreshold.ToString() });
        }
        
        return new P2PKProofSecret()
        {
            Data = Pubkeys.First().ToHex(),
            Nonce = Nonce ?? RandomNumberGenerator.GetHexString(32, true),
            Tags = tags.ToArray()
        };
    }

    public static P2PKBuilder Load(P2PKProofSecret proofSecret)
    {
        var builder = new P2PKBuilder();
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
        
        var nSigsRefund = proofSecret.Tags?.FirstOrDefault(strings => strings.FirstOrDefault() == "n_sigs_refund")?
            .Skip(1)?.FirstOrDefault();
        if (!string.IsNullOrEmpty(nSigsRefund) && int.TryParse(nSigsRefund, out var nSigsRefundValue))
        {
            builder.RefundSignatureThreshold = nSigsRefundValue;
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
    
    private void Validate()
    {
        if (this.Pubkeys.Count() < SignatureThreshold)
        {
            throw new ArgumentException("Signature threshold bigger than provided pubkeys count!");
        }
        if(this.RefundSignatureThreshold is not null 
           && (RefundPubkeys is null || RefundPubkeys.Length < RefundSignatureThreshold))
        {
            throw new ArgumentException("Signature threshold bigger than provided pubkeys count!");
        }
    }
    
    
    /*
     * =========================
     * NUT-XX Pay to blinded key
     * =========================
     */
    
    //For sig_inputs, generates random p2pk_e for each input
    public P2PKProofSecret BuildBlinded(KeysetId keysetId, out ECPubKey p2pkE)
    {
        var e = new PrivKey(RandomNumberGenerator.GetHexString(64));
        p2pkE = e.Key.CreatePubKey();
        return BuildBlinded(keysetId, e);
    }

    //For sig_all, p2pk_e must be provided
    public P2PKProofSecret BuildBlinded(KeysetId keysetId, ECPrivKey p2pke)
    {
        var pubkeys = RefundPubkeys != null ? Pubkeys.Concat(RefundPubkeys).ToArray() : Pubkeys;
        var rs = new List<ECPrivKey>();
        bool extraByte = false;
        
        var keysetIdBytes = keysetId.GetBytes();

        var e = p2pke;
        
        for (int i = 0; i < pubkeys.Length; i++)
        {
            var Zx = Cashu.ComputeZx(e, pubkeys[i]);
            var Ri = Cashu.ComputeRi(Zx, keysetIdBytes, i);
            rs.Add(Ri);
        }
        _blindPubkeys(rs.ToArray());
        return Build();
    }
    
    private void _blindPubkeys(ECPrivKey[] rs)
    {
        var expectedLength = Pubkeys.Length + (RefundPubkeys?.Length ?? 0);
        if (expectedLength != rs.Length)
        {
            throw new ArgumentException("Invalid P2Pk blinding factors length");
        }

        for (var i = 0; i < rs.Length; i++)
        {
            if (i < Pubkeys.Length)
            {
                Pubkeys[i] = Cashu.ComputeB_(Pubkeys[i], rs[i]);
                continue;
            }

            if (RefundPubkeys != null)
            {
                RefundPubkeys[i - Pubkeys.Length] = Cashu.ComputeB_(RefundPubkeys[i - Pubkeys.Length], rs[i]);
            }
        }
    }
}