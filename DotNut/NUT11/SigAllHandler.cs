using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using DotNut;
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
    
    private P2PKProofSecret? _firstProofSecret; 
    
    
    public bool TrySign(out P2PKWitness? p2pkwitness)
    {
        p2pkwitness = null;
        
        if (BlindedMessages.Count == 0 || Proofs.Count == 0)
        {
            return false;
        }

        byte[] msg;
        try
        {
            var msgStr = GetMessageToSign(Proofs.ToArray(), BlindedMessages.ToArray(), MeltQuoteId);
            msg = Encoding.UTF8.GetBytes(msgStr);
        }
        catch (Exception _)
        {
            return false;
        }
        
        if (_firstProofSecret is HTLCProofSecret s && HTLCPreimage is {} preimage)
        {
            if (Proofs.First().P2PkE is { } E)
            {
                p2pkwitness = s.GenerateBlindWitness(msg,
                    PrivKeys.Select(pk => (ECPrivKey)pk).ToArray(),
                    Encoding.UTF8.GetBytes(preimage),
                    Proofs[0].Id,
                    E
                );
                return true;
            }
            p2pkwitness = 
                s.GenerateWitness(msg, PrivKeys.Select(pk => (ECPrivKey)pk).ToArray(), 
                    Encoding.UTF8.GetBytes(preimage)
                    );
            return true;
        }

        if (Proofs.First().P2PkE is { } e2)
        {
            p2pkwitness = _firstProofSecret!.GenerateBlindWitness(msg, PrivKeys.Select(pk => (ECPrivKey)pk).ToArray(), Proofs[0].Id, e2);
            return true;
        }
        p2pkwitness = _firstProofSecret!.GenerateWitness(msg, PrivKeys.Select(pk => (ECPrivKey)pk).ToArray());
        return true;
    }

    public static string GetMessageToSign(Proof[] inputs, BlindedMessage[] outputs, string? meltQuoteId = null)
    {
        if (!ValidateFirstProof(inputs[0], out var firstSecret))
        {
            throw new ArgumentException("Provided first proof is invalid");
        }
        var msg = new StringBuilder();
        
        if (inputs.Length > 0)
        {
            for (var i = 0; i < inputs.Length; i++)
            {
                var p = inputs[i];

                if (p.Secret is not Nut10Secret nut10)
                {
                    throw new ArgumentException($"When signing sig_all, every proof must be sig_all.");
                }
                
                if (!CheckIfEqualToFirst(firstSecret, nut10.ProofSecret))
                {
                    throw new ArgumentException($"When signing sig_all, every proof must have identical tags and data.");
                }
                // serialize as raw object
                var secret = JsonSerializer.Serialize((object)p.Secret);
                msg.Append(secret);
                msg.Append(p.C);
            }
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
        string? meltQuoteId = null)
    {
        if (proofs[0].Secret is Nut10Secret nut10_3)
            Console.WriteLine($"CP3 ProofSecret: {nut10_3.ProofSecret.GetType()}");
        byte[] msg;
        try
        {
            var msgStr = meltQuoteId is null
                ? GetMessageToSign(proofs, blindedMessages)
                : GetMessageToSign(proofs, blindedMessages, meltQuoteId);

            msg = Encoding.UTF8.GetBytes(msgStr);
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
            return false;
        }
        
        if (proofs[0].Secret is not Nut10Secret nut10)
            return false;
        
        return nut10.ProofSecret switch
        {
            HTLCProofSecret htlcs => htlcs.VerifyWitness(msg, witness),
            P2PKProofSecret p2pks => p2pks.VerifyWitness(msg, witness),
            _ => false
        };
    }

    public static bool VerifySigAllWitness(Proof[] proofs, BlindedMessage[] blindedMessages, string? meltQuoteId = null)
    {
        var firstProof = proofs.FirstOrDefault();
        if (firstProof?.Secret is not Nut10Secret { ProofSecret: var proofSecret } || firstProof.Witness is null)
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
        return witness is not null && VerifySigAllWitness(proofs, blindedMessages, witness, meltQuoteId);
    }
    
    private static bool ValidateFirstProof(Proof firstProof, out Nut10ProofSecret secret)
    {
        secret = null;
        
        if (firstProof.Secret is not Nut10Secret nut10)
        {
            return false;
        }

        var builder = nut10.ProofSecret switch
        {
            HTLCProofSecret htlcs => HTLCBuilder.Load(htlcs),
            P2PKProofSecret p2pks => P2PkBuilder.Load(p2pks),
            // won't throw exception if there will be a new type of nut10 secret, but will return false
            _ => new P2PkBuilder(){SigFlag = null} 
        };
        
        if (builder.SigFlag != "SIG_ALL")
        {
            return false;
        }

        secret = nut10.ProofSecret;
        return true;
    }
    
    private static bool CheckIfEqualToFirst(Nut10ProofSecret first, Nut10ProofSecret other) =>
        first is { } a && other is { } b &&
        a.Data == b.Data &&
        ((a.Tags == null && b.Tags == null) ||
         (a.Tags != null && b.Tags != null && a.Tags.SequenceEqual(b.Tags)));
}