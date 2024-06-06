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
        if (builder.Lock.HasValue && builder.Lock.Value.ToUnixTimeSeconds() < DateTimeOffset.Now.ToUnixTimeSeconds())
        {
            requiredSignatures = Math.Min(builder.RefundPubkeys?.Length ?? 0, 1);
            return builder.RefundPubkeys ?? Array.Empty<ECPubKey>();
        }

        requiredSignatures = builder.SignatureThreshold;
        return builder.Pubkeys;
    }

    public HTLCWitness GenerateWitness(Proof proof, ECPrivKey[] keys, string preimage)
    {
        return GenerateWitness(proof.Secret.GetBytes(), keys, Encoding.UTF8.GetBytes(preimage));
    }

    public HTLCWitness GenerateWitness(BlindedMessage blindedMessage, ECPrivKey[] keys, string preimage)
    {
        return GenerateWitness(blindedMessage.B_.Key.ToBytes(), keys, Encoding.UTF8.GetBytes(preimage));
    }

    public HTLCWitness GenerateWitness(byte[] msg, ECPrivKey[] keys, byte[] preimage)
    {
        var hash = SHA256.HashData(msg);

        return GenerateWitness(ECPrivKey.Create(hash), keys, preimage);
    }

    public HTLCWitness GenerateWitness(ECPrivKey hash, ECPrivKey[] keys, byte[] preimage)
    {
        if (!VerifyPreimage(Encoding.UTF8.GetString(preimage)))
            throw new InvalidOperationException("Invalid preimage");
        var p2pkhWitness = base.GenerateWitness(hash, keys);
        return new HTLCWitness()
        {
            Signatures = p2pkhWitness.Signatures,
            Preimage = Encoding.UTF8.GetString(preimage)
        };
    }

    public bool VerifyPreimage(string preimage)
    {
        return Builder.HashLock.ToBytes().SequenceEqual(SHA256.HashData(Encoding.UTF8.GetBytes(preimage)));
    }

    public bool VerifyWitness(string message, HTLCWitness witness)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(message));
        return VerifyWitnessHash(hash, witness);
    }

    public bool VerifyWitness(ISecret secret, HTLCWitness witness)
    {
        if (secret is Nut10Secret {ProofSecret: HTLCProofSecret htlcProofSecret} &&
            !VerifyPreimage(htlcProofSecret.Builder.HashLock.ToHex()))
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
        return base.VerifyWitnessHash(hash, witness);
    }
}