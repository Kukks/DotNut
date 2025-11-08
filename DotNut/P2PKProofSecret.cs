using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json.Serialization;
using NBitcoin.Secp256k1;
using SHA256 = System.Security.Cryptography.SHA256;

namespace DotNut;

public class P2PKProofSecret : Nut10ProofSecret
{
    public const string Key = "P2PK";

    [JsonIgnore] P2PkBuilder Builder => P2PkBuilder.Load(this);

    public virtual ECPubKey[] GetAllowedPubkeys(out int requiredSignatures)
    {
        var builder = Builder;
        if (builder.Lock.HasValue && builder.Lock.Value.ToUnixTimeSeconds() < DateTimeOffset.Now.ToUnixTimeSeconds())
        {
            requiredSignatures = Math.Min(builder.RefundPubkeys?.Length ?? 0, 1);
            return builder.RefundPubkeys ?? Array.Empty<ECPubKey>();
        }

        requiredSignatures = builder.SignatureThreshold;
        return builder.Pubkeys;
    }
    

    public virtual P2PKWitness GenerateWitness(Proof proof, ECPrivKey[] keys)
    {
        return GenerateWitness(proof.Secret.GetBytes(), keys);
    }  
    
    public virtual P2PKWitness GenerateWitness(BlindedMessage message, ECPrivKey[] keys)
    {
        return GenerateWitness(message.B_.Key.ToBytes(), keys);
    }
    
    public virtual P2PKWitness GenerateWitness(byte[] msg, ECPrivKey[] keys)
    {
        var hash = SHA256.HashData(msg);
        return GenerateWitness(ECPrivKey.Create(hash), keys);
    }
    
    public virtual P2PKWitness GenerateWitness(ECPrivKey hash, ECPrivKey[] keys)
    {
        var msg = hash.ToBytes();
        //filter out keys that matter
        var allowedKeys = GetAllowedPubkeys(out var requiredSignatures);
        var keysRequiredLeft = requiredSignatures;
        var availableKeysLeft = keys;
        var result = new P2PKWitness();
        while (keysRequiredLeft > 0 && availableKeysLeft.Any())
        {
            var key = availableKeysLeft.First();
            var pubkey = key.CreatePubKey();
            var isAllowed = allowedKeys.Any(p => p == pubkey);
            if (isAllowed)
            {
                var sig = key.SignBIP340(msg);

                
                key.CreateXOnlyPubKey().SigVerifyBIP340(sig, msg);
                result.Signatures = result.Signatures.Append(sig.ToHex()).ToArray();
            }

            availableKeysLeft = availableKeysLeft.Except(new[] {key}).ToArray();
            keysRequiredLeft = requiredSignatures - result.Signatures.Length;
        }

        if (keysRequiredLeft > 0)
            throw new InvalidOperationException("Not enough valid keys to sign");

        return result;
    }

    
    public P2PKWitness GenerateBlindWitness(Proof proof, ECPrivKey[] keys, KeysetId keysetId, ECPubKey P2PkE)
    {
        return GenerateBlindWitness(proof.Secret.GetBytes(), keys, keysetId, P2PkE);
    }  
    
    public P2PKWitness GenerateBlindWitness(BlindedMessage message, ECPrivKey[] keys, KeysetId keysetId, ECPubKey P2PkE)
    {
        return GenerateBlindWitness(message.B_.Key.ToBytes(), keys, keysetId, P2PkE);
    }
    
    public P2PKWitness GenerateBlindWitness(byte[] msg, ECPrivKey[] keys, KeysetId keysetId, ECPubKey P2PkE)
    {
        var hash = SHA256.HashData(msg);
        return GenerateBlindWitness(ECPrivKey.Create(hash), keys, keysetId, P2PkE);
    }
    
    public P2PKWitness GenerateBlindWitness(ECPrivKey hash, ECPrivKey[] keys, KeysetId keysetId, ECPubKey P2PkE)
    {
        if (Key != "P2PK")
        {
            throw new InvalidOperationException("Only P2PK is supported");
        }
        var msg = hash.ToBytes();
        var allowedKeys = GetAllowedPubkeys(out var requiredSignatures);
        var keysRequiredLeft = requiredSignatures;
        var availableKeysLeft = keys;
        var result = new P2PKWitness();

        var keysetIdBytes = keysetId.GetBytes();
        var pubkeysTotalCount = Builder.Pubkeys.Length + Builder.RefundPubkeys?.Length;
        
        HashSet<int> usedSlots = new();
        
        while (keysRequiredLeft > 0 && availableKeysLeft.Any())
        {
            var key = availableKeysLeft.First();
            for (int i = 0; i < pubkeysTotalCount; i++)
            {
                if (usedSlots.Contains(i))
                {
                    continue;
                }
                
                var Zx = Cashu.ComputeZx(key, P2PkE);
                var shouldAddBytes = Cashu.CheckRiOverflow(Zx, keysetIdBytes, i);
                var ri = Cashu.ComputeRi(Zx, keysetIdBytes, i, shouldAddBytes);

                var tweakedPrivkey = key.TweakAdd(ri.ToBytes());
                var tweakedPubkey = tweakedPrivkey.CreatePubKey();
                
                var tweakedPrivkeyNeg = key.sec.Negate().Add(ri.sec).ToPrivateKey();
                var tweakedPubkeyNeg = tweakedPrivkeyNeg.CreatePubKey();
                
                if (allowedKeys.Contains(tweakedPubkey))
                {
                    usedSlots.Add(i);
                    var sig = tweakedPrivkey.SignBIP340(msg);
                    tweakedPrivkey.CreateXOnlyPubKey().SigVerifyBIP340(sig, msg);
                    result.Signatures = result.Signatures.Append(sig.ToHex()).ToArray();
                    availableKeysLeft = availableKeysLeft.Except(new[] {key}).ToArray();
                    keysRequiredLeft = requiredSignatures - result.Signatures.Length;
                    break;
                }

                if (allowedKeys.Contains(tweakedPubkeyNeg))
                {
                    usedSlots.Add(i);
                    var sig = tweakedPrivkeyNeg.SignBIP340(msg);
                    tweakedPrivkeyNeg.CreateXOnlyPubKey().SigVerifyBIP340(sig, msg);
                    result.Signatures = result.Signatures.Append(sig.ToHex()).ToArray();
                    availableKeysLeft = availableKeysLeft.Except(new[] {key}).ToArray();
                    keysRequiredLeft = requiredSignatures - result.Signatures.Length;
                    break;

                }
            }
        }
        if (keysRequiredLeft > 0)
            throw new InvalidOperationException("Not enough valid keys to sign");
        return result;
    }

   
    public virtual bool VerifyWitness(string message, P2PKWitness witness)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(message));
        return VerifyWitnessHash(hash, witness);
    }

    public virtual bool VerifyWitness(ISecret secret, P2PKWitness witness)
    {
        return VerifyWitness(secret.GetBytes(), witness);
    }

    public virtual bool VerifyWitness(byte[] message, P2PKWitness witness)
    {
        var hash = SHA256.HashData(message);
        return VerifyWitnessHash(hash, witness);
    }

    public virtual bool VerifyWitnessHash(byte[] hash, P2PKWitness witness)
    {
        try
        {
            var allowedKeys = GetAllowedPubkeys(out var requiredSignatures);
            if (witness.Signatures.Length < requiredSignatures)
                return false;
            var sigs = witness.Signatures
                .Select(s => SecpSchnorrSignature.TryCreate(Convert.FromHexString(s), out var sig) ? sig : null)
                .Where(signature => signature is not null).ToArray();
            return sigs.Count(s => allowedKeys.Any(p => p.ToXOnlyPubKey().SigVerifyBIP340(s, hash))) >=
                   requiredSignatures;
        }
        catch (Exception e)
        {
            return false;
        }
    }
}