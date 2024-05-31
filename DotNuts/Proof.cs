using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DotNuts;
using NBitcoin.Secp256k1;
using SHA256 = System.Security.Cryptography.SHA256;

public class Proof
{
    [JsonPropertyName("amount")] public int Amount { get; set; }

    [JsonConverter(typeof(KeysetIdJsonConverter))]
    [JsonPropertyName("id")]
    public KeysetId Id { get; set; }

    [JsonPropertyName("secret")] public string Secret { get; set; }

    [JsonPropertyName("C")] public PubKey C { get; set; }
    [JsonPropertyName("witness")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? Witness { get; set; }
}


public static class P2PK
{
    
    public class P2PkBuilder
    {
        public DateTimeOffset? Lock { get; set; }
        public ECPubKey[]? RefundPubkeys { get; set; }
        public int SignatureThreshold { get; set; } = 1;
        public ECPubKey[] Pubkeys  { get; set; }
        public string? SigFlag { get; set; }

        public ProofSecret Build()
        {
            var tags = new List<string[]>();
            if (!string.IsNullOrEmpty(SigFlag))
            {
                tags.Add(new []{"sigflag",SigFlag});
            }
            if(Lock.HasValue)
            {
                tags.Add(new []{"locktime",Lock.Value.ToUnixTimeSeconds().ToString()});
                if (RefundPubkeys?.Any() is true)
                {
                    tags.Add(new []{"refund"}.Concat(RefundPubkeys.Select(p => Convert.ToHexString(p.ToBytes()).ToLower())).ToArray());
                }
            }
            if(SignatureThreshold > 1 && Pubkeys.Length >= SignatureThreshold )
            {
                tags.Add(new []{"n_sigs",SignatureThreshold.ToString()});
            }
            
            return new ProofSecret()
            {
                Data = Convert.ToHexString(Pubkeys.First().ToBytes()).ToLower(),
                Nonce = RandomNumberGenerator.GetHexString(32,true),
                Tags = tags.ToArray()
            };
        }
    }
    
    
    public static bool WitnessValid(this Proof proof)
    {
        //check if the secret is an array of json
        try
        {
            var secretSet = JsonSerializer.Deserialize<ProofSecretSet>(proof.Secret);
            if (secretSet is null)
            {
                return true;
            }


            if (!secretSet.TryGetValue("P2PK", out var proofSecret))
            {
                return true;
            }

            if (proofSecret.)
        }
        catch (Exception e)
        {
            return true;
        }
    }


    public static ECPubKey[] ExtractValidPubkeys(this ProofSecret proof)
    {
        //check locktime 
        var locktime = proof.Tags?.FirstOrDefault(strings => strings.FirstOrDefault() == "locktime")?.Skip(1)
            ?.FirstOrDefault();
        if (!string.IsNullOrEmpty(locktime) && long.TryParse(locktime, out var locktimeValue) &&
            locktimeValue < DateTimeOffset.Now.ToUnixTimeSeconds())
        {
            return ExtractRefundPubkeys(proof);
        }

        var primary = proof.Data.ToPubKey();
        var extraPubKeys = proof.Tags?.FirstOrDefault(strings => strings.FirstOrDefault() == "pubkeys");

        if (extraPubKeys is not null && extraPubKeys.Length > 1)
        {
            return extraPubKeys.Skip(1).Select(s => s.ToPubKey()).Prepend(primary).ToArray();
        }

        return new[] {primary};
    }

    public static ECPubKey[] ExtractRefundPubkeys(this ProofSecret proof)
    {
        var extraPubKeys = proof.Tags?.FirstOrDefault(strings => strings.FirstOrDefault() == "refund");

        if (extraPubKeys is not null && extraPubKeys.Length > 1)
        {
            return extraPubKeys.Skip(1).Select(s => s.ToPubKey()).ToArray();
        }

        return Array.Empty<ECPubKey>();
    }

    public static string GenerateWitness(this ProofSecret proof, ECPrivKey key)
    {
        var toSign = JsonSerializer.Serialize(proof);
        using SHA256 sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(toSign));
        if (proof.Tags?.Any() is true)
        {
            foreach (var tag in proof.Tags)
            {
                if (tag.Length < 1)
                    continue;
                var cmd = tag.First();
                switch (cmd)
                {
                    case "sigflag":
                        if (tag.Length < 2)
                            continue;
                        var sigHashFlag = tag[1];
                }
            }
        }

        var signature = key.SignBIP340(hash);

        return JsonSerializer.Serialize(new P2PKWitness {Signatures = [Convert.ToHexString(signature.ToBytes())]});
    }

    public class P2PKWitness
    {
        [JsonPropertyName("signatures")] public string[] Signatures { get; set; }
    }
}