using NBitcoin.Secp256k1;

namespace DotNut;

public class HTLCBuilder : P2PkBuilder
{
    public ECPubKey HashLock { get; set; }
    
    public static HTLCBuilder Load(HTLCProofSecret proofSecret)
    {
        var hashLock = proofSecret.Data.ToPubKey();
        var innerbuilder = P2PkBuilder.Load(proofSecret);
        innerbuilder.Pubkeys = innerbuilder.Pubkeys.Except(new[] {hashLock}).ToArray();
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
        innerBuilder.Pubkeys = innerBuilder.Pubkeys.Prepend(HashLock).ToArray();
        
        var p2pkProof = innerBuilder.Build();
        return new HTLCProofSecret()
        {
            Data = HashLock.ToHex(),
            Nonce = p2pkProof.Nonce,
            Tags = p2pkProof.Tags
        };
    }

    public new HTLCProofSecret BuildBlinded(KeysetId keysetId, out ECPubKey p2pkE)
    {
        throw new NotImplementedException();
    }

    public HTLCProofSecret BuildBlinded(KeysetId keysetId, ECPrivKey p2pke)
    {
        throw new NotImplementedException();
    }
}