using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using DotNut;
using NBitcoin.Secp256k1;

namespace DotNut;

public class SigAllHandler
{
    public Proof[] Proofs { get; set; }
    public PrivKey[] PrivKeys { get; set; }
    public BlindedMessage[] BlindedMessages { get; set; }
    
    public string? MeltQuoteId { get; set; }
    
    private P2PKProofSecret? _firstProofSecret; 
    
    public bool TrySign(out P2PKWitness? p2pkwitness)
    {
        p2pkwitness = null;

        if (BlindedMessages.Length == 0)
        {
            return false;
        }
        
        if (_validateFirstProof() == false)
        {
            return false;
        }

        string message = "";
        
        if (Proofs.Length > 0)
        {
            for (var i = 1; i < Proofs.Length; i++)
            {
                var p = Proofs[i];

                if (p.Secret is not Nut10Secret { ProofSecret: P2PKProofSecret p2pk })
                {
                    throw new ArgumentException($"When signing sig_all, every proof must be sig_all.");
                }

                if (!_checkIfEqualToFirst(p2pk))
                {
                    throw new ArgumentException($"When signing sig_all, every proof must have identical tags and data.");
                }
                message += JsonSerializer.Serialize(p.Secret);
            }
        }

        foreach (var b in BlindedMessages)
        {
            message += b.B_.ToString();
        }

        if (MeltQuoteId is not null)
        {
            message += MeltQuoteId;
        }
        var bytesMsg = System.Text.Encoding.UTF8.GetBytes(message);
        
        p2pkwitness = _firstProofSecret!.GenerateWitness(bytesMsg, PrivKeys.Select(pk => (ECPrivKey)pk).ToArray());
        return true;
    }

    

    private bool _validateFirstProof()
    {
        if (Proofs[0].Secret is not Nut10Secret { ProofSecret: P2PKProofSecret p2pks })
        {
            return false;
        }
        var b = P2PkBuilder.Load(p2pks);
        if (b.SigFlag != "SIG_ALL")
        {
            return false;
        }
        this._firstProofSecret = p2pks;
        
        return true;
    }
    private bool _checkIfEqualToFirst(P2PKProofSecret other) =>
        _firstProofSecret is { } a && other is { } b &&
        a.Data == b.Data &&
        ((a.Tags == null && b.Tags == null) ||
         (a.Tags != null && b.Tags != null && a.Tags.SequenceEqual(b.Tags)));
}