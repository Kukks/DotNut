using System.Security.Cryptography;
using NBitcoin.Secp256k1;

namespace DotNut;

public class P2PkBuilder
{
    public DateTimeOffset? Lock { get; set; }
    public ECPubKey[]? RefundPubkeys { get; set; }
    public int SignatureThreshold { get; set; } = 1;

    /// <summary>
    /// Keys of the main pathway. Empty for a NUT-14 hashlock with no <c>pubkeys</c> tag, where
    /// the preimage alone spends the proof.
    /// </summary>
    public ECPubKey[] Pubkeys { get; set; } = [];

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
                tags.Add(new[] { "refund" }.Concat(RefundPubkeys.Select(p => p.ToHex())).ToArray());
                RefundSignatureThreshold ??= 1;
            }
            if (RefundSignatureThreshold is { } refundSignatureThreshold and > 1)
            {
                tags.Add(new[] { "n_sigs_refund", refundSignatureThreshold.ToString() });
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
            Tags = tags.ToArray(),
        };
    }

    /// <summary>
    /// Tags NUT-11 gives meaning to. Each may appear at most once.
    /// </summary>
    private static readonly string[] KnownTags =
    [
        "pubkeys",
        "locktime",
        "refund",
        "n_sigs",
        "n_sigs_refund",
        "sigflag",
    ];

    /// <summary>
    /// Checks the NUT-11 well-formedness rules. A secret that breaks any of them makes the
    /// proof unspendable, so it is rejected rather than interpreted.
    /// </summary>
    /// <returns>What is wrong, or null when the secret is well formed.</returns>
    internal static string? FindMalformation(P2PKProofSecret proofSecret)
    {
        var tags = proofSecret.Tags ?? [];

        foreach (var tag in KnownTags)
        {
            if (tags.Count(t => t.FirstOrDefault() == tag) > 1)
            {
                return $"Tag '{tag}' appears more than once";
            }
        }

        var sigFlag = TagValues(tags, "sigflag").FirstOrDefault();
        if (sigFlag is not null && sigFlag != "SIG_INPUTS" && sigFlag != "SIG_ALL")
        {
            return $"Unknown sigflag '{sigFlag}'";
        }

        string[] mainKeys = [proofSecret.Data, .. TagValues(tags, "pubkeys")];
        var refundKeys = TagValues(tags, "refund").ToArray();

        // A key may appear in both pathways, but not twice within one.
        if (HasDuplicate(mainKeys))
        {
            return "Duplicate pubkey in the main pathway";
        }
        if (HasDuplicate(refundKeys))
        {
            return "Duplicate pubkey in the refund pathway";
        }

        if (FindBadThreshold(tags, "n_sigs", mainKeys.Length) is { } mainError)
        {
            return mainError;
        }
        if (FindBadThreshold(tags, "n_sigs_refund", refundKeys.Length) is { } refundError)
        {
            return refundError;
        }

        return null;
    }

    private static string? FindBadThreshold(string[][] tags, string tag, int keyCount)
    {
        var raw = TagValues(tags, tag).FirstOrDefault();
        if (raw is null)
        {
            return null;
        }

        if (!int.TryParse(raw, out var threshold) || threshold < 1)
        {
            return $"'{tag}' must be a positive integer, got '{raw}'";
        }

        return threshold > keyCount
            ? $"'{tag}' is {threshold} but its pathway has {keyCount} key(s)"
            : null;
    }

    private static IEnumerable<string> TagValues(string[][] tags, string tag) =>
        tags.FirstOrDefault(t => t.FirstOrDefault() == tag)?.Skip(1) ?? [];

    /// <summary>
    /// Compares keys the way NUT-11 does: by lowercase x-coordinate, ignoring the 02/03 parity
    /// prefix, since either prefix is spendable by the same Schnorr secret.
    /// </summary>
    private static bool HasDuplicate(IReadOnlyCollection<string> keys)
    {
        var seen = new HashSet<string>();
        foreach (var key in keys)
        {
            if (!seen.Add(XOnly(key)))
            {
                return true;
            }
        }
        return false;
    }

    private static string XOnly(string pubkeyHex) =>
        pubkeyHex.Length == 66 ? pubkeyHex[2..].ToLowerInvariant() : pubkeyHex.ToLowerInvariant();

    public static P2PkBuilder Load(P2PKProofSecret proofSecret)
    {
        if (FindMalformation(proofSecret) is { } malformation)
        {
            throw new FormatException($"Malformed P2PK secret: {malformation}");
        }

        var builder = new P2PkBuilder();
        var primaryPubkey = proofSecret.Data.ToPubKey();
        var pubkeys = proofSecret.Tags?.FirstOrDefault(strings =>
            strings.FirstOrDefault() == "pubkeys"
        );
        if (pubkeys is not null && pubkeys.Length > 1)
        {
            builder.Pubkeys = pubkeys
                .Skip(1)
                .Select(s => s.ToPubKey())
                .Prepend(primaryPubkey)
                .ToArray();
        }
        else
        {
            builder.Pubkeys = [primaryPubkey];
        }

        var rawUnixTs = proofSecret
            .Tags?.FirstOrDefault(strings => strings.FirstOrDefault() == "locktime")
            ?.Skip(1)
            ?.FirstOrDefault();
        builder.Lock =
            rawUnixTs is not null && long.TryParse(rawUnixTs, out var unixTs)
                ? DateTimeOffset.FromUnixTimeSeconds(unixTs)
                : null;

        var refund = proofSecret.Tags?.FirstOrDefault(strings =>
            strings.FirstOrDefault() == "refund"
        );
        if (refund is not null && refund.Length > 1)
        {
            builder.RefundPubkeys = refund.Skip(1).Select(s => s.ToPubKey()).ToArray();
        }

        var nSigsRefund = proofSecret
            .Tags?.FirstOrDefault(strings => strings.FirstOrDefault() == "n_sigs_refund")
            ?.Skip(1)
            ?.FirstOrDefault();
        if (
            !string.IsNullOrEmpty(nSigsRefund)
            && int.TryParse(nSigsRefund, out var nSigsRefundValue)
        )
        {
            builder.RefundSignatureThreshold = nSigsRefundValue;
        }

        var sigFlag = proofSecret
            .Tags?.FirstOrDefault(strings => strings.FirstOrDefault() == "sigflag")
            ?.Skip(1)
            ?.FirstOrDefault();
        if (!string.IsNullOrEmpty(sigFlag))
        {
            builder.SigFlag = sigFlag;
        }

        var nSigs = proofSecret
            .Tags?.FirstOrDefault(strings => strings.FirstOrDefault() == "n_sigs")
            ?.Skip(1)
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
        if (
            this.RefundSignatureThreshold is not null
            && (RefundPubkeys is null || RefundPubkeys.Length < RefundSignatureThreshold)
        )
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
    public P2PKProofSecret BuildBlinded(out ECPubKey p2pkE)
    {
        var e = new PrivKey(RandomNumberGenerator.GetHexString(64));
        p2pkE = e.Key.CreatePubKey();
        return BuildBlinded(e);
    }

    //For sig_all, p2pk_e must be provided
    public P2PKProofSecret BuildBlinded(ECPrivKey p2pke)
    {
        var pubkeys = RefundPubkeys != null ? Pubkeys.Concat(RefundPubkeys).ToArray() : Pubkeys;
        var rs = new List<ECPrivKey>();
        for (int i = 0; i < pubkeys.Length; i++)
        {
            var Zx = Cashu.ComputeZx(p2pke, pubkeys[i]);
            var Ri = Cashu.ComputeRi(Zx, i);
            rs.Add(Ri);
        }
        BlindPubkeys(rs.ToArray());
        return Build();
    }

    protected void BlindPubkeys(ECPrivKey[] rs)
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
                RefundPubkeys[i - Pubkeys.Length] = Cashu.ComputeB_(
                    RefundPubkeys[i - Pubkeys.Length],
                    rs[i]
                );
            }
        }
    }

    public virtual P2PkBuilder Clone()
    {
        return new P2PkBuilder()
        {
            Lock = Lock,
            RefundPubkeys = RefundPubkeys?.ToArray(),
            SignatureThreshold = SignatureThreshold,
            RefundSignatureThreshold = RefundSignatureThreshold,
            Pubkeys = Pubkeys.ToArray(),
            SigFlag = SigFlag,
            Nonce = Nonce,
        };
    }
}
