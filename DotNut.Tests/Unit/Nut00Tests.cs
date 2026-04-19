using NBitcoin.Secp256k1;

namespace DotNut.Tests.Unit;

public class Nut00Tests
{
     [InlineData(
        "0000000000000000000000000000000000000000000000000000000000000000",
        "024cce997d3b518f739663b757deaec95bcd9473c30a14ac2fd04023a739d1a725"
    )]
    [InlineData(
        "0000000000000000000000000000000000000000000000000000000000000001",
        "022e7158e11c9506f1aa4248bf531298daa7febd6194f003edcd9b93ade6253acf"
    )]
    [InlineData(
        "0000000000000000000000000000000000000000000000000000000000000002",
        "026cdbe15362df59cd1dd3c9c11de8aedac2106eca69236ecd9fbe117af897be4f"
    )]
    [Theory]
    public void Nut00Tests_HashToCurve(string message, string point)
    {
        var result = Cashu.HexToCurve(message);
        Assert.Equal(point, result.ToHex());
    }

    [InlineData(
        "d341ee4871f1f889041e63cf0d3823c713eea6aff01e80f1719f08f9e5be98f6",
        "99fce58439fc37412ab3468b73db0569322588f62fb3a49182d67e23d877824a",
        "033b1a9737a40cc3fd9b6af4b723632b76a67a36782596304612a6c2bfb5197e6d"
    )]
    [InlineData(
        "f1aaf16c2239746f369572c0784d9dd3d032d952c2d992175873fb58fae31a60",
        "f78476ea7cc9ade20f9e05e58a804cf19533f03ea805ece5fee88c8e2874ba50",
        "029bdf2d716ee366eddf599ba252786c1033f47e230248a4612a5670ab931f1763"
    )]
    [Theory]
    public void Nut00Tests_BlindedMessages(string x, string r, string b)
    {
        var y = Cashu.HexToCurve(x);
        var blindingFactor = ECPrivKey.Create(Convert.FromHexString(r));

        var computedB = Cashu.ComputeB_(y, blindingFactor);
        Assert.Equal(b, computedB.ToHex());
    }

    [InlineData(
        "0000000000000000000000000000000000000000000000000000000000000001",
        "02a9acc1e48c25eeeb9289b5031cc57da9fe72f3fe2861d264bdc074209b107ba2",
        "02a9acc1e48c25eeeb9289b5031cc57da9fe72f3fe2861d264bdc074209b107ba2"
    )]
    [InlineData(
        "7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f",
        "02a9acc1e48c25eeeb9289b5031cc57da9fe72f3fe2861d264bdc074209b107ba2",
        "0398bc70ce8184d27ba89834d19f5199c84443c31131e48d3c1214db24247d005d"
    )]
    [Theory]
    public void Nut00Tests_BlindedSignatures(string k, string b_, string blindedKey)
    {
        var mintKey = ECPrivKey.Create(Convert.FromHexString(k));
        var B_ = ECPubKey.Create(Convert.FromHexString(b_));
        ECPubKey.Create(Convert.FromHexString(blindedKey));

        var computedC = Cashu.ComputeC_(B_, mintKey);
        Assert.Equal(blindedKey, computedC.ToHex());
    }

    [Fact]
    public void Nut00Tests_TokenSerialization()
    {
        string originalToken =
            "cashuAeyJ0b2tlbiI6W3sibWludCI6Imh0dHBzOi8vODMzMy5zcGFjZTozMzM4IiwicHJvb2ZzIjpbeyJhbW91bnQiOjIsImlkIjoiMDA5YTFmMjkzMjUzZTQxZSIsInNlY3JldCI6IjQwNzkxNWJjMjEyYmU2MWE3N2UzZTZkMmFlYjRjNzI3OTgwYmRhNTFjZDA2YTZhZmMyOWUyODYxNzY4YTc4MzciLCJDIjoiMDJiYzkwOTc5OTdkODFhZmIyY2M3MzQ2YjVlNDM0NWE5MzQ2YmQyYTUwNmViNzk1ODU5OGE3MmYwY2Y4NTE2M2VhIn0seyJhbW91bnQiOjgsImlkIjoiMDA5YTFmMjkzMjUzZTQxZSIsInNlY3JldCI6ImZlMTUxMDkzMTRlNjFkNzc1NmIwZjhlZTBmMjNhNjI0YWNhYTNmNGUwNDJmNjE0MzNjNzI4YzcwNTdiOTMxYmUiLCJDIjoiMDI5ZThlNTA1MGI4OTBhN2Q2YzA5NjhkYjE2YmMxZDVkNWZhMDQwZWExZGUyODRmNmVjNjlkNjEyOTlmNjcxMDU5In1dfV0sInVuaXQiOiJzYXQiLCJtZW1vIjoiVGhhbmsgeW91LiJ9";
        var result = CashuTokenHelper.Decode(originalToken, out var v);
        Assert.Equal("A", v);
        Assert.Equal("Thank you.", result.Memo);
        Assert.Equal("sat", result.Unit);
        var token = Assert.Single(result.Tokens);
        Assert.Equal("https://8333.space:3338", token.Mint);
        Assert.Equal(2, token.Proofs.Count);
        Assert.Collection(
            token.Proofs,
            proof =>
            {
                Assert.Equal((ulong)2, proof.Amount);
                Assert.Equal(new KeysetId("009a1f293253e41e"), proof.Id);

                Assert.Equal(
                    "407915bc212be61a77e3e6d2aeb4c727980bda51cd06a6afc29e2861768a7837",
                    Assert.IsType<StringSecret>(proof.Secret).Secret
                );
                Assert.Equal(
                    "02bc9097997d81afb2cc7346b5e4345a9346bd2a506eb7958598a72f0cf85163ea".ToPubKey(),
                    (ECPubKey)proof.C
                );
            },
            proof =>
            {
                Assert.Equal((ulong)8, proof.Amount);
                Assert.Equal(new KeysetId("009a1f293253e41e"), proof.Id);
                Assert.Equal(
                    "fe15109314e61d7756b0f8ee0f23a624acaa3f4e042f61433c728c7057b931be",
                    Assert.IsType<StringSecret>(proof.Secret).Secret
                );
                Assert.Equal(
                    "029e8e5050b890a7d6c0968db16bc1d5d5fa040ea1de284f6ec69d61299f671059".ToPubKey(),
                    (ECPubKey)proof.C
                );
            }
        );

        Assert.Equal(originalToken, result.Encode("A", false));

        Assert.Throws<FormatException>(() =>
            CashuTokenHelper.Decode(
                "casshuAeyJ0b2tlbiI6W3sibWludCI6Imh0dHBzOi8vODMzMy5zcGFjZTozMzM4IiwicHJvb2ZzIjpbeyJhbW91bnQiOjIsImlkIjoiMDA5YTFmMjkzMjUzZTQxZSIsInNlY3JldCI6IjQwNzkxNWJjMjEyYmU2MWE3N2UzZTZkMmFlYjRjNzI3OTgwYmRhNTFjZDA2YTZhZmMyOWUyODYxNzY4YTc4MzciLCJDIjoiMDJiYzkwOTc5OTdkODFhZmIyY2M3MzQ2YjVlNDM0NWE5MzQ2YmQyYTUwNmViNzk1ODU5OGE3MmYwY2Y4NTE2M2VhIn0seyJhbW91bnQiOjgsImlkIjoiMDA5YTFmMjkzMjUzZTQxZSIsInNlY3JldCI6ImZlMTUxMDkzMTRlNjFkNzc1NmIwZjhlZTBmMjNhNjI0YWNhYTNmNGUwNDJmNjE0MzNjNzI4YzcwNTdiOTMxYmUiLCJDIjoiMDI5ZThlNTA1MGI4OTBhN2Q2YzA5NjhkYjE2YmMxZDVkNWZhMDQwZWExZGUyODRmNmVjNjlkNjEyOTlmNjcxMDU5In1dfV0sInVuaXQiOiJzYXQiLCJtZW1vIjoiVGhhbmsgeW91LiJ9",
                out _
            )
        );
        Assert.Throws<FormatException>(() =>
            CashuTokenHelper.Decode(
                "eyJ0b2tlbiI6W3sibWludCI6Imh0dHBzOi8vODMzMy5zcGFjZTozMzM4IiwicHJvb2ZzIjpbeyJhbW91bnQiOjIsImlkIjoiMDA5YTFmMjkzMjUzZTQxZSIsInNlY3JldCI6IjQwNzkxNWJjMjEyYmU2MWE3N2UzZTZkMmFlYjRjNzI3OTgwYmRhNTFjZDA2YTZhZmMyOWUyODYxNzY4YTc4MzciLCJDIjoiMDJiYzkwOTc5OTdkODFhZmIyY2M3MzQ2YjVlNDM0NWE5MzQ2YmQyYTUwNmViNzk1ODU5OGE3MmYwY2Y4NTE2M2VhIn0seyJhbW91bnQiOjgsImlkIjoiMDA5YTFmMjkzMjUzZTQxZSIsInNlY3JldCI6ImZlMTUxMDkzMTRlNjFkNzc1NmIwZjhlZTBmMjNhNjI0YWNhYTNmNGUwNDJmNjE0MzNjNzI4YzcwNTdiOTMxYmUiLCJDIjoiMDI5ZThlNTA1MGI4OTBhN2Q2YzA5NjhkYjE2YmMxZDVkNWZhMDQwZWExZGUyODRmNmVjNjlkNjEyOTlmNjcxMDU5In1dfV0sInVuaXQiOiJzYXQiLCJtZW1vIjoiVGhhbmsgeW91LiJ9",
                out _
            )
        );

        var v4Token =
            "cashuBo2F0gqJhaUgA_9SLj17PgGFwgaNhYQFhc3hAYWNjMTI0MzVlN2I4NDg0YzNjZjE4NTAxNDkyMThhZjkwZjcxNmE1MmJmNGE1ZWQzNDdlNDhlY2MxM2Y3NzM4OGFjWCECRFODGd5IXVW-07KaZCvuWHk3WrnnpiDhHki6SCQh88-iYWlIAK0mjE0fWCZhcIKjYWECYXN4QDEzMjNkM2Q0NzA3YTU4YWQyZTIzYWRhNGU5ZjFmNDlmNWE1YjRhYzdiNzA4ZWIwZDYxZjczOGY0ODMwN2U4ZWVhY1ghAjRWqhENhLSsdHrr2Cw7AFrKUL9Ffr1XN6RBT6w659lNo2FhAWFzeEA1NmJjYmNiYjdjYzY0MDZiM2ZhNWQ1N2QyMTc0ZjRlZmY4YjQ0MDJiMTc2OTI2ZDNhNTdkM2MzZGNiYjU5ZDU3YWNYIQJzEpxXGeWZN5qXSmJjY8MzxWyvwObQGr5G1YCCgHicY2FtdWh0dHA6Ly9sb2NhbGhvc3Q6MzMzOGF1Y3NhdA";
        result = CashuTokenHelper.Decode(v4Token, out v);

        Assert.Equal("B", v);
        Assert.Null(result.Memo);
        Assert.Equal("sat", result.Unit);
        token = Assert.Single(result.Tokens);
        Assert.Equal("http://localhost:3338", token.Mint);
        Assert.Equal(3, token.Proofs.Count);
        Assert.Collection(
            token.Proofs,
            proof =>
            {
                Assert.Equal((ulong)1, proof.Amount);
                Assert.Equal(new KeysetId("00ffd48b8f5ecf80"), proof.Id);

                Assert.Equal(
                    "acc12435e7b8484c3cf1850149218af90f716a52bf4a5ed347e48ecc13f77388",
                    Assert.IsType<StringSecret>(proof.Secret).Secret
                );
                Assert.Equal(
                    "0244538319de485d55bed3b29a642bee5879375ab9e7a620e11e48ba482421f3cf".ToPubKey(),
                    (ECPubKey)proof.C
                );
            },
            proof =>
            {
                Assert.Equal((ulong)2, proof.Amount);
                Assert.Equal(new KeysetId("00ad268c4d1f5826"), proof.Id);
                Assert.Equal(
                    "1323d3d4707a58ad2e23ada4e9f1f49f5a5b4ac7b708eb0d61f738f48307e8ee",
                    Assert.IsType<StringSecret>(proof.Secret).Secret
                );
                Assert.Equal(
                    "023456aa110d84b4ac747aebd82c3b005aca50bf457ebd5737a4414fac3ae7d94d".ToPubKey(),
                    (ECPubKey)proof.C
                );
            },
            proof =>
            {
                Assert.Equal((ulong)1, proof.Amount);
                Assert.Equal(new KeysetId("00ad268c4d1f5826"), proof.Id);
                Assert.Equal(
                    "56bcbcbb7cc6406b3fa5d57d2174f4eff8b4402b176926d3a57d3c3dcbb59d57",
                    Assert.IsType<StringSecret>(proof.Secret).Secret
                );
                Assert.Equal(
                    "0273129c5719e599379a974a626363c333c56cafc0e6d01abe46d5808280789c63".ToPubKey(),
                    (ECPubKey)proof.C
                );
            }
        );
        Assert.Equal(v4Token, result.Encode("B", false));
    }
}