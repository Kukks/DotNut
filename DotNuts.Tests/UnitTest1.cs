using NBitcoin.Secp256k1;

namespace DotNuts.Tests;

public class UnitTest1
{
    [InlineData("0000000000000000000000000000000000000000000000000000000000000000",
        "024cce997d3b518f739663b757deaec95bcd9473c30a14ac2fd04023a739d1a725")]
    [InlineData("0000000000000000000000000000000000000000000000000000000000000001",
        "022e7158e11c9506f1aa4248bf531298daa7febd6194f003edcd9b93ade6253acf")]
    [InlineData("0000000000000000000000000000000000000000000000000000000000000002",
        "026cdbe15362df59cd1dd3c9c11de8aedac2106eca69236ecd9fbe117af897be4f")]
    [Theory]
    public void Nut00Tests_HashToCurve(string message, string point)
    {
        var result = Nut00.HexToCurve(message);
        Assert.Equal(point, Convert.ToHexString(result.ToBytes(true)).ToLower());
    }


    [InlineData("d341ee4871f1f889041e63cf0d3823c713eea6aff01e80f1719f08f9e5be98f6",
        "99fce58439fc37412ab3468b73db0569322588f62fb3a49182d67e23d877824a",
        "026a0019ed7dd2fc84aec809a7d937da0dd6cca6693bfea9a887be33119c153ee9")]
    [InlineData("f1aaf16c2239746f369572c0784d9dd3d032d952c2d992175873fb58fae31a60",
        "f78476ea7cc9ade20f9e05e58a804cf19533f03ea805ece5fee88c8e2874ba50",
        "02be78ed8172c85cec8e7aacb6d38fbde518d726daa27d3d1144193e0ce474b681")]
    [Theory]
    public void Nut00Tests_BlindedMessages(string x, string r, string b)
    {
        //     secret_msg: str, blinding_factor: Optional[PrivateKey] = None
        //     ) -> tuple[PublicKey, PrivateKey]:
        // Y: PublicKey = hash_to_curve(secret_msg.encode("utf-8"))
        // r = blinding_factor or PrivateKey()
        // B_: PublicKey = Y + r.pubkey  # type: ignore
        // return B_, r

        
        // Y = hash_to_curve(secret_message)
        // r = random blinding factor
        // B'= Y + r*G
        // return B'
        

        var y = Nut00.HexToCurve(x);
        var blindingFactor = ECPrivKey.Create(Convert.FromHexString(r));

        var computedB = Nut00.ComputeB(y, blindingFactor);
        Assert.Equal(b, Convert.ToHexString(computedB.ToBytes()).ToLower());
    }

    [InlineData("0000000000000000000000000000000000000000000000000000000000000001","02a9acc1e48c25eeeb9289b5031cc57da9fe72f3fe2861d264bdc074209b107ba2" ,"02a9acc1e48c25eeeb9289b5031cc57da9fe72f3fe2861d264bdc074209b107ba2")]
    [InlineData("7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f","02a9acc1e48c25eeeb9289b5031cc57da9fe72f3fe2861d264bdc074209b107ba2" ,"0398bc70ce8184d27ba89834d19f5199c84443c31131e48d3c1214db24247d005d")]
    [Theory]
    public void Nut00Tests_BlindedSignatures(string k, string b_, string blindedKey)
    {
        var mintKey = ECPrivKey.Create(Convert.FromHexString(k));
        var B_ = ECPubKey.Create(Convert.FromHexString(b_));
        ECPubKey.Create(Convert.FromHexString(blindedKey));

        var computedC = Nut00.ComputeC_(B_, mintKey);
        Assert.Equal(blindedKey, Convert.ToHexString(computedC.ToBytes()).ToLower());
    }

    [Fact]
    public void Nut00Tests_TokenSerialization()
    {
        string originalToken =
            "cashuAeyJ0b2tlbiI6W3sibWludCI6Imh0dHBzOi8vODMzMy5zcGFjZTozMzM4IiwicHJvb2ZzIjpbeyJhbW91bnQiOjIsImlkIjoiMDA5YTFmMjkzMjUzZTQxZSIsInNlY3JldCI6IjQwNzkxNWJjMjEyYmU2MWE3N2UzZTZkMmFlYjRjNzI3OTgwYmRhNTFjZDA2YTZhZmMyOWUyODYxNzY4YTc4MzciLCJDIjoiMDJiYzkwOTc5OTdkODFhZmIyY2M3MzQ2YjVlNDM0NWE5MzQ2YmQyYTUwNmViNzk1ODU5OGE3MmYwY2Y4NTE2M2VhIn0seyJhbW91bnQiOjgsImlkIjoiMDA5YTFmMjkzMjUzZTQxZSIsInNlY3JldCI6ImZlMTUxMDkzMTRlNjFkNzc1NmIwZjhlZTBmMjNhNjI0YWNhYTNmNGUwNDJmNjE0MzNjNzI4YzcwNTdiOTMxYmUiLCJDIjoiMDI5ZThlNTA1MGI4OTBhN2Q2YzA5NjhkYjE2YmMxZDVkNWZhMDQwZWExZGUyODRmNmVjNjlkNjEyOTlmNjcxMDU5In1dfV0sInVuaXQiOiJzYXQiLCJtZW1vIjoiVGhhbmsgeW91LiJ9";
        var result = CashuTokenHelper.Decode(
            originalToken,
            out var v);
        Assert.Equal("A", v);
        Assert.Equal("Thank you.", result.Memo);
        Assert.Equal("sat", result.Unit);
        var token = Assert.Single(result.Tokens);
        Assert.Equal("https://8333.space:3338", token.Mint);
        Assert.Equal(2, token.Proofs.Count);
        Assert.Collection(token.Proofs, proof =>
            {
                Assert.Equal(2, proof.Amount);
                Assert.Equal("009a1f293253e41e", proof.Id);
                Assert.Equal("407915bc212be61a77e3e6d2aeb4c727980bda51cd06a6afc29e2861768a7837", proof.Secret);
                Assert.Equal("02bc9097997d81afb2cc7346b5e4345a9346bd2a506eb7958598a72f0cf85163ea", proof.C);
            }, proof =>
            {
                Assert.Equal(8, proof.Amount);
                Assert.Equal("009a1f293253e41e", proof.Id);
                Assert.Equal("fe15109314e61d7756b0f8ee0f23a624acaa3f4e042f61433c728c7057b931be", proof.Secret);
                Assert.Equal("029e8e5050b890a7d6c0968db16bc1d5d5fa040ea1de284f6ec69d61299f671059", proof.C);
            }
        );

        Assert.Equal(originalToken, CashuTokenHelper.Encode(result, "A", false));

        Assert.Throws<FormatException>(() => CashuTokenHelper.Decode(
            "casshuAeyJ0b2tlbiI6W3sibWludCI6Imh0dHBzOi8vODMzMy5zcGFjZTozMzM4IiwicHJvb2ZzIjpbeyJhbW91bnQiOjIsImlkIjoiMDA5YTFmMjkzMjUzZTQxZSIsInNlY3JldCI6IjQwNzkxNWJjMjEyYmU2MWE3N2UzZTZkMmFlYjRjNzI3OTgwYmRhNTFjZDA2YTZhZmMyOWUyODYxNzY4YTc4MzciLCJDIjoiMDJiYzkwOTc5OTdkODFhZmIyY2M3MzQ2YjVlNDM0NWE5MzQ2YmQyYTUwNmViNzk1ODU5OGE3MmYwY2Y4NTE2M2VhIn0seyJhbW91bnQiOjgsImlkIjoiMDA5YTFmMjkzMjUzZTQxZSIsInNlY3JldCI6ImZlMTUxMDkzMTRlNjFkNzc1NmIwZjhlZTBmMjNhNjI0YWNhYTNmNGUwNDJmNjE0MzNjNzI4YzcwNTdiOTMxYmUiLCJDIjoiMDI5ZThlNTA1MGI4OTBhN2Q2YzA5NjhkYjE2YmMxZDVkNWZhMDQwZWExZGUyODRmNmVjNjlkNjEyOTlmNjcxMDU5In1dfV0sInVuaXQiOiJzYXQiLCJtZW1vIjoiVGhhbmsgeW91LiJ9",
            out _));
        Assert.Throws<FormatException>(() => CashuTokenHelper.Decode(
            "eyJ0b2tlbiI6W3sibWludCI6Imh0dHBzOi8vODMzMy5zcGFjZTozMzM4IiwicHJvb2ZzIjpbeyJhbW91bnQiOjIsImlkIjoiMDA5YTFmMjkzMjUzZTQxZSIsInNlY3JldCI6IjQwNzkxNWJjMjEyYmU2MWE3N2UzZTZkMmFlYjRjNzI3OTgwYmRhNTFjZDA2YTZhZmMyOWUyODYxNzY4YTc4MzciLCJDIjoiMDJiYzkwOTc5OTdkODFhZmIyY2M3MzQ2YjVlNDM0NWE5MzQ2YmQyYTUwNmViNzk1ODU5OGE3MmYwY2Y4NTE2M2VhIn0seyJhbW91bnQiOjgsImlkIjoiMDA5YTFmMjkzMjUzZTQxZSIsInNlY3JldCI6ImZlMTUxMDkzMTRlNjFkNzc1NmIwZjhlZTBmMjNhNjI0YWNhYTNmNGUwNDJmNjE0MzNjNzI4YzcwNTdiOTMxYmUiLCJDIjoiMDI5ZThlNTA1MGI4OTBhN2Q2YzA5NjhkYjE2YmMxZDVkNWZhMDQwZWExZGUyODRmNmVjNjlkNjEyOTlmNjcxMDU5In1dfV0sInVuaXQiOiJzYXQiLCJtZW1vIjoiVGhhbmsgeW91LiJ9",
            out _));
    }
}