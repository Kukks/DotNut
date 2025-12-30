using System.Text;
using System.Text.Json.Serialization;
using NBitcoin.Secp256k1;
using SHA256 = System.Security.Cryptography.SHA256;

namespace DotNut;

public class P2PKProofSecret : Nut10ProofSecret
{
    public const string Key = "P2PK";

    [JsonIgnore] P2PKBuilder Builder => P2PKBuilder.Load(this);

    public virtual ECPubKey[] GetAllowedPubkeys(out int requiredSignatures)
    {
        var builder = Builder;
        requiredSignatures = builder.SignatureThreshold;
        return builder.Pubkeys;
    }
    
    public virtual ECPubKey[] GetAllowedRefundPubkeys(out int? requiredSignatures)
    {
        var builder = Builder;
        if (!builder.Lock.HasValue || builder.Lock.Value.ToUnixTimeSeconds() <= DateTimeOffset.Now.ToUnixTimeSeconds())
        {
            requiredSignatures = null; // there's no refund condition, or timelock didn't expire yet :/
            return [];
        }
        
        if (builder.RefundPubkeys == null)
        {
            requiredSignatures = 0; // proof is spendable without any signature
            return [];
        }
        requiredSignatures = builder.RefundSignatureThreshold ?? 1;
        return builder.RefundPubkeys ?? [];
    }
    
    /* 
     * ====================================================================== *
     * If any of these returns null witness - well, witness is not necessary *
     * ====================================================================== *
     */
    
    public virtual P2PKWitness? GenerateWitness(Proof proof, ECPrivKey[] keys)
    {
        return GenerateWitness(proof.Secret.GetBytes(), keys);
    }  
    
    public virtual P2PKWitness? GenerateWitness(BlindedMessage message, ECPrivKey[] keys)
    {
        return GenerateWitness(message.B_.Key.ToBytes(), keys);
    }
    
    public virtual P2PKWitness? GenerateWitness(byte[] msg, ECPrivKey[] keys)
    {
        var hash = SHA256.HashData(msg);
        return GenerateWitness(ECPrivKey.Create(hash), keys);
    }
    
    public virtual P2PKWitness? GenerateWitness(ECPrivKey hash, ECPrivKey[] keys)
    {
        var msg = hash.ToBytes();
    
        var allowedKeys = GetAllowedPubkeys(out var requiredSignatures);
        var allowedRefundKeys = GetAllowedRefundPubkeys(out var requiredRefundSignatures);
    
        if (requiredRefundSignatures == 0)
        {
            return null;
        }
    
        // try normal path
        var (isValid, result) = TrySignPath(allowedKeys.ToArray(), requiredSignatures, keys, msg);
        if (isValid)
        { 
            return result;
        }
    
        // if it's after locktime - try refund path
        if (requiredRefundSignatures.HasValue && allowedRefundKeys.Any())
        {
            (isValid, result) = TrySignPath(allowedRefundKeys.ToArray(), requiredRefundSignatures.Value, keys, msg);
            if (isValid)
            {
                return result;
            }
        }
    
        throw new InvalidOperationException("Not enough valid keys to sign!");
    }
    
    private (bool IsValid, P2PKWitness Witness) TrySignPath(ECPubKey[] allowedKeys, int requiredSignatures, 
        ECPrivKey[] availableKeys, byte[] msg)
    {
        var allowedKeysSet = new HashSet<ECPubKey>(allowedKeys);
        var result = new P2PKWitness();

        foreach (var privKey in availableKeys)
        {
            if (result.Signatures.Length >= requiredSignatures)
                break;

            var pubkey = privKey.CreatePubKey();
            if (allowedKeysSet.Contains(pubkey))
            {
                var sig = privKey.SignBIP340(msg);
                result.Signatures = result.Signatures.Append(sig.ToHex()).ToArray();
            }
        }

        return (result.Signatures.Length >= requiredSignatures, result);
    }
    
    private bool VerifyPath(ECPubKey[] allowedKeys, int requiredSignatures, 
        SecpSchnorrSignature[] sigs, byte[] hash)
    {
        if (sigs.Length < requiredSignatures)
            return false;

        var xonlyKeys = allowedKeys.Select(k => k.ToXOnlyPubKey()).ToArray();
        var validCount = sigs.Count(s => xonlyKeys.Any(xonly => xonly.SigVerifyBIP340(s, hash)));
    
        return validCount >= requiredSignatures;
    }



    /*
     * =========================
     * NUT-XX Pay to blinded key
     * =========================
     */
    
    public virtual P2PKWitness? GenerateBlindWitness(Proof proof, ECPrivKey[] keys, KeysetId keysetId)
    {
        ArgumentNullException.ThrowIfNull(proof.P2PkE);
        return GenerateBlindWitness(proof.Secret.GetBytes(), keys, keysetId, proof.P2PkE);
    }

    public virtual P2PKWitness? GenerateBlindWitness(Proof proof, ECPrivKey[] keys, KeysetId keysetId, ECPubKey P2PkE)
    {
        return GenerateBlindWitness(proof.Secret.GetBytes(), keys, keysetId, P2PkE);
    }
    
    public virtual P2PKWitness? GenerateBlindWitness(BlindedMessage message, ECPrivKey[] keys, KeysetId keysetId, ECPubKey P2PkE)
    {
        return GenerateBlindWitness(message.B_.Key.ToBytes(), keys, keysetId, P2PkE);
    }
    
    public virtual P2PKWitness? GenerateBlindWitness(byte[] msg, ECPrivKey[] keys, KeysetId keysetId, ECPubKey P2PkE)
    {
        var hash = SHA256.HashData(msg);
        return GenerateBlindWitness(ECPrivKey.Create(hash), keys, keysetId, P2PkE);
    }
    
    public virtual P2PKWitness? GenerateBlindWitness(ECPrivKey hash, ECPrivKey[] keys, KeysetId keysetId, ECPubKey P2PkE)
    {
        var msg = hash.ToBytes();
    
        var allowedKeys = GetAllowedPubkeys(out var requiredSignatures);
        var allowedRefundKeys = GetAllowedRefundPubkeys(out var requiredRefundSignatures);

        if (requiredRefundSignatures == 0)
            return null;

        var (isValid, result) = TrySignBlindPath(allowedKeys.ToArray(), requiredSignatures, keys, keysetId, P2PkE, msg);
        if (isValid)
        {
            return result;
        }

        if (requiredRefundSignatures.HasValue && allowedRefundKeys.Any())
        {
            (isValid, result) = TrySignBlindPath(allowedRefundKeys.ToArray(), requiredRefundSignatures.Value, keys, keysetId, P2PkE, msg);
            if (isValid)
            {
                return result;
            }
        }

        throw new InvalidOperationException("Not enough valid keys to sign any blind path");
    }

    
    private (bool IsValid, P2PKWitness Witness) TrySignBlindPath(ECPubKey[] allowedKeys, int requiredSignatures,
        ECPrivKey[] availableKeys, KeysetId keysetId, ECPubKey P2PkE, byte[] msg)
    {
        var allowedKeysSet = new HashSet<ECPubKey>(allowedKeys);
        var result = new P2PKWitness();
        var keysetIdBytes = keysetId.GetBytes();
        var usedSlots = new HashSet<int>();

        foreach (var key in availableKeys)
        {
            if (result.Signatures.Length >= requiredSignatures)
                break;

            for (int i = 0; i < allowedKeys.Length; i++) 
            {
                if (usedSlots.Contains(i)) continue;

                var Zx = Cashu.ComputeZx(key, P2PkE);
                var ri = Cashu.ComputeRi(Zx, keysetIdBytes, i);
                var tweakedPrivkey = key.TweakAdd(ri.ToBytes());
                var tweakedPubkey = tweakedPrivkey.CreatePubKey();

                if (allowedKeysSet.Contains(tweakedPubkey))
                {
                    usedSlots.Add(i);
                    var sig = tweakedPrivkey.SignBIP340(msg);
                    result.Signatures = result.Signatures.Append(sig.ToHex()).ToArray();
                    break;
                }

                var tweakedPrivkeyNeg = key.sec.Negate().Add(ri.sec).ToPrivateKey();
                var tweakedPubkeyNeg = tweakedPrivkeyNeg.CreatePubKey();

                if (allowedKeysSet.Contains(tweakedPubkeyNeg))
                {
                    usedSlots.Add(i);
                    var sig = tweakedPrivkeyNeg.SignBIP340(msg);
                    result.Signatures = result.Signatures.Append(sig.ToHex()).ToArray();
                    break;
                }
            }
        }

        return (result.Signatures.Length >= requiredSignatures, result);
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
            var sigs = witness.Signatures
                .Select(s => SecpSchnorrSignature.TryCreate(Convert.FromHexString(s), out var sig) ? sig : null)
                .Where(signature => signature is not null)
                .ToArray();

            var allowedKeys = GetAllowedPubkeys(out var requiredSignatures);
            var allowedRefundKeys = GetAllowedRefundPubkeys(out var requiredRefundSignatures);
            if (requiredRefundSignatures == 0)
            {
                return true;
            }
            
            if (VerifyPath(allowedKeys.ToArray(), requiredSignatures, sigs, hash))
                return true;

            
            if (requiredRefundSignatures.HasValue && allowedRefundKeys.Any())
            {
                if (VerifyPath(allowedRefundKeys.ToArray(), requiredRefundSignatures.Value, sigs, hash))
                    return true;
            }
            
            return false;
        }
        catch (Exception e)
        {
            return false;
        }
    }

}