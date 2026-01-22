using System.Text;
using System.Text.Json;
using NBitcoin.Secp256k1;

namespace DotNut;

//Handles both P2PK and HTLC (if preimage added)
public class SigAllHandler
{
    public List<Proof> Proofs { get; set; }
    public List<PrivKey> PrivKeys { get; set; }
    public List<BlindedMessage> BlindedMessages { get; set; }
    public string? HTLCPreimage { get; set; }
    public string? MeltQuoteId { get; set; }

    private Nut10ProofSecret? _firstProofSecret;

    public bool TrySign(out string? witness)
    {
        witness = null;

        if (
            BlindedMessages is null
            || Proofs is null
            || PrivKeys is null
            || BlindedMessages.Count == 0
            || Proofs.Count == 0
            || PrivKeys.Count == 0
        )
        {
            return false;
        }

        byte[] msg;
        try
        {
            var msgStr = GetMessageToSign(Proofs.ToArray(), BlindedMessages.ToArray(), MeltQuoteId);
            if (!ValidateFirstProof(Proofs[0], out var sec) || sec is null)
            {
                return false;
            }
            _firstProofSecret = sec;
            msg = Encoding.UTF8.GetBytes(msgStr);
        }
        catch (ArgumentException)
        {
            return false;
        }

        if (_firstProofSecret is not P2PKProofSecret fps)
        {
            return false;
        }

        P2PKWitness witnessObj;
        if (fps is HTLCProofSecret s && HTLCPreimage is { } preimage)
        {
            if (Proofs.First().P2PkE is { } E)
            {
                witnessObj = s.GenerateBlindWitness(
                    msg,
                    PrivKeys.Select(pk => (ECPrivKey)pk).ToArray(),
                    Convert.FromHexString(preimage),
                    Proofs[0].Id,
                    E
                );
                witness = JsonSerializer.Serialize((HTLCWitness)witnessObj);
                return true;
            }
            witnessObj = s.GenerateWitness(
                msg,
                PrivKeys.Select(pk => (ECPrivKey)pk).ToArray(),
                Convert.FromHexString(preimage)
            );
            witness = JsonSerializer.Serialize((HTLCWitness)witnessObj);
            return true;
        }

        if (Proofs.First().P2PkE is { } e2)
        {
            witnessObj = fps.GenerateBlindWitness(
                msg,
                PrivKeys.Select(pk => (ECPrivKey)pk).ToArray(),
                Proofs[0].Id,
                e2
            );
            witness = JsonSerializer.Serialize(witnessObj);
            return true;
        }
        witnessObj = fps.GenerateWitness(msg, PrivKeys.Select(pk => (ECPrivKey)pk).ToArray());
        witness = JsonSerializer.Serialize(witnessObj);
        return true;
    }

    public static string GetMessageToSign(
        Proof[] inputs,
        BlindedMessage[] outputs,
        string? meltQuoteId = null
    )
    {
        if (inputs is null || inputs.Length == 0)
        {
            throw new ArgumentException(
                "At least one proof is required for SIG_ALL.",
                nameof(inputs)
            );
        }
        if (outputs is null || outputs.Length == 0)
        {
            throw new ArgumentException(
                "At least one blinded output is required for SIG_ALL.",
                nameof(outputs)
            );
        }
        if (!ValidateFirstProof(inputs[0], out var firstSecret))
        {
            throw new ArgumentException("Provided first proof is invalid");
        }
        var msg = new StringBuilder();

        for (var i = 0; i < inputs.Length; i++)
        {
            var p = inputs[i];

            if (p.Secret is not Nut10Secret nut10)
            {
                throw new ArgumentException(
                    "When signing sig_all, every proof must be a nut 10 secret."
                );
            }

            if (!CheckIfEqualToFirst(firstSecret, nut10.ProofSecret))
            {
                throw new ArgumentException(
                    "When signing sig_all, every proof must have identical tags and data."
                );
            }
            // serialize as raw object
            var secret = JsonSerializer.Serialize((object)p.Secret);
            msg.Append(secret);
            msg.Append(p.C);
        }

        foreach (var b in outputs)
        {
            msg.Append(b.Amount);
            msg.Append(b.B_);
        }

        if (meltQuoteId is not null)
        {
            msg.Append(meltQuoteId);
        }
        return msg.ToString();
    }

    public static bool VerifySigAllWitness(
        Proof[] proofs,
        BlindedMessage[] blindedMessages,
        P2PKWitness witness,
        string? meltQuoteId = null
    )
    {
        if (proofs is null || proofs.Length == 0)
        {
            return false;
        }
        byte[] msg;
        try
        {
            var msgStr = meltQuoteId is null
                ? GetMessageToSign(proofs, blindedMessages)
                : GetMessageToSign(proofs, blindedMessages, meltQuoteId);

            msg = Encoding.UTF8.GetBytes(msgStr);
        }
        catch (Exception ex)
        {
            return false;
        }

        if (proofs[0].Secret is not Nut10Secret nut10)
            return false;

        return nut10.ProofSecret switch
        {
            HTLCProofSecret htlcs => htlcs.VerifyWitness(msg, witness),
            P2PKProofSecret p2pks => p2pks.VerifyWitness(msg, witness),
            _ => false,
        };
    }

    public static bool VerifySigAllWitness(
        Proof[] proofs,
        BlindedMessage[] blindedMessages,
        string? meltQuoteId = null
    )
    {
        if (proofs is null || proofs.Length == 0)
        {
            return false;
        }
        var firstProof = proofs.FirstOrDefault();
        if (
            firstProof?.Secret is not Nut10Secret { ProofSecret: var proofSecret }
            || firstProof.Witness is null
        )
            return false;

        P2PKWitness? witness;
        try
        {
            var htlcWitness = JsonSerializer.Deserialize<HTLCWitness>(firstProof.Witness);
            if (htlcWitness?.Preimage is not null)
            {
                witness = htlcWitness;
            }
            else
            {
                witness = JsonSerializer.Deserialize<P2PKWitness>(firstProof.Witness);
            }
        }
        catch
        {
            return false;
        }
        return witness is not null
            && VerifySigAllWitness(proofs, blindedMessages, witness, meltQuoteId);
    }

    private static bool ValidateFirstProof(Proof firstProof, out Nut10ProofSecret? secret)
    {
        secret = null;

        if (firstProof.Secret is not Nut10Secret nut10)
        {
            return false;
        }

        var builder = nut10.ProofSecret switch
        {
            HTLCProofSecret htlcs => HTLCBuilder.Load(htlcs),
            P2PKProofSecret p2pks => P2PKBuilder.Load(p2pks),
            // won't throw exception if there will be a new type of nut10 secret, but will return false
            _ => new P2PKBuilder() { SigFlag = null },
        };

        if (builder.SigFlag != "SIG_ALL")
        {
            return false;
        }

        secret = nut10.ProofSecret;
        return true;
    }

    private static bool CheckIfEqualToFirst(Nut10ProofSecret first, Nut10ProofSecret other) =>
        first is { } a
        && other is { } b
        && a.Data == b.Data
        && (
            (a.Tags == null && b.Tags == null)
            || (
                a.Tags != null
                && b.Tags != null
                && a.Tags.Length == b.Tags.Length
                && a.Tags.Zip(b.Tags).All(pair => pair.First.SequenceEqual(pair.Second))
            )
        );
}
