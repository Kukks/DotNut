using System.Text.Json;
using NBitcoin.Secp256k1;

namespace DotNut.Abstractions;

public static class Nut10Helper
{
    public static void MaybeProcessNut10(
        List<PrivKey> privKeys,
        List<Proof> proofs,
        List<OutputData>? outputs = null,
        string? htlcPreimage = null,
        string? meltQuoteId = null
        )
    {
        if (privKeys.Count == 0 || proofs.Count == 0)
        {
            return;
        }
        
        outputs ??= [];
        var sigAllHandler = new SigAllHandler
        {
            Proofs = proofs,
            PrivKeys = privKeys,
            BlindedMessages = outputs.Select(o=>o.BlindedMessage).ToList(),
            HTLCPreimage = htlcPreimage,
            MeltQuoteId = meltQuoteId
        };
        
        if (sigAllHandler.TrySign(out P2PKWitness? witness))
        {
            if (witness == null)
            {
                throw new ArgumentNullException(nameof(witness),
                    "sig_all input was correct, but couldn't create a witness signature!");
            }

            proofs[0].Witness = JsonSerializer.Serialize(witness);
            return;
        }

        var keys = privKeys.Select(p => p.Key).ToArray();

        foreach (var proof in proofs)
        {
            handleWitnessCreation(proof, keys, htlcPreimage);
        }
    }

    private static void handleWitnessCreation(Proof proof, ECPrivKey[] keys, string? htlcPreimage)
    {
        if (proof.Secret is Nut10Secret { ProofSecret: HTLCProofSecret htlc })
        {
            // preimage isn't verified after timelock 
            var preimage = htlcPreimage??""; 
            if (proof.P2PkE is { } E)
            {
                var blindwitness = htlc.GenerateBlindWitness(proof, keys, preimage);
                proof.Witness = JsonSerializer.Serialize(blindwitness);
                return;
            }
            var witness = htlc.GenerateWitness(proof, keys, preimage);
            proof.Witness = JsonSerializer.Serialize(witness);
            return;
        }

        if (proof.Secret is Nut10Secret { ProofSecret: P2PKProofSecret p2pk })
        {
            if (proof.P2PkE is { } E)
            {
                var blindWitness = p2pk.GenerateBlindWitness(proof, keys);
                proof.Witness = JsonSerializer.Serialize(blindWitness);
                return;
            }
            var proofWitness = p2pk.GenerateWitness(proof, keys);
            proof.Witness = JsonSerializer.Serialize(proofWitness);
        }
    }
}