using System.Text;
using System.Text.Json.Serialization;
using NBitcoin.Secp256k1;
using SHA256 = System.Security.Cryptography.SHA256;

namespace DotNut;

public class HTLCProofSecret : P2PKProofSecret
{
    public const string Key = "HTLC";

    [JsonIgnore] public HTLCBuilder Builder => HTLCBuilder.Load(this);

    public override ECPubKey[] GetAllowedPubkeys(out int requiredSignatures)
    {
        var builder = Builder;
        requiredSignatures = builder.SignatureThreshold;
        return builder.Pubkeys;
    }
    
    public override ECPubKey[] GetAllowedRefundPubkeys(out int? requiredSignatures)
    {
        var builder = Builder;
        if (builder.Lock.HasValue && builder.Lock.Value.ToUnixTimeSeconds() < DateTimeOffset.Now.ToUnixTimeSeconds())
        {
            if (builder.RefundPubkeys == null)
            {
                requiredSignatures = 0; // proof is spendable without any signature
                return [];
            }
            requiredSignatures = builder.RefundSignatureThreshold ?? 1;
            return [..builder.RefundPubkeys??[]];
        }

        requiredSignatures = null; // there's no refund condition :/
        return [];
    }

    
    
    public HTLCWitness GenerateWitness(Proof proof, ECPrivKey[] keys, string preimage)
    {
        return GenerateWitness(proof.Secret.GetBytes(), keys, Convert.FromHexString(preimage));
    }

    public HTLCWitness GenerateWitness(BlindedMessage blindedMessage, ECPrivKey[] keys, string preimage)
    {
        return GenerateWitness(blindedMessage.B_.Key.ToBytes(), keys, Convert.FromHexString(preimage));
    }

    public HTLCWitness GenerateWitness(byte[] msg, ECPrivKey[] keys, byte[] preimage)
    {
        var hash = SHA256.HashData(msg);
        return GenerateWitness(ECPrivKey.Create(hash), keys, preimage);
    }

    public HTLCWitness GenerateWitness(ECPrivKey hash, ECPrivKey[] keys, byte[] preimage)
    {
        if (!VerifyPreimage(preimage))
            throw new InvalidOperationException("Invalid preimage");
        var p2pkhWitness = base.GenerateWitness(hash, keys);
        return new HTLCWitness()
        {
            Signatures = p2pkhWitness.Signatures,
            Preimage = Convert.ToHexString(preimage)
        };
    }

    

    public HTLCWitness GenerateBlindWitness(Proof proof, ECPrivKey[] keys, byte[] preimage, KeysetId keysetId)
    {
        throw new NotImplementedException();
    }
    public HTLCWitness GenerateBlindWitness(Proof proof, ECPrivKey[] keys, byte[] preimage, KeysetId keysetId,
        ECPubKey P2PkE)
    {
        throw new NotImplementedException();
    }

    public HTLCWitness GenerateBlindWitness(BlindedMessage message, ECPrivKey[] keys, byte[] preimage, KeysetId keysetId, ECPubKey P2PkE)
    {
        throw new NotImplementedException();
    }

    public HTLCWitness GenerateBlindWitness(byte[] msg, ECPrivKey[] keys, byte[] preimage, KeysetId keysetId,
        ECPubKey P2PkE)
    {
        throw new NotImplementedException();
    }
    
    public HTLCWitness GenerateBlindWitness(ECPrivKey hash, ECPrivKey[] keys, byte[] preimage, KeysetId keysetId,
        ECPubKey P2PkE)
    {
        throw new NotImplementedException();
    }
    
    
    
    public bool VerifyPreimage(string preimage)
    {
        return Convert.FromHexString(Builder.HashLock).SequenceEqual(SHA256.HashData(Convert.FromHexString(preimage)));
    }

    public bool VerifyPreimage(byte[] preimage)
    {
        return Convert.FromHexString(Builder.HashLock).SequenceEqual(SHA256.HashData(preimage));
    }

    public bool VerifyWitness(string message, HTLCWitness witness)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(message));
        return VerifyWitnessHash(hash, witness);
    }

    public bool VerifyWitness(ISecret secret, HTLCWitness witness)
    {
        if (secret is not Nut10Secret {ProofSecret: HTLCProofSecret})
        {
            return false;
        }

        return VerifyWitness(secret.GetBytes(), witness);
    }
    

    [Obsolete("Use GenerateWitness(Proof proof, ECPrivKey[] keys, string preimage)")]
    public override P2PKWitness GenerateWitness(Proof proof, ECPrivKey[] keys)
    {
        throw new InvalidOperationException("Use GenerateWitness(Proof proof, ECPrivKey[] keys, string preimage)");
    }

    [Obsolete("Use GenerateWitness(byte[] msg, ECPrivKey[] keys, byte[] preimage)")]
    public override P2PKWitness GenerateWitness(BlindedMessage message, ECPrivKey[] keys)
    {
        throw new InvalidOperationException("Use GenerateWitness(BlindedMessage message, ECPrivKey[] keys, string preimage)");
    }

    [Obsolete("Use GenerateWitness(byte[] msg, ECPrivKey[] keys, byte[] preimage)")]
    public override P2PKWitness GenerateWitness(byte[] msg, ECPrivKey[] keys)
    {
        throw new InvalidOperationException("Use GenerateWitness(byte[] msg, ECPrivKey[] keys, byte[] preimage)");
    }
    

    [Obsolete("Use GenerateBlindWitness(Proof proof, ECPrivKey[] keys, byte[] preimage, KeysetId keysetId)")]
    public override P2PKWitness GenerateBlindWitness(Proof proof, ECPrivKey[] keys, KeysetId keysetId)
    {
        throw new InvalidOperationException();
    }
    
    [Obsolete("Use GenerateBlindWitness(Proof proof, ECPrivKey[] keys, byte[] preimage, KeysetId keysetId, ECPubKey P2PkE)")]
    public override P2PKWitness GenerateBlindWitness(Proof proof, ECPrivKey[] keys, KeysetId keysetId, ECPubKey P2PkE)
    {
        throw new InvalidOperationException("Use GenerateBlindWitness(Proof proof, ECPrivKey[] keys, byte[] preimage, KeysetId keysetId, ECPubKey P2PkE)");
    }
    
    [Obsolete("Use GenerateBlindWitness(BlindedMessage message, ECPrivKey[] keys, byte[] preimage, KeysetId keysetId, ECPubKey P2PkE)")]
    public override P2PKWitness GenerateBlindWitness(BlindedMessage message, ECPrivKey[] keys, KeysetId keysetId,
        ECPubKey P2PkE)
    {
        throw new InvalidOperationException("Use GenerateBlindWitness(BlindedMessage message, ECPrivKey[] keys, byte[] preimage, KeysetId keysetId, ECPubKey P2PkE)");
    }
    
    [Obsolete("Use GenerateBlindWitness(byte[] msg, ECPrivKey[] keys, byte[] preimage, KeysetId keysetId, ECPubKey P2PkE)")]
    public override P2PKWitness GenerateBlindWitness(byte[] msg, ECPrivKey[] keys, KeysetId keysetId, ECPubKey P2PkE)
    {
        throw new InvalidOperationException("Use GenerateBlindWitness(byte[] msg, ECPrivKey[] keys, byte[] preimage, KeysetId keysetId, ECPubKey P2PkE)");
    }
    
    [Obsolete("Use GenerateBlindWitness(ECPrivKey hash, ECPrivKey[] keys, byte[] preimage, KeysetId keysetId, ECPubKey P2PkE)")]
    public override P2PKWitness GenerateBlindWitness(ECPrivKey hash, ECPrivKey[] keys, KeysetId keysetId,
        ECPubKey P2PkE)
    {
        throw new InvalidOperationException("Use GenerateBlindWitness(ECPrivKey hash, ECPrivKey[] keys, byte[] preimage, KeysetId keysetId, ECPubKey P2PkE)");
    }


    public override P2PKWitness GenerateWitness(ECPrivKey hash, ECPrivKey[] keys)
    {
        return base.GenerateWitness(hash, keys);
    }

    public override bool VerifyWitness(string message, P2PKWitness witness)
    {
        return base.VerifyWitness(message, witness);
    }

    public override bool VerifyWitness(ISecret secret, P2PKWitness witness)
    {
        return base.VerifyWitness(secret, witness);
    }

    public override bool VerifyWitness(byte[] message, P2PKWitness witness)
    {
        return base.VerifyWitness(message, witness);
    }
    public override bool VerifyWitnessHash(byte[] hash, P2PKWitness witness)
    {
        if (witness is not HTLCWitness htlcWitness)
        {
            return false;
        }
        if (!VerifyPreimage(htlcWitness.Preimage))
        {
            return false;
        }

        return base.VerifyWitnessHash(hash, witness);
    }
}