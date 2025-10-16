using NBitcoin.Secp256k1;

namespace DotNut;

public class HTLCBuilder : P2PkBuilder
{
    public string HashLock { get; set; }

    /*
     * ugly hack to reuse P2PkBuilder for HTLCs.
     * P2PkBuilder expects a pubkey in `data` field, but we need to store a hashlock instead
     * 
     * we inject a dummy pubkey so the loader doesn’t break, then remove it after load/build.
     */
    private static readonly PubKey _dummy =
        "020000000000000000000000000000000000000000000000000000000000000001".ToPubKey();
    
    public static HTLCBuilder Load(HTLCProofSecret proofSecret)
    {
        var hashLock = proofSecret.Data;
        if (hashLock.Length != 64) // hex string
        {
            throw new ArgumentException("HashLock must be 32 bytes (64 chars hex)", nameof(HashLock));
        }
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
        if (HashLock.Length != 64)
        {
            throw new ArgumentException("HashLock must be 32 bytes (64 chars hex)", nameof(HashLock));
        }
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

    public new HTLCProofSecret BuildBlinded(KeysetId keysetId, out ECPubKey p2pkE)
    {
        throw new NotImplementedException();
    }

    public HTLCProofSecret BuildBlinded(KeysetId keysetId, ECPrivKey p2pke)
    {
        throw new NotImplementedException();
    }
}