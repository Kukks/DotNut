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
        foreach (var token1 in token.Tokens)
        {
            if (token1.Mint.EndsWith("/"))
            { 
                token1.Mint = token1.Mint.TrimEnd('/');
            }
            foreach (var proof in token1.Proofs)
            {
                proof.Id = MaybeShortId(proof.Id);
            }
        }
        
        
        
        var encoded = encoder.Encode(token);

        var result = $"{CashuPrefix}{version}{encoded}";

        if (makeUri)
        {
            return CashuUriScheme + result;
        }

        return result;
    }

    public static CashuToken Decode(string token, out string? version, List<Keyset>? keysets = null)
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
        var decoded = encoder.Decode(token);
        
        if (keysets is null)
        {
            return decoded;
        }

        foreach (var innerToken in decoded.Tokens)
        {
            innerToken.Proofs = MapShortKeysetIds(innerToken.Proofs, keysets);
        }
        return decoded;
    }
    
    private static KeysetId MaybeShortId(KeysetId id)
    {
        if (id.GetVersion() != 0x01) return id;
        var s = id.ToString(); 
        return s.Length <= 16 ? id : new KeysetId(s.Substring(0, 16));
    }
    private static List<Proof> MapShortKeysetIds(List<Proof> proofs, List<Keyset>? keysets = null)
    {
        if (proofs.Count == 0)
            return proofs;

        if (proofs.All(p => p.Id.GetVersion() != 0x01 || p.Id.ToString().Length != 16))
            return proofs;

        if (keysets is null)
            throw new ArgumentNullException(nameof(keysets),
                "Encountered short keyset IDs but no keysets were provided for mapping.");

        return proofs.Select(proof =>
        {
            if (proof.Id.GetVersion() != 0x01)
                return proof;

            var proofShortId = proof.Id.ToString();
            var match = keysets.FirstOrDefault(ks => ks.GetKeysetId().ToString().StartsWith(proofShortId, StringComparison.OrdinalIgnoreCase));
            
            if (match is null)
                throw new Exception($"Couldn't map short keyset ID {proof.Id} to any known keysets of the current Mint");

            return new Proof
            {
                Amount = proof.Amount,
                Secret = proof.Secret,
                C = proof.C,
                Witness = proof.Witness,
                DLEQ = proof.DLEQ,
                Id = new KeysetId(match.GetKeysetId().ToString())
            };
        }).ToList();
    }
}