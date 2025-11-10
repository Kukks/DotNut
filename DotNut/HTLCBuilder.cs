using NBitcoin.Secp256k1;

namespace DotNut;

public class HTLCBuilder : P2PkBuilder
{
    public string HashLock { get; set; }

    private static readonly PubKey _dummy =
        "020000000000000000000000000000000000000000000000000000000000000001".ToPubKey();
    
    public static HTLCBuilder Load(HTLCProofSecret proofSecret)
    {
        var hashLock = proofSecret.Data;
        
        var tempProof = new P2PKProofSecret
        {
            Data = _dummy.ToString(),
            Nonce = proofSecret.Nonce,
            Tags = proofSecret.Tags 
        };
        
        var innerbuilder = P2PkBuilder.Load(tempProof);
        innerbuilder.Pubkeys = innerbuilder.Pubkeys.Except([_dummy.Key]).ToArray();
        return new HTLCBuilder()
        {
            HashLock = hashLock,
            Lock = innerbuilder.Lock,
            Pubkeys = innerbuilder.Pubkeys,
            RefundPubkeys = innerbuilder.RefundPubkeys,
            SignatureThreshold = innerbuilder.SignatureThreshold,
            SigFlag = innerbuilder.SigFlag,
            Nonce = innerbuilder.Nonce
        };
        
    }
    
    public new HTLCProofSecret Build()
    {
        var innerBuilder = new P2PkBuilder()
        {
            Lock = Lock,
            Pubkeys = Pubkeys.ToArray(),
            RefundPubkeys = RefundPubkeys,
            SignatureThreshold = SignatureThreshold,
            SigFlag = SigFlag,
            Nonce = Nonce
        };
        innerBuilder.Pubkeys = innerBuilder.Pubkeys.Prepend(_dummy.Key).ToArray();
        
        var p2pkProof = innerBuilder.Build();
        return new HTLCProofSecret()
        {
            Data = HashLock,
            Nonce = p2pkProof.Nonce,
            Tags = p2pkProof.Tags
        };
    }
}