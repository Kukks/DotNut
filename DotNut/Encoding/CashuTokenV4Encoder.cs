using System.Text;
using System.Text.Json;
using NBitcoin.Secp256k1;
using PeterO.Cbor;

namespace DotNut;

public class CashuTokenV4Encoder : ICashuTokenEncoder, ICBORToFromConverter<CashuToken>
{
    public string Encode(CashuToken token)
    {
        var obj = ToCBORObject(token);
        return Base64UrlSafe.Encode(obj.EncodeToBytes());
    }

    public CashuToken Decode(string token)
    {
        var obj = CBORObject.DecodeFromBytes(Base64UrlSafe.Decode(token));
        return FromCBORObject(obj);
    }

    public CBORObject ToCBORObject(CashuToken token)
    {
        //ensure that all token mints are the same
        var mints = token.Tokens.Select(token1 => token1.Mint).ToArray();
        if (mints.Distinct().Count() != 1)
            throw new FormatException("All proofs must have the same mint in v4 tokens");
        var proofSets = CBORObject.NewArray();
        foreach (var proofSet in token.Tokens.SelectMany(token1 => token1.Proofs).GroupBy(proof => proof.Id))
        {
            var proofSetItem = CBORObject.NewOrderedMap();
            proofSetItem.Add("i", Convert.FromHexString(proofSet.Key.ToString()));
            var proofSetItemArray = CBORObject.NewArray();
            foreach (var proof in proofSet)
            {
                var proofItem = CBORObject.NewOrderedMap()
                    .Add("a", proof.Amount)
                    .Add("s", Encoding.UTF8.GetString(proof.Secret.GetBytes()))
                    .Add("c", proof.C.Key.ToBytes());
                if (proof.DLEQ is not null)
                {
                    proofItem.Add("d", CBORObject
                        .NewOrderedMap()
                        .Add("e", proof.DLEQ.E.Key.ToBytes())
                        .Add("s", proof.DLEQ.S.Key.ToBytes())
                        .Add("r", proof.DLEQ.R.Key.ToBytes()));
                }

                if (proof.Witness is not null)
                {
                    proofItem.Add("w", proof.Witness);
                }

                if (proof.P2PkE?.Key is not null)
                {
                    proofItem.Add("pe", proof.P2PkE.Key.ToBytes());
                }

                proofSetItemArray.Add(proofItem);
            }

            proofSetItem.Add("p", proofSetItemArray);
            proofSets.Add(proofSetItem);
        }

        var cbor = CBORObject.NewOrderedMap();
            

        if (token.Memo is not null)
            cbor.Add("d", token.Memo);
        cbor.Add("t", proofSets)
            .Add("m", mints.First())
            .Add("u", token.Unit!);
        return cbor;
    }

    public CashuToken FromCBORObject(CBORObject obj)
    {
        return new CashuToken
        {
            Unit = obj["u"].AsString(),
            Memo = obj.GetOrDefault("d", null)?.AsString(),
            Tokens =
            [
                new CashuToken.Token()
                {
                    Mint = obj["m"].AsString(),
                    Proofs = obj["t"].Values.SelectMany(proofSet =>
                    {
                        var id = new KeysetId(Convert.ToHexString(proofSet["i"].GetByteString()).ToLowerInvariant());

                        return proofSet["p"].Values.Select(proof => new Proof()
                        {
                            Amount = proof["a"].ToObject<ulong>(),
                            Secret = JsonSerializer.Deserialize<ISecret>(proof["s"].ToJSONString())!,
                            C = ECPubKey.Create(proof["c"].GetByteString()),
                            Witness = proof.GetOrDefault("w", null)?.AsString(),
                            DLEQ = proof.GetOrDefault("d", null) is { } cborDLEQ
                                ? new DLEQProof
                                {
                                    E = ECPrivKey.Create(cborDLEQ["e"].GetByteString()),
                                    S = ECPrivKey.Create(cborDLEQ["s"].GetByteString()),
                                    R = ECPrivKey.Create(cborDLEQ["r"].GetByteString())
                                }
                                : null,
                            Id = id,
                            
                            P2PkE = proof.GetOrDefault("pe", null) is { } p2pkE
                                ? (PubKey?) ECPubKey.Create(p2pkE.GetByteString()) 
                                : null
                            
                        });
                    }).ToList()
                }
            ]
        };
    }
}