using System.Text.Json;
using DotNut.ApiModels;
using DotNut.NBitcoin.BIP39;
using DotNut.NUT13;
using NBip32Fast;
using NBitcoin.Secp256k1;
using Xunit.Abstractions;

namespace DotNut.Tests;

public class UnitTest1
{
    private readonly ITestOutputHelper _testOutputHelper;

    public UnitTest1(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

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

    [Theory]
    [InlineData(
        "{\n  \"1\":\"03a40f20667ed53513075dc51e715ff2046cad64eb68960632269ba7f0210e38\",\"2\":\"03fd4ce5a16b65576145949e6f99f445f8249fee17c606b688b504a849cdc452de\",\"4\":\"02648eccfa4c026960966276fa5a4cae46ce0fd432211a4f449bf84f13aa5f8303\",\"8\":\"02fdfd6796bfeac490cbee12f778f867f0a2c68f6508d17c649759ea0dc3547528\"\n}"
    )]
    [InlineData(
        "{\n  \"1\":\"03a40f20667ed53513075dc51e715ff2046cad64eb68960632269ba7f0210e38bc\",\"2\":\"04fd4ce5a16b65576145949e6f99f445f8249fee17c606b688b504a849cdc452de3625246cb2c27dac965cb7200a5986467eee92eb7d496bbf1453b074e223e481\",\"4\":\"02648eccfa4c026960966276fa5a4cae46ce0fd432211a4f449bf84f13aa5f8303\",\"8\":\"02fdfd6796bfeac490cbee12f778f867f0a2c68f6508d17c649759ea0dc3547528\"\n}"
    )]
    public void Nut01Tests_Keysets_Invalid(string keyset)
    {
        Assert.ThrowsAny<Exception>(() => JsonSerializer.Deserialize<Keyset>(keyset));
    }

    [Theory]
    [InlineData(
        "{\n  \"1\":\"03a40f20667ed53513075dc51e715ff2046cad64eb68960632269ba7f0210e38bc\",\"2\":\"03fd4ce5a16b65576145949e6f99f445f8249fee17c606b688b504a849cdc452de\",\"4\":\"02648eccfa4c026960966276fa5a4cae46ce0fd432211a4f449bf84f13aa5f8303\",\"8\":\"02fdfd6796bfeac490cbee12f778f867f0a2c68f6508d17c649759ea0dc3547528\"\n}"
    )]
    [InlineData(
        "{\n  \"1\":\"03ba786a2c0745f8c30e490288acd7a72dd53d65afd292ddefa326a4a3fa14c566\",\"2\":\"03361cd8bd1329fea797a6add1cf1990ffcf2270ceb9fc81eeee0e8e9c1bd0cdf5\",\"4\":\"036e378bcf78738ddf68859293c69778035740e41138ab183c94f8fee7572214c7\",\"8\":\"03909d73beaf28edfb283dbeb8da321afd40651e8902fcf5454ecc7d69788626c0\",\"16\":\"028a36f0e6638ea7466665fe174d958212723019ec08f9ce6898d897f88e68aa5d\",\"32\":\"03a97a40e146adee2687ac60c2ba2586a90f970de92a9d0e6cae5a4b9965f54612\",\"64\":\"03ce86f0c197aab181ddba0cfc5c5576e11dfd5164d9f3d4a3fc3ffbbf2e069664\",\"128\":\"0284f2c06d938a6f78794814c687560a0aabab19fe5e6f30ede38e113b132a3cb9\",\"256\":\"03b99f475b68e5b4c0ba809cdecaae64eade2d9787aa123206f91cd61f76c01459\",\"512\":\"03d4db82ea19a44d35274de51f78af0a710925fe7d9e03620b84e3e9976e3ac2eb\",\"1024\":\"031fbd4ba801870871d46cf62228a1b748905ebc07d3b210daf48de229e683f2dc\",\"2048\":\"0276cedb9a3b160db6a158ad4e468d2437f021293204b3cd4bf6247970d8aff54b\",\"4096\":\"02fc6b89b403ee9eb8a7ed457cd3973638080d6e04ca8af7307c965c166b555ea2\",\"8192\":\"0320265583e916d3a305f0d2687fcf2cd4e3cd03a16ea8261fda309c3ec5721e21\",\"16384\":\"036e41de58fdff3cb1d8d713f48c63bc61fa3b3e1631495a444d178363c0d2ed50\",\"32768\":\"0365438f613f19696264300b069d1dad93f0c60a37536b72a8ab7c7366a5ee6c04\",\"65536\":\"02408426cfb6fc86341bac79624ba8708a4376b2d92debdf4134813f866eb57a8d\",\"131072\":\"031063e9f11c94dc778c473e968966eac0e70b7145213fbaff5f7a007e71c65f41\",\"262144\":\"02f2a3e808f9cd168ec71b7f328258d0c1dda250659c1aced14c7f5cf05aab4328\",\"524288\":\"038ac10de9f1ff9395903bb73077e94dbf91e9ef98fd77d9a2debc5f74c575bc86\",\"1048576\":\"0203eaee4db749b0fc7c49870d082024b2c31d889f9bc3b32473d4f1dfa3625788\",\"2097152\":\"033cdb9d36e1e82ae652b7b6a08e0204569ec7ff9ebf85d80a02786dc7fe00b04c\",\"4194304\":\"02c8b73f4e3a470ae05e5f2fe39984d41e9f6ae7be9f3b09c9ac31292e403ac512\",\"8388608\":\"025bbe0cfce8a1f4fbd7f3a0d4a09cb6badd73ef61829dc827aa8a98c270bc25b0\",\"16777216\":\"037eec3d1651a30a90182d9287a5c51386fe35d4a96839cf7969c6e2a03db1fc21\",\"33554432\":\"03280576b81a04e6abd7197f305506476f5751356b7643988495ca5c3e14e5c262\",\"67108864\":\"03268bfb05be1dbb33ab6e7e00e438373ca2c9b9abc018fdb452d0e1a0935e10d3\",\"134217728\":\"02573b68784ceba9617bbcc7c9487836d296aa7c628c3199173a841e7a19798020\",\"268435456\":\"0234076b6e70f7fbf755d2227ecc8d8169d662518ee3a1401f729e2a12ccb2b276\",\"536870912\":\"03015bd88961e2a466a2163bd4248d1d2b42c7c58a157e594785e7eb34d880efc9\",\"1073741824\":\"02c9b076d08f9020ebee49ac8ba2610b404d4e553a4f800150ceb539e9421aaeee\",\"2147483648\":\"034d592f4c366afddc919a509600af81b489a03caf4f7517c2b3f4f2b558f9a41a\",\"4294967296\":\"037c09ecb66da082981e4cbdb1ac65c0eb631fc75d85bed13efb2c6364148879b5\",\"8589934592\":\"02b4ebb0dda3b9ad83b39e2e31024b777cc0ac205a96b9a6cfab3edea2912ed1b3\",\"17179869184\":\"026cc4dacdced45e63f6e4f62edbc5779ccd802e7fabb82d5123db879b636176e9\",\"34359738368\":\"02b2cee01b7d8e90180254459b8f09bbea9aad34c3a2fd98c85517ecfc9805af75\",\"68719476736\":\"037a0c0d564540fc574b8bfa0253cca987b75466e44b295ed59f6f8bd41aace754\",\"137438953472\":\"021df6585cae9b9ca431318a713fd73dbb76b3ef5667957e8633bca8aaa7214fb6\",\"274877906944\":\"02b8f53dde126f8c85fa5bb6061c0be5aca90984ce9b902966941caf963648d53a\",\"549755813888\":\"029cc8af2840d59f1d8761779b2496623c82c64be8e15f9ab577c657c6dd453785\",\"1099511627776\":\"03e446fdb84fad492ff3a25fc1046fb9a93a5b262ebcd0151caa442ea28959a38a\",\"2199023255552\":\"02d6b25bd4ab599dd0818c55f75702fde603c93f259222001246569018842d3258\",\"4398046511104\":\"03397b522bb4e156ec3952d3f048e5a986c20a00718e5e52cd5718466bf494156a\",\"8796093022208\":\"02d1fb9e78262b5d7d74028073075b80bb5ab281edcfc3191061962c1346340f1e\",\"17592186044416\":\"030d3f2ad7a4ca115712ff7f140434f802b19a4c9b2dd1c76f3e8e80c05c6a9310\",\"35184372088832\":\"03e325b691f292e1dfb151c3fb7cad440b225795583c32e24e10635a80e4221c06\",\"70368744177664\":\"03bee8f64d88de3dee21d61f89efa32933da51152ddbd67466bef815e9f93f8fd1\",\"140737488355328\":\"0327244c9019a4892e1f04ba3bf95fe43b327479e2d57c25979446cc508cd379ed\",\"281474976710656\":\"02fb58522cd662f2f8b042f8161caae6e45de98283f74d4e99f19b0ea85e08a56d\",\"562949953421312\":\"02adde4b466a9d7e59386b6a701a39717c53f30c4810613c1b55e6b6da43b7bc9a\",\"1125899906842624\":\"038eeda11f78ce05c774f30e393cda075192b890d68590813ff46362548528dca9\",\"2251799813685248\":\"02ec13e0058b196db80f7079d329333b330dc30c000dbdd7397cbbc5a37a664c4f\",\"4503599627370496\":\"02d2d162db63675bd04f7d56df04508840f41e2ad87312a3c93041b494efe80a73\",\"9007199254740992\":\"0356969d6aef2bb40121dbd07c68b6102339f4ea8e674a9008bb69506795998f49\",\"18014398509481984\":\"02f4e667567ebb9f4e6e180a4113bb071c48855f657766bb5e9c776a880335d1d6\",\"36028797018963968\":\"0385b4fe35e41703d7a657d957c67bb536629de57b7e6ee6fe2130728ef0fc90b0\",\"72057594037927936\":\"02b2bc1968a6fddbcc78fb9903940524824b5f5bed329c6ad48a19b56068c144fd\",\"144115188075855872\":\"02e0dbb24f1d288a693e8a49bc14264d1276be16972131520cf9e055ae92fba19a\",\"288230376151711744\":\"03efe75c106f931a525dc2d653ebedddc413a2c7d8cb9da410893ae7d2fa7d19cc\",\"576460752303423488\":\"02c7ec2bd9508a7fc03f73c7565dc600b30fd86f3d305f8f139c45c404a52d958a\",\"1152921504606846976\":\"035a6679c6b25e68ff4e29d1c7ef87f21e0a8fc574f6a08c1aa45ff352c1d59f06\",\"2305843009213693952\":\"033cdc225962c052d485f7cfbf55a5b2367d200fe1fe4373a347deb4cc99e9a099\",\"4611686018427387904\":\"024a4b806cf413d14b294719090a9da36ba75209c7657135ad09bc65328fba9e6f\",\"9223372036854775808\":\"0377a6fe114e291a8d8e991627c38001c8305b23b9e98b1c7b1893f5cd0dda6cad\"\n}"
    )]
    public void Nut01Tests_Keysets_Valid(string keyset)
    {
        JsonSerializer.Deserialize<Keyset>(keyset);
    }

    [Theory]
    // v1
    [InlineData(
        "00456a94ab4e1c46",
        "{\n  \"1\":\"03a40f20667ed53513075dc51e715ff2046cad64eb68960632269ba7f0210e38bc\",\"2\":\"03fd4ce5a16b65576145949e6f99f445f8249fee17c606b688b504a849cdc452de\",\"4\":\"02648eccfa4c026960966276fa5a4cae46ce0fd432211a4f449bf84f13aa5f8303\",\"8\":\"02fdfd6796bfeac490cbee12f778f867f0a2c68f6508d17c649759ea0dc3547528\"\n}"
    )]
    [InlineData(
        "000f01df73ea149a",
        "{\n  \"1\":\"03ba786a2c0745f8c30e490288acd7a72dd53d65afd292ddefa326a4a3fa14c566\",\"2\":\"03361cd8bd1329fea797a6add1cf1990ffcf2270ceb9fc81eeee0e8e9c1bd0cdf5\",\"4\":\"036e378bcf78738ddf68859293c69778035740e41138ab183c94f8fee7572214c7\",\"8\":\"03909d73beaf28edfb283dbeb8da321afd40651e8902fcf5454ecc7d69788626c0\",\"16\":\"028a36f0e6638ea7466665fe174d958212723019ec08f9ce6898d897f88e68aa5d\",\"32\":\"03a97a40e146adee2687ac60c2ba2586a90f970de92a9d0e6cae5a4b9965f54612\",\"64\":\"03ce86f0c197aab181ddba0cfc5c5576e11dfd5164d9f3d4a3fc3ffbbf2e069664\",\"128\":\"0284f2c06d938a6f78794814c687560a0aabab19fe5e6f30ede38e113b132a3cb9\",\"256\":\"03b99f475b68e5b4c0ba809cdecaae64eade2d9787aa123206f91cd61f76c01459\",\"512\":\"03d4db82ea19a44d35274de51f78af0a710925fe7d9e03620b84e3e9976e3ac2eb\",\"1024\":\"031fbd4ba801870871d46cf62228a1b748905ebc07d3b210daf48de229e683f2dc\",\"2048\":\"0276cedb9a3b160db6a158ad4e468d2437f021293204b3cd4bf6247970d8aff54b\",\"4096\":\"02fc6b89b403ee9eb8a7ed457cd3973638080d6e04ca8af7307c965c166b555ea2\",\"8192\":\"0320265583e916d3a305f0d2687fcf2cd4e3cd03a16ea8261fda309c3ec5721e21\",\"16384\":\"036e41de58fdff3cb1d8d713f48c63bc61fa3b3e1631495a444d178363c0d2ed50\",\"32768\":\"0365438f613f19696264300b069d1dad93f0c60a37536b72a8ab7c7366a5ee6c04\",\"65536\":\"02408426cfb6fc86341bac79624ba8708a4376b2d92debdf4134813f866eb57a8d\",\"131072\":\"031063e9f11c94dc778c473e968966eac0e70b7145213fbaff5f7a007e71c65f41\",\"262144\":\"02f2a3e808f9cd168ec71b7f328258d0c1dda250659c1aced14c7f5cf05aab4328\",\"524288\":\"038ac10de9f1ff9395903bb73077e94dbf91e9ef98fd77d9a2debc5f74c575bc86\",\"1048576\":\"0203eaee4db749b0fc7c49870d082024b2c31d889f9bc3b32473d4f1dfa3625788\",\"2097152\":\"033cdb9d36e1e82ae652b7b6a08e0204569ec7ff9ebf85d80a02786dc7fe00b04c\",\"4194304\":\"02c8b73f4e3a470ae05e5f2fe39984d41e9f6ae7be9f3b09c9ac31292e403ac512\",\"8388608\":\"025bbe0cfce8a1f4fbd7f3a0d4a09cb6badd73ef61829dc827aa8a98c270bc25b0\",\"16777216\":\"037eec3d1651a30a90182d9287a5c51386fe35d4a96839cf7969c6e2a03db1fc21\",\"33554432\":\"03280576b81a04e6abd7197f305506476f5751356b7643988495ca5c3e14e5c262\",\"67108864\":\"03268bfb05be1dbb33ab6e7e00e438373ca2c9b9abc018fdb452d0e1a0935e10d3\",\"134217728\":\"02573b68784ceba9617bbcc7c9487836d296aa7c628c3199173a841e7a19798020\",\"268435456\":\"0234076b6e70f7fbf755d2227ecc8d8169d662518ee3a1401f729e2a12ccb2b276\",\"536870912\":\"03015bd88961e2a466a2163bd4248d1d2b42c7c58a157e594785e7eb34d880efc9\",\"1073741824\":\"02c9b076d08f9020ebee49ac8ba2610b404d4e553a4f800150ceb539e9421aaeee\",\"2147483648\":\"034d592f4c366afddc919a509600af81b489a03caf4f7517c2b3f4f2b558f9a41a\",\"4294967296\":\"037c09ecb66da082981e4cbdb1ac65c0eb631fc75d85bed13efb2c6364148879b5\",\"8589934592\":\"02b4ebb0dda3b9ad83b39e2e31024b777cc0ac205a96b9a6cfab3edea2912ed1b3\",\"17179869184\":\"026cc4dacdced45e63f6e4f62edbc5779ccd802e7fabb82d5123db879b636176e9\",\"34359738368\":\"02b2cee01b7d8e90180254459b8f09bbea9aad34c3a2fd98c85517ecfc9805af75\",\"68719476736\":\"037a0c0d564540fc574b8bfa0253cca987b75466e44b295ed59f6f8bd41aace754\",\"137438953472\":\"021df6585cae9b9ca431318a713fd73dbb76b3ef5667957e8633bca8aaa7214fb6\",\"274877906944\":\"02b8f53dde126f8c85fa5bb6061c0be5aca90984ce9b902966941caf963648d53a\",\"549755813888\":\"029cc8af2840d59f1d8761779b2496623c82c64be8e15f9ab577c657c6dd453785\",\"1099511627776\":\"03e446fdb84fad492ff3a25fc1046fb9a93a5b262ebcd0151caa442ea28959a38a\",\"2199023255552\":\"02d6b25bd4ab599dd0818c55f75702fde603c93f259222001246569018842d3258\",\"4398046511104\":\"03397b522bb4e156ec3952d3f048e5a986c20a00718e5e52cd5718466bf494156a\",\"8796093022208\":\"02d1fb9e78262b5d7d74028073075b80bb5ab281edcfc3191061962c1346340f1e\",\"17592186044416\":\"030d3f2ad7a4ca115712ff7f140434f802b19a4c9b2dd1c76f3e8e80c05c6a9310\",\"35184372088832\":\"03e325b691f292e1dfb151c3fb7cad440b225795583c32e24e10635a80e4221c06\",\"70368744177664\":\"03bee8f64d88de3dee21d61f89efa32933da51152ddbd67466bef815e9f93f8fd1\",\"140737488355328\":\"0327244c9019a4892e1f04ba3bf95fe43b327479e2d57c25979446cc508cd379ed\",\"281474976710656\":\"02fb58522cd662f2f8b042f8161caae6e45de98283f74d4e99f19b0ea85e08a56d\",\"562949953421312\":\"02adde4b466a9d7e59386b6a701a39717c53f30c4810613c1b55e6b6da43b7bc9a\",\"1125899906842624\":\"038eeda11f78ce05c774f30e393cda075192b890d68590813ff46362548528dca9\",\"2251799813685248\":\"02ec13e0058b196db80f7079d329333b330dc30c000dbdd7397cbbc5a37a664c4f\",\"4503599627370496\":\"02d2d162db63675bd04f7d56df04508840f41e2ad87312a3c93041b494efe80a73\",\"9007199254740992\":\"0356969d6aef2bb40121dbd07c68b6102339f4ea8e674a9008bb69506795998f49\",\"18014398509481984\":\"02f4e667567ebb9f4e6e180a4113bb071c48855f657766bb5e9c776a880335d1d6\",\"36028797018963968\":\"0385b4fe35e41703d7a657d957c67bb536629de57b7e6ee6fe2130728ef0fc90b0\",\"72057594037927936\":\"02b2bc1968a6fddbcc78fb9903940524824b5f5bed329c6ad48a19b56068c144fd\",\"144115188075855872\":\"02e0dbb24f1d288a693e8a49bc14264d1276be16972131520cf9e055ae92fba19a\",\"288230376151711744\":\"03efe75c106f931a525dc2d653ebedddc413a2c7d8cb9da410893ae7d2fa7d19cc\",\"576460752303423488\":\"02c7ec2bd9508a7fc03f73c7565dc600b30fd86f3d305f8f139c45c404a52d958a\",\"1152921504606846976\":\"035a6679c6b25e68ff4e29d1c7ef87f21e0a8fc574f6a08c1aa45ff352c1d59f06\",\"2305843009213693952\":\"033cdc225962c052d485f7cfbf55a5b2367d200fe1fe4373a347deb4cc99e9a099\",\"4611686018427387904\":\"024a4b806cf413d14b294719090a9da36ba75209c7657135ad09bc65328fba9e6f\",\"9223372036854775808\":\"0377a6fe114e291a8d8e991627c38001c8305b23b9e98b1c7b1893f5cd0dda6cad\"\n}"
    )]
    // v2
    [InlineData(
        "015ba18a8adcd02e715a58358eb618da4a4b3791151a4bee5e968bb88406ccf76a",
        "{\n  \"1\": \"03a40f20667ed53513075dc51e715ff2046cad64eb68960632269ba7f0210e38bc\",\n  \"2\": \"03fd4ce5a16b65576145949e6f99f445f8249fee17c606b688b504a849cdc452de\",\n  \"4\": \"02648eccfa4c026960966276fa5a4cae46ce0fd432211a4f449bf84f13aa5f8303\",\n  \"8\": \"02fdfd6796bfeac490cbee12f778f867f0a2c68f6508d17c649759ea0dc3547528\"\n}",
        (byte)1,
        "sat",
        100UL,
        2059210353
    )]
    [InlineData(
        "01ab6aa4ff30390da34986d84be5274b48ad7a74265d791095bfc39f4098d9764f",
        "{\n  \"1\": \"03ba786a2c0745f8c30e490288acd7a72dd53d65afd292ddefa326a4a3fa14c566\",\n  \"2\": \"03361cd8bd1329fea797a6add1cf1990ffcf2270ceb9fc81eeee0e8e9c1bd0cdf5\",\n  \"4\": \"036e378bcf78738ddf68859293c69778035740e41138ab183c94f8fee7572214c7\",\n  \"8\": \"03909d73beaf28edfb283dbeb8da321afd40651e8902fcf5454ecc7d69788626c0\",\n  \"16\": \"028a36f0e6638ea7466665fe174d958212723019ec08f9ce6898d897f88e68aa5d\",\n  \"32\": \"03a97a40e146adee2687ac60c2ba2586a90f970de92a9d0e6cae5a4b9965f54612\",\n  \"64\": \"03ce86f0c197aab181ddba0cfc5c5576e11dfd5164d9f3d4a3fc3ffbbf2e069664\",\n  \"128\": \"0284f2c06d938a6f78794814c687560a0aabab19fe5e6f30ede38e113b132a3cb9\",\n  \"256\": \"03b99f475b68e5b4c0ba809cdecaae64eade2d9787aa123206f91cd61f76c01459\",\n  \"512\": \"03d4db82ea19a44d35274de51f78af0a710925fe7d9e03620b84e3e9976e3ac2eb\",\n  \"1024\": \"031fbd4ba801870871d46cf62228a1b748905ebc07d3b210daf48de229e683f2dc\",\n  \"2048\": \"0276cedb9a3b160db6a158ad4e468d2437f021293204b3cd4bf6247970d8aff54b\",\n  \"4096\": \"02fc6b89b403ee9eb8a7ed457cd3973638080d6e04ca8af7307c965c166b555ea2\",\n  \"8192\": \"0320265583e916d3a305f0d2687fcf2cd4e3cd03a16ea8261fda309c3ec5721e21\",\n  \"16384\": \"036e41de58fdff3cb1d8d713f48c63bc61fa3b3e1631495a444d178363c0d2ed50\",\n  \"32768\": \"0365438f613f19696264300b069d1dad93f0c60a37536b72a8ab7c7366a5ee6c04\",\n  \"65536\": \"02408426cfb6fc86341bac79624ba8708a4376b2d92debdf4134813f866eb57a8d\",\n  \"131072\": \"031063e9f11c94dc778c473e968966eac0e70b7145213fbaff5f7a007e71c65f41\",\n  \"262144\": \"02f2a3e808f9cd168ec71b7f328258d0c1dda250659c1aced14c7f5cf05aab4328\",\n  \"524288\": \"038ac10de9f1ff9395903bb73077e94dbf91e9ef98fd77d9a2debc5f74c575bc86\",\n  \"1048576\": \"0203eaee4db749b0fc7c49870d082024b2c31d889f9bc3b32473d4f1dfa3625788\",\n  \"2097152\": \"033cdb9d36e1e82ae652b7b6a08e0204569ec7ff9ebf85d80a02786dc7fe00b04c\",\n  \"4194304\": \"02c8b73f4e3a470ae05e5f2fe39984d41e9f6ae7be9f3b09c9ac31292e403ac512\",\n  \"8388608\": \"025bbe0cfce8a1f4fbd7f3a0d4a09cb6badd73ef61829dc827aa8a98c270bc25b0\",\n  \"16777216\": \"037eec3d1651a30a90182d9287a5c51386fe35d4a96839cf7969c6e2a03db1fc21\",\n  \"33554432\": \"03280576b81a04e6abd7197f305506476f5751356b7643988495ca5c3e14e5c262\",\n  \"67108864\": \"03268bfb05be1dbb33ab6e7e00e438373ca2c9b9abc018fdb452d0e1a0935e10d3\",\n  \"134217728\": \"02573b68784ceba9617bbcc7c9487836d296aa7c628c3199173a841e7a19798020\",\n  \"268435456\": \"0234076b6e70f7fbf755d2227ecc8d8169d662518ee3a1401f729e2a12ccb2b276\",\n  \"536870912\": \"03015bd88961e2a466a2163bd4248d1d2b42c7c58a157e594785e7eb34d880efc9\",\n  \"1073741824\": \"02c9b076d08f9020ebee49ac8ba2610b404d4e553a4f800150ceb539e9421aaeee\",\n  \"2147483648\": \"034d592f4c366afddc919a509600af81b489a03caf4f7517c2b3f4f2b558f9a41a\",\n  \"4294967296\": \"037c09ecb66da082981e4cbdb1ac65c0eb631fc75d85bed13efb2c6364148879b5\",\n  \"8589934592\": \"02b4ebb0dda3b9ad83b39e2e31024b777cc0ac205a96b9a6cfab3edea2912ed1b3\",\n  \"17179869184\": \"026cc4dacdced45e63f6e4f62edbc5779ccd802e7fabb82d5123db879b636176e9\",\n  \"34359738368\": \"02b2cee01b7d8e90180254459b8f09bbea9aad34c3a2fd98c85517ecfc9805af75\",\n  \"68719476736\": \"037a0c0d564540fc574b8bfa0253cca987b75466e44b295ed59f6f8bd41aace754\",\n  \"137438953472\": \"021df6585cae9b9ca431318a713fd73dbb76b3ef5667957e8633bca8aaa7214fb6\",\n  \"274877906944\": \"02b8f53dde126f8c85fa5bb6061c0be5aca90984ce9b902966941caf963648d53a\",\n  \"549755813888\": \"029cc8af2840d59f1d8761779b2496623c82c64be8e15f9ab577c657c6dd453785\",\n  \"1099511627776\": \"03e446fdb84fad492ff3a25fc1046fb9a93a5b262ebcd0151caa442ea28959a38a\",\n  \"2199023255552\": \"02d6b25bd4ab599dd0818c55f75702fde603c93f259222001246569018842d3258\",\n  \"4398046511104\": \"03397b522bb4e156ec3952d3f048e5a986c20a00718e5e52cd5718466bf494156a\",\n  \"8796093022208\": \"02d1fb9e78262b5d7d74028073075b80bb5ab281edcfc3191061962c1346340f1e\",\n  \"17592186044416\": \"030d3f2ad7a4ca115712ff7f140434f802b19a4c9b2dd1c76f3e8e80c05c6a9310\",\n  \"35184372088832\": \"03e325b691f292e1dfb151c3fb7cad440b225795583c32e24e10635a80e4221c06\",\n  \"70368744177664\": \"03bee8f64d88de3dee21d61f89efa32933da51152ddbd67466bef815e9f93f8fd1\",\n  \"140737488355328\": \"0327244c9019a4892e1f04ba3bf95fe43b327479e2d57c25979446cc508cd379ed\",\n  \"281474976710656\": \"02fb58522cd662f2f8b042f8161caae6e45de98283f74d4e99f19b0ea85e08a56d\",\n  \"562949953421312\": \"02adde4b466a9d7e59386b6a701a39717c53f30c4810613c1b55e6b6da43b7bc9a\",\n  \"1125899906842624\": \"038eeda11f78ce05c774f30e393cda075192b890d68590813ff46362548528dca9\",\n  \"2251799813685248\": \"02ec13e0058b196db80f7079d329333b330dc30c000dbdd7397cbbc5a37a664c4f\",\n  \"4503599627370496\": \"02d2d162db63675bd04f7d56df04508840f41e2ad87312a3c93041b494efe80a73\",\n  \"9007199254740992\": \"0356969d6aef2bb40121dbd07c68b6102339f4ea8e674a9008bb69506795998f49\",\n  \"18014398509481984\": \"02f4e667567ebb9f4e6e180a4113bb071c48855f657766bb5e9c776a880335d1d6\",\n  \"36028797018963968\": \"0385b4fe35e41703d7a657d957c67bb536629de57b7e6ee6fe2130728ef0fc90b0\",\n  \"72057594037927936\": \"02b2bc1968a6fddbcc78fb9903940524824b5f5bed329c6ad48a19b56068c144fd\",\n  \"144115188075855872\": \"02e0dbb24f1d288a693e8a49bc14264d1276be16972131520cf9e055ae92fba19a\",\n  \"288230376151711744\": \"03efe75c106f931a525dc2d653ebedddc413a2c7d8cb9da410893ae7d2fa7d19cc\",\n  \"576460752303423488\": \"02c7ec2bd9508a7fc03f73c7565dc600b30fd86f3d305f8f139c45c404a52d958a\",\n  \"1152921504606846976\": \"035a6679c6b25e68ff4e29d1c7ef87f21e0a8fc574f6a08c1aa45ff352c1d59f06\",\n  \"2305843009213693952\": \"033cdc225962c052d485f7cfbf55a5b2367d200fe1fe4373a347deb4cc99e9a099\",\n  \"4611686018427387904\": \"024a4b806cf413d14b294719090a9da36ba75209c7657135ad09bc65328fba9e6f\",\n  \"9223372036854775808\": \"0377a6fe114e291a8d8e991627c38001c8305b23b9e98b1c7b1893f5cd0dda6cad\"\n}",
        (byte)0x01,
        "sat",
        0UL,
        2059210353
    )]
    [InlineData(
        "012fbb01a4e200c76df911eeba3b8fe1831202914b24664f4bccbd25852a6708f8",
        "{\n  \"1\": \"03ba786a2c0745f8c30e490288acd7a72dd53d65afd292ddefa326a4a3fa14c566\",\n  \"2\": \"03361cd8bd1329fea797a6add1cf1990ffcf2270ceb9fc81eeee0e8e9c1bd0cdf5\",\n  \"4\": \"036e378bcf78738ddf68859293c69778035740e41138ab183c94f8fee7572214c7\",\n  \"8\": \"03909d73beaf28edfb283dbeb8da321afd40651e8902fcf5454ecc7d69788626c0\",\n  \"16\": \"028a36f0e6638ea7466665fe174d958212723019ec08f9ce6898d897f88e68aa5d\",\n  \"32\": \"03a97a40e146adee2687ac60c2ba2586a90f970de92a9d0e6cae5a4b9965f54612\",\n  \"64\": \"03ce86f0c197aab181ddba0cfc5c5576e11dfd5164d9f3d4a3fc3ffbbf2e069664\",\n  \"128\": \"0284f2c06d938a6f78794814c687560a0aabab19fe5e6f30ede38e113b132a3cb9\",\n  \"256\": \"03b99f475b68e5b4c0ba809cdecaae64eade2d9787aa123206f91cd61f76c01459\",\n  \"512\": \"03d4db82ea19a44d35274de51f78af0a710925fe7d9e03620b84e3e9976e3ac2eb\",\n  \"1024\": \"031fbd4ba801870871d46cf62228a1b748905ebc07d3b210daf48de229e683f2dc\",\n  \"2048\": \"0276cedb9a3b160db6a158ad4e468d2437f021293204b3cd4bf6247970d8aff54b\",\n  \"4096\": \"02fc6b89b403ee9eb8a7ed457cd3973638080d6e04ca8af7307c965c166b555ea2\",\n  \"8192\": \"0320265583e916d3a305f0d2687fcf2cd4e3cd03a16ea8261fda309c3ec5721e21\",\n  \"16384\": \"036e41de58fdff3cb1d8d713f48c63bc61fa3b3e1631495a444d178363c0d2ed50\",\n  \"32768\": \"0365438f613f19696264300b069d1dad93f0c60a37536b72a8ab7c7366a5ee6c04\",\n  \"65536\": \"02408426cfb6fc86341bac79624ba8708a4376b2d92debdf4134813f866eb57a8d\",\n  \"131072\": \"031063e9f11c94dc778c473e968966eac0e70b7145213fbaff5f7a007e71c65f41\",\n  \"262144\": \"02f2a3e808f9cd168ec71b7f328258d0c1dda250659c1aced14c7f5cf05aab4328\",\n  \"524288\": \"038ac10de9f1ff9395903bb73077e94dbf91e9ef98fd77d9a2debc5f74c575bc86\",\n  \"1048576\": \"0203eaee4db749b0fc7c49870d082024b2c31d889f9bc3b32473d4f1dfa3625788\",\n  \"2097152\": \"033cdb9d36e1e82ae652b7b6a08e0204569ec7ff9ebf85d80a02786dc7fe00b04c\",\n  \"4194304\": \"02c8b73f4e3a470ae05e5f2fe39984d41e9f6ae7be9f3b09c9ac31292e403ac512\",\n  \"8388608\": \"025bbe0cfce8a1f4fbd7f3a0d4a09cb6badd73ef61829dc827aa8a98c270bc25b0\",\n  \"16777216\": \"037eec3d1651a30a90182d9287a5c51386fe35d4a96839cf7969c6e2a03db1fc21\",\n  \"33554432\": \"03280576b81a04e6abd7197f305506476f5751356b7643988495ca5c3e14e5c262\",\n  \"67108864\": \"03268bfb05be1dbb33ab6e7e00e438373ca2c9b9abc018fdb452d0e1a0935e10d3\",\n  \"134217728\": \"02573b68784ceba9617bbcc7c9487836d296aa7c628c3199173a841e7a19798020\",\n  \"268435456\": \"0234076b6e70f7fbf755d2227ecc8d8169d662518ee3a1401f729e2a12ccb2b276\",\n  \"536870912\": \"03015bd88961e2a466a2163bd4248d1d2b42c7c58a157e594785e7eb34d880efc9\",\n  \"1073741824\": \"02c9b076d08f9020ebee49ac8ba2610b404d4e553a4f800150ceb539e9421aaeee\",\n  \"2147483648\": \"034d592f4c366afddc919a509600af81b489a03caf4f7517c2b3f4f2b558f9a41a\",\n  \"4294967296\": \"037c09ecb66da082981e4cbdb1ac65c0eb631fc75d85bed13efb2c6364148879b5\",\n  \"8589934592\": \"02b4ebb0dda3b9ad83b39e2e31024b777cc0ac205a96b9a6cfab3edea2912ed1b3\",\n  \"17179869184\": \"026cc4dacdced45e63f6e4f62edbc5779ccd802e7fabb82d5123db879b636176e9\",\n  \"34359738368\": \"02b2cee01b7d8e90180254459b8f09bbea9aad34c3a2fd98c85517ecfc9805af75\",\n  \"68719476736\": \"037a0c0d564540fc574b8bfa0253cca987b75466e44b295ed59f6f8bd41aace754\",\n  \"137438953472\": \"021df6585cae9b9ca431318a713fd73dbb76b3ef5667957e8633bca8aaa7214fb6\",\n  \"274877906944\": \"02b8f53dde126f8c85fa5bb6061c0be5aca90984ce9b902966941caf963648d53a\",\n  \"549755813888\": \"029cc8af2840d59f1d8761779b2496623c82c64be8e15f9ab577c657c6dd453785\",\n  \"1099511627776\": \"03e446fdb84fad492ff3a25fc1046fb9a93a5b262ebcd0151caa442ea28959a38a\",\n  \"2199023255552\": \"02d6b25bd4ab599dd0818c55f75702fde603c93f259222001246569018842d3258\",\n  \"4398046511104\": \"03397b522bb4e156ec3952d3f048e5a986c20a00718e5e52cd5718466bf494156a\",\n  \"8796093022208\": \"02d1fb9e78262b5d7d74028073075b80bb5ab281edcfc3191061962c1346340f1e\",\n  \"17592186044416\": \"030d3f2ad7a4ca115712ff7f140434f802b19a4c9b2dd1c76f3e8e80c05c6a9310\",\n  \"35184372088832\": \"03e325b691f292e1dfb151c3fb7cad440b225795583c32e24e10635a80e4221c06\",\n  \"70368744177664\": \"03bee8f64d88de3dee21d61f89efa32933da51152ddbd67466bef815e9f93f8fd1\",\n  \"140737488355328\": \"0327244c9019a4892e1f04ba3bf95fe43b327479e2d57c25979446cc508cd379ed\",\n  \"281474976710656\": \"02fb58522cd662f2f8b042f8161caae6e45de98283f74d4e99f19b0ea85e08a56d\",\n  \"562949953421312\": \"02adde4b466a9d7e59386b6a701a39717c53f30c4810613c1b55e6b6da43b7bc9a\",\n  \"1125899906842624\": \"038eeda11f78ce05c774f30e393cda075192b890d68590813ff46362548528dca9\",\n  \"2251799813685248\": \"02ec13e0058b196db80f7079d329333b330dc30c000dbdd7397cbbc5a37a664c4f\",\n  \"4503599627370496\": \"02d2d162db63675bd04f7d56df04508840f41e2ad87312a3c93041b494efe80a73\",\n  \"9007199254740992\": \"0356969d6aef2bb40121dbd07c68b6102339f4ea8e674a9008bb69506795998f49\",\n  \"18014398509481984\": \"02f4e667567ebb9f4e6e180a4113bb071c48855f657766bb5e9c776a880335d1d6\",\n  \"36028797018963968\": \"0385b4fe35e41703d7a657d957c67bb536629de57b7e6ee6fe2130728ef0fc90b0\",\n  \"72057594037927936\": \"02b2bc1968a6fddbcc78fb9903940524824b5f5bed329c6ad48a19b56068c144fd\",\n  \"144115188075855872\": \"02e0dbb24f1d288a693e8a49bc14264d1276be16972131520cf9e055ae92fba19a\",\n  \"288230376151711744\": \"03efe75c106f931a525dc2d653ebedddc413a2c7d8cb9da410893ae7d2fa7d19cc\",\n  \"576460752303423488\": \"02c7ec2bd9508a7fc03f73c7565dc600b30fd86f3d305f8f139c45c404a52d958a\",\n  \"1152921504606846976\": \"035a6679c6b25e68ff4e29d1c7ef87f21e0a8fc574f6a08c1aa45ff352c1d59f06\",\n  \"2305843009213693952\": \"033cdc225962c052d485f7cfbf55a5b2367d200fe1fe4373a347deb4cc99e9a099\",\n  \"4611686018427387904\": \"024a4b806cf413d14b294719090a9da36ba75209c7657135ad09bc65328fba9e6f\",\n  \"9223372036854775808\": \"0377a6fe114e291a8d8e991627c38001c8305b23b9e98b1c7b1893f5cd0dda6cad\"\n}",
        (byte)0x01,
        "sat",
        0UL
    )]
    public void Nut02Tests_KeysetIdMatch(
        string keysetId,
        string keyset,
        byte? version = null,
        string? unit = null,
        ulong? inputFee = null,
        ulong? finalExpiration = null
    )
    {
        var keysetIdParsed = new KeysetId(keysetId);
        var keysetParsed = JsonSerializer.Deserialize<Keyset>(keyset);
        Assert.Equal(
            keysetIdParsed,
            keysetParsed.GetKeysetId(version ?? 0x00, unit, inputFee, finalExpiration)
        );
    }

    [Theory]
    [InlineData("00456a94ab4e1c46", 0x00)]
    [InlineData("000f01df73ea149a", 0x00)]
    [InlineData("01adc013fa9d85171586660abab27579888611659d357bc86bc09cb26eee8bc035", 0x01)]
    public void Nut02Tests_KeysetIdVersion(string keysetId, byte version)
    {
        var keysetIdParsed = new KeysetId(keysetId);
        Assert.Equal(version, keysetIdParsed.GetVersion());
    }

    [Fact]
    public void Nut04Tests_Proofs_1()
    {
        var a = "0000000000000000000000000000000000000000000000000000000000000001".ToPrivKey();
        var A = a.CreatePubKey();
        Assert.Equal(
            "0279be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798".ToPubKey(),
            A
        );
        var message = new StringSecret("secret_msg");
        var blindingFactor =
            "0000000000000000000000000000000000000000000000000000000000000001".ToPrivKey();
        // var Y = Cashu.MessageToCurve(message);
        var Y = message.ToCurve();
        var B_ = Cashu.ComputeB_(Y, blindingFactor);
        var C_ = Cashu.ComputeC_(B_, a);
        //p doesn;t have to be blinding factor. in fact it should be random nonce

        var proof = Cashu.ComputeProof(B_, a, blindingFactor);
        Cashu.VerifyProof(B_, C_, proof.e, proof.s, A);
        var C = Cashu.ComputeC(C_, blindingFactor, A);

        Cashu.VerifyProof(Y, blindingFactor, C, proof.e, proof.s, A);
    }

    [Fact]
    public void Nut04Tests_Proofs_2()
    {
        var A = "0279be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798".ToPubKey();
        var proof = JsonSerializer.Deserialize<Proof>(
            @"

        {
            ""amount"": 1,
            ""id"": ""00882760bfa2eb41"",
            ""secret"": ""daf4dd00a2b68a0858a80450f52c8a7d2ccf87d375e43e216e0c571f089f63e9"",
            ""C"": ""024369d2d22a80ecf78f3937da9d5f30c1b9f74f0c32684d583cca0fa6a61cdcfc"",
            ""dleq"": {
                ""e"": ""b31e58ac6527f34975ffab13e70a48b6d2b0d35abc4b03f0151f09ee1a9763d4"",
                ""s"": ""8fbae004c59e754d71df67e392b6ae4e29293113ddc2ec86592a0431d16306d8"",
                ""r"": ""a6d13fcd7a18442e6076f5e1e7c887ad5de40a019824bdfa9fe740d302e8d861""
            }
        }

"
        );

        Assert.NotNull(proof?.DLEQ);
        Cashu.VerifyProof(
            Cashu.HexToCurve(Assert.IsType<StringSecret>(proof.Secret).Secret),
            proof.DLEQ.R,
            proof.C,
            proof.DLEQ.E,
            proof.DLEQ.S,
            A
        );
    }

    [Fact]
    public void Nut11_Signatures()
    {
        var secretKey = ECPrivKey.Create(
            Convert.FromHexString(
                "99590802251e78ee1051648439eedb003dc539093a48a44e7b8f2642c909ea37"
            )
        );

        var signing_key_two = ECPrivKey.Create(
            Convert.FromHexString(
                "0000000000000000000000000000000000000000000000000000000000000001"
            )
        );

        var signing_key_three = ECPrivKey.Create(
            Convert.FromHexString(
                "7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f"
            )
        );

        var conditions = new P2PKBuilder
        {
            Lock = DateTimeOffset.FromUnixTimeSeconds(21000000000),
            Pubkeys = new[] { signing_key_two.CreatePubKey(), signing_key_three.CreatePubKey() },
            RefundPubkeys = new[] { secretKey.CreatePubKey() },
            SignatureThreshold = 2,
            SigFlag = "SIG_INPUTS",
        };
        var p2pkProofSecret = conditions.Build();

        var secret = new Nut10Secret(P2PKProofSecret.Key, p2pkProofSecret);

        var proof = new Proof()
        {
            Id = new KeysetId("009a1f293253e41e"),
            Amount = 0,
            Secret = secret,
            C = "02698c4e2b5f9534cd0687d87513c759790cf829aa5739184a3e3735471fbda904".ToPubKey(),
        };
        var witness = p2pkProofSecret.GenerateWitness(
            proof,
            new[] { signing_key_two, signing_key_three }
        );
        proof.Witness = JsonSerializer.Serialize(witness);
        Assert.True(p2pkProofSecret.VerifyWitness(proof.Secret, witness));

        
        // SIG_INPUTS

        var valid1 =
            "{\"amount\":1,\"secret\":\"[\\\"P2PK\\\",{\\\"nonce\\\":\\\"859d4935c4907062a6297cf4e663e2835d90d97ecdd510745d32f6816323a41f\\\",\\\"data\\\":\\\"0249098aa8b9d2fbec49ff8598feb17b592b986e62319a4fa488a3dc36387157a7\\\",\\\"tags\\\":[[\\\"sigflag\\\",\\\"SIG_INPUTS\\\"]]}]\",\"C\":\"02698c4e2b5f9534cd0687d87513c759790cf829aa5739184a3e3735471fbda904\",\"id\":\"009a1f293253e41e\",\"witness\":\"{\\\"signatures\\\":[\\\"60f3c9b766770b46caac1d27e1ae6b77c8866ebaeba0b9489fe6a15a837eaa6fcd6eaa825499c72ac342983983fd3ba3a8a41f56677cc99ffd73da68b59e1383\\\"]}\"}";
        var valid1Proof = JsonSerializer.Deserialize<Proof>(valid1);
        var valid1ProofSecret = Assert.IsType<Nut10Secret>(valid1Proof.Secret);
        Assert.Equal(P2PKProofSecret.Key, valid1ProofSecret!.Key);
        var valid1ProofSecretp2pkValue = Assert.IsType<P2PKProofSecret>(
            valid1ProofSecret.ProofSecret
        );
        var valid1ProofWitnessP2pk = JsonSerializer.Deserialize<P2PKWitness>(valid1Proof.Witness);
        Assert.True(
            valid1ProofSecretp2pkValue.VerifyWitness(valid1Proof.Secret, valid1ProofWitnessP2pk)
        );

        var invalid1 =
            "{\n  \"amount\": 1,\n  \"secret\": \"[\\\"P2PK\\\",{\\\"nonce\\\":\\\"859d4935c4907062a6297cf4e663e2835d90d97ecdd510745d32f6816323a41f\\\",\\\"data\\\":\\\"0249098aa8b9d2fbec49ff8598feb17b592b986e62319a4fa488a3dc36387157a7\\\",\\\"tags\\\":[[\\\"sigflag\\\",\\\"SIG_INPUTS\\\"]]}]\",\n  \"C\": \"02698c4e2b5f9534cd0687d87513c759790cf829aa5739184a3e3735471fbda904\",\n  \"id\": \"009a1f293253e41e\",\n  \"witness\": \"{\\\"signatures\\\":[\\\"83564aca48c668f50d022a426ce0ed19d3a9bdcffeeaee0dc1e7ea7e98e9eff1840fcc821724f623468c94f72a8b0a7280fa9ef5a54a1b130ef3055217f467b3\\\"]}\"\n}";
        var invalid1Proof = JsonSerializer.Deserialize<Proof>(invalid1);
        var invalid1ProofSecret = Assert.IsType<Nut10Secret>(invalid1Proof.Secret);
        Assert.Equal(P2PKProofSecret.Key, invalid1ProofSecret!.Key);
        var invalid1ProofSecretp2pkValue = Assert.IsType<P2PKProofSecret>(
            invalid1ProofSecret.ProofSecret
        );
        var invalid1ProofWitnessP2pk = JsonSerializer.Deserialize<P2PKWitness>(
            invalid1Proof.Witness
        );
        Assert.False(
            invalid1ProofSecretp2pkValue.VerifyWitness(
                invalid1Proof.Secret,
                invalid1ProofWitnessP2pk
            )
        );

        var validMultisig =
            "{\"amount\":1,\"secret\":\"[\\\"P2PK\\\",{\\\"nonce\\\":\\\"0ed3fcb22c649dd7bbbdcca36e0c52d4f0187dd3b6a19efcc2bfbebb5f85b2a1\\\",\\\"data\\\":\\\"0249098aa8b9d2fbec49ff8598feb17b592b986e62319a4fa488a3dc36387157a7\\\",\\\"tags\\\":[[\\\"pubkeys\\\",\\\"0279be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798\\\",\\\"02142715675faf8da1ecc4d51e0b9e539fa0d52fdd96ed60dbe99adb15d6b05ad9\\\"],[\\\"n_sigs\\\",\\\"2\\\"],[\\\"sigflag\\\",\\\"SIG_INPUTS\\\"]]}]\",\"C\":\"02698c4e2b5f9534cd0687d87513c759790cf829aa5739184a3e3735471fbda904\",\"id\":\"009a1f293253e41e\",\"witness\":\"{\\\"signatures\\\":[\\\"83564aca48c668f50d022a426ce0ed19d3a9bdcffeeaee0dc1e7ea7e98e9eff1840fcc821724f623468c94f72a8b0a7280fa9ef5a54a1b130ef3055217f467b3\\\",\\\"9a72ca2d4d5075be5b511ee48dbc5e45f259bcf4a4e8bf18587f433098a9cd61ff9737dc6e8022de57c76560214c4568377792d4c2c6432886cc7050487a1f22\\\"]}\"}";
        var validMultisigProof = JsonSerializer.Deserialize<Proof>(validMultisig);
        var validMultisigProofSecret = Assert.IsType<Nut10Secret>(validMultisigProof.Secret);
        Assert.Equal(P2PKProofSecret.Key, validMultisigProofSecret!.Key);
        var validMultisigProofSecretp2pkValue = Assert.IsType<P2PKProofSecret>(
            validMultisigProofSecret.ProofSecret
        );
        var validMultisigProofWitnessP2pk = JsonSerializer.Deserialize<P2PKWitness>(
            validMultisigProof.Witness
        );
        Assert.True(
            validMultisigProofSecretp2pkValue.VerifyWitness(
                validMultisigProof.Secret,
                validMultisigProofWitnessP2pk
            )
        );

        var invalidMultisig =
            "{\"amount\":1,\"secret\":\"[\\\"P2PK\\\",{\\\"nonce\\\":\\\"0ed3fcb22c649dd7bbbdcca36e0c52d4f0187dd3b6a19efcc2bfbebb5f85b2a1\\\",\\\"data\\\":\\\"0249098aa8b9d2fbec49ff8598feb17b592b986e62319a4fa488a3dc36387157a7\\\",\\\"tags\\\":[[\\\"pubkeys\\\",\\\"0279be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798\\\",\\\"02142715675faf8da1ecc4d51e0b9e539fa0d52fdd96ed60dbe99adb15d6b05ad9\\\"],[\\\"n_sigs\\\",\\\"2\\\"],[\\\"sigflag\\\",\\\"SIG_INPUTS\\\"]]}]\",\"C\":\"02698c4e2b5f9534cd0687d87513c759790cf829aa5739184a3e3735471fbda904\",\"id\":\"009a1f293253e41e\",\"witness\":\"{\\\"signatures\\\":[\\\"83564aca48c668f50d022a426ce0ed19d3a9bdcffeeaee0dc1e7ea7e98e9eff1840fcc821724f623468c94f72a8b0a7280fa9ef5a54a1b130ef3055217f467b3\\\"]}\"}";
        var invalidMultisigProof = JsonSerializer.Deserialize<Proof>(invalidMultisig);
        var invalidMultisigProofSecret = Assert.IsType<Nut10Secret>(invalidMultisigProof.Secret);
        Assert.Equal(P2PKProofSecret.Key, invalidMultisigProofSecret!.Key);
        var invalidMultisigProofSecretp2pkValue = Assert.IsType<P2PKProofSecret>(
            invalidMultisigProofSecret.ProofSecret
        );
        var invalidMultisigProofWitnessP2pk = JsonSerializer.Deserialize<P2PKWitness>(
            invalidMultisigProof.Witness
        );
        Assert.False(
            invalidMultisigProofSecretp2pkValue.VerifyWitness(
                invalidMultisigProof.Secret,
                invalidMultisigProofWitnessP2pk
            )
        );

        var validProofRefund =
            "{\n  \"amount\": 64,\n  \"C\": \"0257353051c02e2d650dede3159915c8be123ba4f47cf33183c7fedd20bd91a79b\",\n  \"id\": \"001b6c716bf42c7e\",\n  \"secret\": \"[\\\"P2PK\\\",{\\\"nonce\\\":\\\"4bc88ee09d1886c7461d45da205ca3274e1e3d9da2667c4865045cb18265a407\\\",\\\"data\\\":\\\"03d5edeb839be873df2348785506d36565f3b8f390fb931709a422b5a247ddefb1\\\",\\\"tags\\\":[[\\\"locktime\\\",\\\"21\\\"],[\\\"refund\\\",\\\"0234ad87e907e117db1590cc20a3942ffdfd5137aa563d36095d5cf5f96bada122\\\"]]}]\",\n  \"witness\": \"{\\\"signatures\\\":[\\\"b316c2ff9c15f0c5c3d230e99ad94bc76a11dfccbdc820366a3db7210288f22ef6cedcded1152904ec31056d1d5176d83a2d96df5cd4ff86afdde1c90c63af5e\\\"]}\"\n}";
        var validProofRefundParsed = JsonSerializer.Deserialize<Proof>(validProofRefund);
        var validProofRefundSecret = Assert.IsType<Nut10Secret>(validProofRefundParsed.Secret);
        Assert.Equal(P2PKProofSecret.Key, validProofRefundSecret!.Key);
        var validProofRefundSecretp2pkValue = Assert.IsType<P2PKProofSecret>(
            validProofRefundSecret.ProofSecret
        );
        var validProofRefundWitnessP2pk = JsonSerializer.Deserialize<P2PKWitness>(
            validProofRefundParsed.Witness
        );
        Assert.True(
            validProofRefundSecretp2pkValue.VerifyWitness(
                validProofRefundParsed.Secret,
                validProofRefundWitnessP2pk
            )
        );

        var invalidProofRefund =
            "{\n  \"amount\": 64,\n  \"C\": \"0215865e3b30bdf6f5cdc1ee2c33379d5629bdf2eff2595603d939ff8c65d80586\",\n  \"id\": \"001b6c716bf42c7e\",\n  \"secret\": \"[\\\"P2PK\\\",{\\\"nonce\\\":\\\"0c3d085898f1abf2b5521035f4d0f4ecf68c6a5109f6bc836833a1188f06be65\\\",\\\"data\\\":\\\"03206e0d488387a816bbafd957be51b073432c6c7a403ec4c2a0b27647326c5150\\\",\\\"tags\\\":[[\\\"locktime\\\",\\\"99999999999\\\"],[\\\"refund\\\",\\\"026acbcd0fff3a424499c83ec892d3155c9d1984438659f448d9d0f1af3e92276a\\\"]]}]\",\n  \"witness\": \"{\\\"signatures\\\":[\\\"e5b10d7627ab39bd0cefa219c63752a0026aa5ae754b91a0c7ee2596222f87942c442aca2957166a6b468350c09c9968792784d2ae7c42fc91739b55689f4c7a\\\"]}\"\n}";
        var invalidProofRefundParsed = JsonSerializer.Deserialize<Proof>(invalidProofRefund);
        var invalidProofRefundSecret = Assert.IsType<Nut10Secret>(invalidProofRefundParsed.Secret);
        Assert.Equal(P2PKProofSecret.Key, invalidProofRefundSecret!.Key);
        var invalidProofRefundSecretp2pkValue = Assert.IsType<P2PKProofSecret>(
            invalidProofRefundSecret.ProofSecret
        );
        var invalidProofRefundWitnessP2pk = JsonSerializer.Deserialize<P2PKWitness>(
            invalidProofRefundParsed.Witness
        );
        Assert.False(
            invalidProofRefundSecretp2pkValue.VerifyWitness(
                invalidProofRefundParsed.Secret,
                invalidProofRefundWitnessP2pk
            )
        );
    }

    [Fact]
    public void Nut11_New_P2PkRules()
    {
        // since https://github.com/cashubtc/nuts/pull/315 p2pk and htlc behavior will be changed. After locktime, the 
        // proof will be spendable on both (refund and normal) paths.
        
        var spendableProof = 
            "{\n  \"amount\": 64,\n  \"C\": \"02d7cd858d866fca404b5cb1ffd813946e6d19efa1af00d654080fd20266bdc0b1\",\n  \"id\": \"001b6c716bf42c7e\",\n  \"secret\": \"[\\\"P2PK\\\",{\\\"nonce\\\":\\\"395162bf2d0add3c66aea9f22c45251dbee6e04bd9282addbb366a94cd4fb482\\\",\\\"data\\\":\\\"03ab50a667926fac858bac540766254c14b2b0334d10e8ec766455310224bbecf4\\\",\\\"tags\\\":[[\\\"locktime\\\",\\\"21\\\"],[\\\"pubkeys\\\",\\\"0229a91adec8dd9badb228c628a07fc1bf707a9b7d95dd505c490b1766fa7dc541\\\",\\\"033281c37677ea273eb7183b783067f5244933ef78d8c3f15b1a77cb246099c26e\\\"],[\\\"n_sigs\\\",\\\"2\\\"],[\\\"refund\\\",\\\"03ab50a667926fac858bac540766254c14b2b0334d10e8ec766455310224bbecf4\\\",\\\"033281c37677ea273eb7183b783067f5244933ef78d8c3f15b1a77cb246099c26e\\\"]]}]\"\n}";
        var spendableProofParsed = JsonSerializer.Deserialize<Proof>(spendableProof);
        Assert.NotNull(spendableProofParsed);
        var spendableProofSecret = Assert.IsType<Nut10Secret>(spendableProofParsed.Secret);
        Assert.Equal(P2PKProofSecret.Key, spendableProofSecret.Key);
        var secretValue = Assert.IsType<P2PKProofSecret>(spendableProofSecret.ProofSecret);
        
        // "standard path" witness, n_sigs = 2.
        // since locktime is expired, it would fail under old conditions. now the proof should remain spendable
        var validWitness1 =
            "{\"signatures\":[\"6a4dd46f929b4747efe7380d655be5cfc0ea943c679a409ea16d4e40968ce89de885d995937d5b85f24fa33a25df10990c5e11d5397199d779d5cf87d42f6627\",\"0c266fffe2ea2358fb93b5d30dfbcefe52a5bb53d6c85f37d54723613224a256165d20dd095768f168ab2e97bc5a879f7c2a84eee8963c9bcedcd39552dbe093\"]}";
        var validWitness1Parsed = JsonSerializer.Deserialize<P2PKWitness>(validWitness1);
        Assert.NotNull(validWitness1Parsed);
        Assert.True(secretValue.VerifyWitness(spendableProofParsed.Secret, validWitness1Parsed));

        // "refund path" witness, n_sigs_refund is omitted, so it's 1 by default
        var validWitness2 =
            "{\"signatures\":[\"d39631363480adf30433ee25c7cec28237e02b4808d4143469d4f390d4eae6ec97d18ba3cc6494ab1d04372f0838426ea296f25cb4bd8bddb296adc292eeaa96\"]}";
        var validWitness2Parsed = JsonSerializer.Deserialize<P2PKWitness>(validWitness2);
        Assert.NotNull(validWitness2Parsed);
        Assert.True(secretValue.VerifyWitness(spendableProofParsed.Secret, validWitness2Parsed));
    }

    [Fact]
    public void Nut11_SIG_ALL()
    {
        var swapRequest =
            "{\n  \"inputs\": [\n    {\n      \"amount\": 2,\n      \"id\": \"00bfa73302d12ffd\",\n      \"secret\": \"[\\\"P2PK\\\",{\\\"nonce\\\":\\\"c7f280eb55c1e8564e03db06973e94bc9b666d9e1ca42ad278408fe625950303\\\",\\\"data\\\":\\\"030d8acedfe072c9fa449a1efe0817157403fbec460d8e79f957966056e5dd76c1\\\",\\\"tags\\\":[[\\\"sigflag\\\",\\\"SIG_ALL\\\"]]}]\",\n      \"C\": \"02c97ee3d1db41cf0a3ddb601724be8711a032950811bf326f8219c50c4808d3cd\",\n      \"witness\": \"{\\\"signatures\\\":[\\\"ce017ca25b1b97df2f72e4b49f69ac26a240ce14b3690a8fe619d41ccc42d3c1282e073f85acd36dc50011638906f35b56615f24e4d03e8effe8257f6a808538\\\"]}\"\n    }\n  ],\n  \"outputs\": [\n    {\n      \"amount\": 2,\n      \"id\": \"00bfa73302d12ffd\",\n      \"B_\": \"038ec853d65ae1b79b5cdbc2774150b2cb288d6d26e12958a16fb33c32d9a86c39\"\n    }\n  ]\n}";
        var swapRequestParsed = JsonSerializer.Deserialize<PostSwapRequest>(swapRequest);
        var msgToSign = "[\"P2PK\",{\"nonce\":\"c7f280eb55c1e8564e03db06973e94bc9b666d9e1ca42ad278408fe625950303\",\"data\":\"030d8acedfe072c9fa449a1efe0817157403fbec460d8e79f957966056e5dd76c1\",\"tags\":[[\"sigflag\",\"SIG_ALL\"]]}]02c97ee3d1db41cf0a3ddb601724be8711a032950811bf326f8219c50c4808d3cd2038ec853d65ae1b79b5cdbc2774150b2cb288d6d26e12958a16fb33c32d9a86c39";
        Assert.Equal(msgToSign, SigAllHandler.GetMessageToSign(swapRequestParsed.Inputs, swapRequestParsed.Outputs));

        var signedSwapRequest =
            "{\n  \"inputs\": [\n    {\n      \"amount\": 2,\n      \"id\": \"00bfa73302d12ffd\",\n      \"secret\": \"[\\\"P2PK\\\",{\\\"nonce\\\":\\\"c7f280eb55c1e8564e03db06973e94bc9b666d9e1ca42ad278408fe625950303\\\",\\\"data\\\":\\\"030d8acedfe072c9fa449a1efe0817157403fbec460d8e79f957966056e5dd76c1\\\",\\\"tags\\\":[[\\\"sigflag\\\",\\\"SIG_ALL\\\"]]}]\",\n      \"C\": \"02c97ee3d1db41cf0a3ddb601724be8711a032950811bf326f8219c50c4808d3cd\",\n      \"witness\": \"{\\\"signatures\\\":[\\\"ce017ca25b1b97df2f72e4b49f69ac26a240ce14b3690a8fe619d41ccc42d3c1282e073f85acd36dc50011638906f35b56615f24e4d03e8effe8257f6a808538\\\"]}\"\n    }\n  ],\n  \"outputs\": [\n    {\n      \"amount\": 2,\n      \"id\": \"00bfa73302d12ffd\",\n      \"B_\": \"038ec853d65ae1b79b5cdbc2774150b2cb288d6d26e12958a16fb33c32d9a86c39\"\n    }\n  ]\n}";
        var signedSwapRequestParsed = JsonSerializer.Deserialize<PostSwapRequest>(signedSwapRequest);
        Assert.True(SigAllHandler.VerifySigAllWitness(signedSwapRequestParsed.Inputs, signedSwapRequestParsed.Outputs));
        var witness = JsonSerializer.Deserialize<P2PKWitness>(signedSwapRequestParsed.Inputs.First().Witness);
        Assert.True(SigAllHandler.VerifySigAllWitness(signedSwapRequestParsed.Inputs, signedSwapRequestParsed.Outputs, witness));

        var validSwapRequest =
            "{\n  \"inputs\": [\n    {\n      \"amount\": 2,\n      \"id\": \"00bfa73302d12ffd\",\n      \"secret\": \"[\\\"P2PK\\\",{\\\"nonce\\\":\\\"c7f280eb55c1e8564e03db06973e94bc9b666d9e1ca42ad278408fe625950303\\\",\\\"data\\\":\\\"030d8acedfe072c9fa449a1efe0817157403fbec460d8e79f957966056e5dd76c1\\\",\\\"tags\\\":[[\\\"sigflag\\\",\\\"SIG_ALL\\\"]]}]\",\n      \"C\": \"02c97ee3d1db41cf0a3ddb601724be8711a032950811bf326f8219c50c4808d3cd\",\n      \"witness\": \"{\\\"signatures\\\":[\\\"ce017ca25b1b97df2f72e4b49f69ac26a240ce14b3690a8fe619d41ccc42d3c1282e073f85acd36dc50011638906f35b56615f24e4d03e8effe8257f6a808538\\\"]}\"\n    }\n  ],\n  \"outputs\": [\n    {\n      \"amount\": 2,\n      \"id\": \"00bfa73302d12ffd\",\n      \"B_\": \"038ec853d65ae1b79b5cdbc2774150b2cb288d6d26e12958a16fb33c32d9a86c39\"\n    }\n  ]\n}";
        var validSwapRequestParsed = JsonSerializer.Deserialize<PostSwapRequest>(validSwapRequest);
        var witness1 = JsonSerializer.Deserialize<P2PKWitness>(validSwapRequestParsed?.Inputs[0].Witness);
        Assert.True(SigAllHandler.VerifySigAllWitness(validSwapRequestParsed.Inputs, validSwapRequestParsed.Outputs, witness1));
        Assert.True(SigAllHandler.VerifySigAllWitness(validSwapRequestParsed.Inputs, validSwapRequestParsed.Outputs));
        
        var invalidSwapRequest =
            "{\n  \"inputs\": [\n    {\n      \"amount\": 1,\n      \"id\": \"00bfa73302d12ffd\",\n      \"secret\": \"[\\\"P2PK\\\",{\\\"nonce\\\":\\\"fa6dd3fac9086c153878dec90b9e37163d38ff2ecf8b37db6470e9d185abbbae\\\",\\\"data\\\":\\\"033b42b04e659fed13b669f8b16cdaffc3ee5738608810cf97a7631d09bd01399d\\\",\\\"tags\\\":[[\\\"sigflag\\\",\\\"SIG_ALL\\\"]]}]\",\n      \"C\": \"024d232312bab25af2e73f41d56864d378edca9109ae8f76e1030e02e585847786\",\n      \"witness\": \"{\\\"signatures\\\":[\\\"27b4d260a1186e3b62a26c0d14ffeab3b9f7c3889e78707b8fd3836b473a00601afbd53a2288ad20a624a8bbe3344453215ea075fc0ce479dd8666fd3d9162cc\\\"]}\"\n    },\n    {\n      \"amount\": 2,\n      \"id\": \"00bfa73302d12ffd\",\n      \"secret\": \"[\\\"P2PK\\\",{\\\"nonce\\\":\\\"4007b21fc5f5b1d4920bc0a08b158d98fd0fb2b0b0262b57ff53c6c5d6c2ae8c\\\",\\\"data\\\":\\\"033b42b04e659fed13b669f8b16cdaffc3ee5738608810cf97a7631d09bd01399d\\\",\\\"tags\\\":[[\\\"locktime\\\",\\\"122222222222222\\\"],[\\\"sigflag\\\",\\\"SIG_ALL\\\"]]}]\",\n      \"C\": \"02417400f2af09772219c831501afcbab4efb3b2e75175635d5474069608deb641\"\n    }\n  ],\n  \"outputs\": [\n    {\n      \"amount\": 1,\n      \"id\": \"00bfa73302d12ffd\",\n      \"B_\": \"038ec853d65ae1b79b5cdbc2774150b2cb288d6d26e12958a16fb33c32d9a86c39\"\n    },\n    {\n      \"amount\": 1,\n      \"id\": \"00bfa73302d12ffd\",\n      \"B_\": \"03afe7c87e32d436f0957f1d70a2bca025822a84a8623e3a33aed0a167016e0ca5\"\n    },\n    {\n      \"amount\": 1,\n      \"id\": \"00bfa73302d12ffd\",\n      \"B_\": \"02c0d4fce02a7a0f09e3f1bca952db910b17e81a7ebcbce62cd8dcfb127d21e37b\"\n    }\n  ]\n}";
        var invalidSwapRequestParsed = JsonSerializer.Deserialize<PostSwapRequest>(invalidSwapRequest);
        Assert.False(SigAllHandler.VerifySigAllWitness(invalidSwapRequestParsed.Inputs, invalidSwapRequestParsed.Outputs));
        var witness2 = JsonSerializer.Deserialize<P2PKWitness>(invalidSwapRequestParsed?.Inputs[0].Witness);
        Assert.False(SigAllHandler.VerifySigAllWitness(invalidSwapRequestParsed.Inputs, invalidSwapRequestParsed.Outputs, witness2));
        
        var validSwapRequestMultisig = 
            "{\n  \"inputs\": [\n    {\n      \"amount\": 2,\n      \"id\": \"00bfa73302d12ffd\",\n      \"secret\": \"[\\\"P2PK\\\",{\\\"nonce\\\":\\\"04bfd885fc982d553711092d037fdceb7320fd8f96b0d4fd6d31a65b83b94272\\\",\\\"data\\\":\\\"0275e78025b558dbe6cb8fdd032a2e7613ca14fda5c1f4c4e3427f5077a7bd90e4\\\",\\\"tags\\\":[[\\\"pubkeys\\\",\\\"035163650bbd5ed4be7693f40f340346ba548b941074e9138b67ef6c42755f3449\\\",\\\"02817d22a8edc44c4141e192995a7976647c335092199f9e076a170c7336e2f5cc\\\"],[\\\"n_sigs\\\",\\\"2\\\"],[\\\"sigflag\\\",\\\"SIG_ALL\\\"]]}]\",\n      \"C\": \"03866a09946562482c576ca989d06371e412b221890804c7da8887d321380755be\",\n      \"witness\": \"{\\\"signatures\\\":[\\\"be1d72c5ca16a93c5a34f25ec63ce632ddc3176787dac363321af3fd0f55d1927e07451bc451ffe5c682d76688ea9925d7977dffbb15bd79763b527f474734b0\\\",\\\"669d6d10d7ed35395009f222f6c7bdc28a378a1ebb72ee43117be5754648501da3bedf2fd6ff0c7849ac92683538c60af0af504102e40f2d8daca8e08b1ca16b\\\"]}\"\n    }\n  ],\n  \"outputs\": [\n    {\n      \"amount\": 2,\n      \"id\": \"00bfa73302d12ffd\",\n      \"B_\": \"038ec853d65ae1b79b5cdbc2774150b2cb288d6d26e12958a16fb33c32d9a86c39\"\n    }\n  ]\n}";
        var validSwapRequestMultisigParsed = JsonSerializer.Deserialize<PostSwapRequest>(validSwapRequestMultisig);
        var witness3 = JsonSerializer.Deserialize<P2PKWitness>(validSwapRequestMultisigParsed.Inputs[0].Witness);
        Assert.True(SigAllHandler.VerifySigAllWitness(validSwapRequestMultisigParsed.Inputs, validSwapRequestMultisigParsed.Outputs, witness3));
        Assert.True(SigAllHandler.VerifySigAllWitness(validSwapRequestMultisigParsed.Inputs, validSwapRequestMultisigParsed.Outputs));
        
        var validSwapRequestMultisigRefundLocktime = 
            "{\n  \"inputs\": [\n    {\n      \"amount\": 2,\n      \"id\": \"00bfa73302d12ffd\",\n      \"secret\": \"[\\\"P2PK\\\",{\\\"nonce\\\":\\\"9ea35553beb18d553d0a53120d0175a0991ca6109370338406eed007b26eacd1\\\",\\\"data\\\":\\\"02af21e09300af92e7b48c48afdb12e22933738cfb9bba67b27c00c679aae3ec25\\\",\\\"tags\\\":[[\\\"locktime\\\",\\\"1\\\"],[\\\"refund\\\",\\\"02637c19143c58b2c58bd378400a7b82bdc91d6dedaeb803b28640ef7d28a887ac\\\",\\\"0345c7fdf7ec7c8e746cca264bf27509eb4edb9ac421f8fbfab1dec64945a4d797\\\"],[\\\"n_sigs_refund\\\",\\\"2\\\"],[\\\"sigflag\\\",\\\"SIG_ALL\\\"]]}]\",\n      \"C\": \"03dd83536fbbcbb74ccb3c87147df26753fd499cc2c095f74367fff0fb459c312e\",\n      \"witness\": \"{\\\"signatures\\\":[\\\"23b58ef28cd22f3dff421121240ddd621deee83a3bc229fd67019c2e338d91e2c61577e081e1375dbab369307bba265e887857110ca3b4bd949211a0a298805f\\\",\\\"7e75948ef1513564fdcecfcbd389deac67c730f7004f8631ba90c0844d3e8c0cf470b656306877df5141f65fd3b7e85445a8452c3323ab273e6d0d44843817ed\\\"]}\"\n    }\n  ],\n  \"outputs\": [\n    {\n      \"amount\": 2,\n      \"id\": \"00bfa73302d12ffd\",\n      \"B_\": \"038ec853d65ae1b79b5cdbc2774150b2cb288d6d26e12958a16fb33c32d9a86c39\"\n    }\n  ]\n}";
        var validSwapRequestMultisigRefundLocktimeParsed =
            JsonSerializer.Deserialize<PostSwapRequest>(validSwapRequestMultisigRefundLocktime);
        var witness5 = JsonSerializer.Deserialize<P2PKWitness>(validSwapRequestMultisigRefundLocktimeParsed.Inputs[0].Witness);
        Assert.True(SigAllHandler.VerifySigAllWitness(validSwapRequestMultisigRefundLocktimeParsed.Inputs, validSwapRequestMultisigRefundLocktimeParsed.Outputs, witness5));
        Assert.True(SigAllHandler.VerifySigAllWitness(validSwapRequestMultisigRefundLocktimeParsed.Inputs, validSwapRequestMultisigRefundLocktimeParsed.Outputs));
        
        var validSwapRequestHTLC = 
            "{\n  \"inputs\": [\n    {\n      \"amount\": 2,\n      \"id\": \"00bfa73302d12ffd\",\n      \"secret\": \"[\\\"HTLC\\\",{\\\"nonce\\\":\\\"d730dd70cd7ec6e687829857de8e70aab2b970712f4dbe288343eca20e63c28c\\\",\\\"data\\\":\\\"ec4916dd28fc4c10d78e287ca5d9cc51ee1ae73cbfde08c6b37324cbfaac8bc5\\\",\\\"tags\\\":[[\\\"pubkeys\\\",\\\"0350cda8a1d5257dbd6ba8401a9a27384b9ab699e636e986101172167799469b14\\\"],[\\\"sigflag\\\",\\\"SIG_ALL\\\"]]}]\",\n      \"C\": \"03ff6567e2e6c31db5cb7189dab2b5121930086791c93899e4eff3dda61cb57273\",\n      \"witness\": \"{\\\"preimage\\\":\\\"0000000000000000000000000000000000000000000000000000000000000001\\\",\\\"signatures\\\":[\\\"a4c00a9ad07f9936e404494fda99a9b935c82d7c053173b304b8663124c81d4b00f64a225f5acf41043ca52b06382722bd04ded0fbeb0fcc404eed3b24778b88\\\"]}\"\n    }\n  ],\n  \"outputs\": [\n    {\n      \"amount\": 2,\n      \"id\": \"00bfa73302d12ffd\",\n      \"B_\": \"038ec853d65ae1b79b5cdbc2774150b2cb288d6d26e12958a16fb33c32d9a86c39\"\n    }\n  ]\n}";
        var validSwapRequestHTLCParsed =
            JsonSerializer.Deserialize<PostSwapRequest>(validSwapRequestHTLC);
        var witness6 = JsonSerializer.Deserialize<HTLCWitness>(validSwapRequestHTLCParsed.Inputs[0].Witness);
        var b = SigAllHandler.VerifySigAllWitness(validSwapRequestHTLCParsed.Inputs, validSwapRequestHTLCParsed.Outputs,
            witness6);
        Assert.True(b);
        Assert.True(SigAllHandler.VerifySigAllWitness(validSwapRequestHTLCParsed.Inputs, validSwapRequestHTLCParsed.Outputs));

        var invalidSwapRequestHTLC = 
            "{\n  \"inputs\": [\n    {\n      \"amount\": 2,\n      \"id\": \"00bfa73302d12ffd\",\n      \"secret\": \"[\\\"HTLC\\\",{\\\"nonce\\\":\\\"512c4045f12fdfd6f55059669c189e040c37c1ce2f8be104ed6aec296acce4e9\\\",\\\"data\\\":\\\"ec4916dd28fc4c10d78e287ca5d9cc51ee1ae73cbfde08c6b37324cbfaac8bc5\\\",\\\"tags\\\":[[\\\"pubkeys\\\",\\\"03ba83defd31c63f8841d188f0d41b5bb3af1bb3c08d0ba46f8f1d26a4d45e8cad\\\"],[\\\"locktime\\\",\\\"4854185133\\\"],[\\\"refund\\\",\\\"032f1008a79c722e93a1b4b853f85f38283f9ef74ee4c5c91293eb1cc3c5e46e34\\\"],[\\\"sigflag\\\",\\\"SIG_ALL\\\"]]}]\",\n      \"C\": \"02207abeff828146f1fc3909c74613d5605bd057f16791994b3c91f045b39a6939\",\n      \"witness\": \"{\\\"preimage\\\":\\\"0000000000000000000000000000000000000000000000000000000000000001\\\",\\\"signatures\\\":[\\\"7816d57871bde5be2e4281065dbe5b15f641d8f1ed9437a3ae556464d6f9b8a0a2e6660337a915f2c26dce1453a416daf682b8fb593b67a0750fce071e0759b9\\\"]}\"\n    }\n  ],\n  \"outputs\": [\n    {\n      \"amount\": 1,\n      \"id\": \"00bfa73302d12ffd\",\n      \"B_\": \"038ec853d65ae1b79b5cdbc2774150b2cb288d6d26e12958a16fb33c32d9a86c39\"\n    },\n    {\n      \"amount\": 1,\n      \"id\": \"00bfa73302d12ffd\",\n      \"B_\": \"03afe7c87e32d436f0957f1d70a2bca025822a84a8623e3a33aed0a167016e0ca5\"\n    }\n  ]\n}";
        var invalidSwapRequestHTLCParsed = JsonSerializer.Deserialize<PostSwapRequest>(invalidSwapRequestHTLC);
        Assert.False(SigAllHandler.VerifySigAllWitness(invalidSwapRequestHTLCParsed.Inputs, invalidSwapRequestHTLCParsed.Outputs));
        var witness7 = JsonSerializer.Deserialize<HTLCWitness>(invalidSwapRequestHTLCParsed.Inputs[0].Witness);
        Assert.False(SigAllHandler.VerifySigAllWitness(invalidSwapRequestHTLCParsed.Inputs, invalidSwapRequestHTLCParsed.Outputs, witness7));

        var validSwapRequestHTLCMultisig =
            "{\n  \"inputs\": [\n    {\n      \"amount\": 2,\n      \"id\": \"00bfa73302d12ffd\",\n      \"secret\": \"[\\\"HTLC\\\",{\\\"nonce\\\":\\\"c9b0fabb8007c0db4bef64d5d128cdcf3c79e8bb780c3294adf4c88e96c32647\\\",\\\"data\\\":\\\"ec4916dd28fc4c10d78e287ca5d9cc51ee1ae73cbfde08c6b37324cbfaac8bc5\\\",\\\"tags\\\":[[\\\"pubkeys\\\",\\\"039e6ec7e922abb4162235b3a42965eb11510b07b7461f6b1a17478b1c9c64d100\\\"],[\\\"locktime\\\",\\\"1\\\"],[\\\"refund\\\",\\\"02ce1bbd2c9a4be8029c9a6435ad601c45677f5cde81f8a7f0ed535e0039d0eb6c\\\",\\\"03c43c00ff57f63cfa9e732f0520c342123e21331d0121139f1b636921eeec095f\\\"],[\\\"n_sigs_refund\\\",\\\"2\\\"],[\\\"sigflag\\\",\\\"SIG_ALL\\\"]]}]\",\n      \"C\": \"0344b6f1471cf18a8cbae0e624018c816be5e3a9b04dcb7689f64173c1ae90a3a5\",\n      \"witness\": \"{\\\"preimage\\\":\\\"0000000000000000000000000000000000000000000000000000000000000001\\\",\\\"signatures\\\":[\\\"98e21672d409cc782c720f203d8284f0af0c8713f18167499f9f101b7050c3e657fb0e57478ebd8bd561c31aa6c30f4cd20ec38c73f5755b7b4ddee693bca5a5\\\",\\\"693f40129dbf905ed9c8008081c694f72a36de354f9f4fa7a61b389cf781f62a0ae0586612fb2eb504faaf897fefb6742309186117f4743bcebcb8e350e975e2\\\"]}\"\n    }\n  ],\n  \"outputs\": [\n    {\n      \"amount\": 2,\n      \"id\": \"00bfa73302d12ffd\",\n      \"B_\": \"038ec853d65ae1b79b5cdbc2774150b2cb288d6d26e12958a16fb33c32d9a86c39\"\n    }\n  ]\n}";
        var validSwapRequestHTLCMultisigParsed = JsonSerializer.Deserialize<PostSwapRequest>(validSwapRequestHTLCMultisig);
        var witness8 = JsonSerializer.Deserialize<HTLCWitness>(validSwapRequestHTLCMultisigParsed.Inputs[0].Witness);
        Assert.True(SigAllHandler.VerifySigAllWitness(validSwapRequestHTLCMultisigParsed.Inputs, validSwapRequestHTLCMultisigParsed.Outputs, witness8));
        Assert.True(SigAllHandler.VerifySigAllWitness(validSwapRequestHTLCMultisigParsed.Inputs, validSwapRequestHTLCMultisigParsed.Outputs));

        var meltRequest = 
            "{\n  \"quote\": \"cF8911fzT88aEi1d-6boZZkq5lYxbUSVs-HbJxK0\",\n  \"inputs\": [\n    {\n      \"amount\": 2,\n      \"id\": \"00bfa73302d12ffd\",\n      \"secret\": \"[\\\"P2PK\\\",{\\\"nonce\\\":\\\"bbf9edf441d17097e39f5095a3313ba24d3055ab8a32f758ff41c10d45c4f3de\\\",\\\"data\\\":\\\"029116d32e7da635c8feeb9f1f4559eb3d9b42d400f9d22a64834d89cde0eb6835\\\",\\\"tags\\\":[[\\\"sigflag\\\",\\\"SIG_ALL\\\"]]}]\",\n      \"C\": \"02a9d461ff36448469dccf828fa143833ae71c689886ac51b62c8d61ddaa10028b\",\n      \"witness\": \"{\\\"signatures\\\":[\\\"478224fbe715e34f78cb33451db6fcf8ab948afb8bd04ff1a952c92e562ac0f7c1cb5e61809410635be0aa94d0448f7f7959bd5762cc3802b0a00ff58b2da747\\\"]}\"\n    }\n  ],\n  \"outputs\": [\n    {\n      \"amount\": 0,\n      \"id\": \"00bfa73302d12ffd\",\n      \"B_\": \"038ec853d65ae1b79b5cdbc2774150b2cb288d6d26e12958a16fb33c32d9a86c39\"\n    }\n  ]\n}";
        var meltRequestParsed = JsonSerializer.Deserialize<PostMeltRequest>(meltRequest);
        var msg2 =
            "[\"P2PK\",{\"nonce\":\"bbf9edf441d17097e39f5095a3313ba24d3055ab8a32f758ff41c10d45c4f3de\",\"data\":\"029116d32e7da635c8feeb9f1f4559eb3d9b42d400f9d22a64834d89cde0eb6835\",\"tags\":[[\"sigflag\",\"SIG_ALL\"]]}]02a9d461ff36448469dccf828fa143833ae71c689886ac51b62c8d61ddaa10028b0038ec853d65ae1b79b5cdbc2774150b2cb288d6d26e12958a16fb33c32d9a86c39cF8911fzT88aEi1d-6boZZkq5lYxbUSVs-HbJxK0";
        Assert.Equal(SigAllHandler.GetMessageToSign(meltRequestParsed.Inputs, meltRequestParsed.Outputs, meltRequestParsed.Quote), msg2);

        var meltRequestValid = 
            "{\n  \"quote\": \"cF8911fzT88aEi1d-6boZZkq5lYxbUSVs-HbJxK0\",\n  \"inputs\": [\n    {\n      \"amount\": 2,\n      \"id\": \"00bfa73302d12ffd\",\n      \"secret\": \"[\\\"P2PK\\\",{\\\"nonce\\\":\\\"bbf9edf441d17097e39f5095a3313ba24d3055ab8a32f758ff41c10d45c4f3de\\\",\\\"data\\\":\\\"029116d32e7da635c8feeb9f1f4559eb3d9b42d400f9d22a64834d89cde0eb6835\\\",\\\"tags\\\":[[\\\"sigflag\\\",\\\"SIG_ALL\\\"]]}]\",\n      \"C\": \"02a9d461ff36448469dccf828fa143833ae71c689886ac51b62c8d61ddaa10028b\",\n      \"witness\": \"{\\\"signatures\\\":[\\\"478224fbe715e34f78cb33451db6fcf8ab948afb8bd04ff1a952c92e562ac0f7c1cb5e61809410635be0aa94d0448f7f7959bd5762cc3802b0a00ff58b2da747\\\"]}\"\n    }\n  ],\n  \"outputs\": [\n    {\n      \"amount\": 0,\n      \"id\": \"00bfa73302d12ffd\",\n      \"B_\": \"038ec853d65ae1b79b5cdbc2774150b2cb288d6d26e12958a16fb33c32d9a86c39\"\n    }\n  ]\n}";
        var meltRequestValidParsed = JsonSerializer.Deserialize<PostMeltRequest>(meltRequestValid);
        Assert.True(SigAllHandler.VerifySigAllWitness(meltRequestValidParsed.Inputs, meltRequestValidParsed.Outputs, meltRequestValidParsed.Quote));
        var witness9 = JsonSerializer.Deserialize<P2PKWitness>(meltRequestValidParsed.Inputs[0].Witness);
        Assert.True(SigAllHandler.VerifySigAllWitness(meltRequestValidParsed.Inputs, meltRequestValidParsed.Outputs, witness9, meltRequestValidParsed.Quote));

        var meltRequestMultisig =
            "{\n  \"quote\": \"Db3qEMVwFN2tf_1JxbZp29aL5cVXpSMIwpYfyOVF\",\n  \"inputs\": [\n    {\n      \"amount\": 2,\n      \"id\": \"00bfa73302d12ffd\",\n      \"secret\": \"[\\\"P2PK\\\",{\\\"nonce\\\":\\\"68d7822538740e4f9c9ebf5183ef6c4501c7a9bca4e509ce2e41e1d62e7b8a99\\\",\\\"data\\\":\\\"0394e841bd59aeadce16380df6174cb29c9fea83b0b65b226575e6d73cc5a1bd59\\\",\\\"tags\\\":[[\\\"pubkeys\\\",\\\"033d892d7ad2a7d53708b7a5a2af101cbcef69522bd368eacf55fcb4f1b0494058\\\"],[\\\"n_sigs\\\",\\\"2\\\"],[\\\"sigflag\\\",\\\"SIG_ALL\\\"]]}]\",\n      \"C\": \"03a70c42ec9d7192422c7f7a3ad017deda309fb4a2453fcf9357795ea706cc87a9\",\n      \"witness\": \"{\\\"signatures\\\":[\\\"ed739970d003f703da2f101a51767b63858f4894468cc334be04aa3befab1617a81e3eef093441afb499974152d279e59d9582a31dc68adbc17ffc22a2516086\\\",\\\"f9efe1c70eb61e7ad8bd615c50ff850410a4135ea73ba5fd8e12a734743ad045e575e9e76ea5c52c8e7908d3ad5c0eaae93337e5c11109e52848dc328d6757a2\\\"]}\"\n    }\n  ],\n  \"outputs\": [\n    {\n      \"amount\": 0,\n      \"id\": \"00bfa73302d12ffd\",\n      \"B_\": \"038ec853d65ae1b79b5cdbc2774150b2cb288d6d26e12958a16fb33c32d9a86c39\"\n    }\n  ]\n}";
        var meltRequestMultisigParsed = JsonSerializer.Deserialize<PostMeltRequest>(meltRequestMultisig);
        Assert.True(SigAllHandler.VerifySigAllWitness(meltRequestMultisigParsed.Inputs, meltRequestMultisigParsed.Outputs, meltRequestMultisigParsed.Quote));
        var witness10 = JsonSerializer.Deserialize<P2PKWitness>(meltRequestMultisigParsed.Inputs[0].Witness);
        Assert.True(SigAllHandler.VerifySigAllWitness(meltRequestMultisigParsed.Inputs, meltRequestMultisigParsed.Outputs, witness10, meltRequestMultisigParsed.Quote));
    }
        
    [Fact]
    public void Nut12Tests_Hash_e()
    {
        var r1 = "020000000000000000000000000000000000000000000000000000000000000001".ToPubKey();
        var r2 = "020000000000000000000000000000000000000000000000000000000000000001".ToPubKey();
        var k = "020000000000000000000000000000000000000000000000000000000000000001".ToPubKey();
        var c = "02a9acc1e48c25eeeb9289b5031cc57da9fe72f3fe2861d264bdc074209b107ba2".ToPubKey();
        var e = Cashu.ComputeE(r1, r2, k, c).ToHex();
        Assert.Equal("a4dc034b74338c28c6bc3ea49731f2a24440fc7c4affc08b31a93fc9fbe6401e", e);
    }

    [Fact]
    public void Nut12Tests_BlindSignaturesDLEQ()
    {
        var A = "0279be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798".ToPubKey();
        var B_ = "02a9acc1e48c25eeeb9289b5031cc57da9fe72f3fe2861d264bdc074209b107ba2".ToPubKey();
        var blindSig = JsonSerializer.Deserialize<BlindSignature>(
            "{\n  \"amount\": 8,\n  \"id\": \"00882760bfa2eb41\",\n  \"C_\": \"02a9acc1e48c25eeeb9289b5031cc57da9fe72f3fe2861d264bdc074209b107ba2\",\n  \"dleq\": {\n    \"e\": \"9818e061ee51d5c8edc3342369a554998ff7b4381c8652d724cdf46429be73d9\",\n    \"s\": \"9818e061ee51d5c8edc3342369a554998ff7b4381c8652d724cdf46429be73da\"\n  }\n}"
        );

        Assert.NotNull(blindSig?.DLEQ);
        blindSig.Verify(A, B_);
    }

    [Fact]
    public void Nut12Tests_ProofDLEQ()
    {
        var A = "0279be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798".ToPubKey();
        var proof = JsonSerializer.Deserialize<Proof>(
            "{\"amount\": 1,\"id\": \"00882760bfa2eb41\",\"secret\": \"daf4dd00a2b68a0858a80450f52c8a7d2ccf87d375e43e216e0c571f089f63e9\",\"C\": \"024369d2d22a80ecf78f3937da9d5f30c1b9f74f0c32684d583cca0fa6a61cdcfc\",\"dleq\": {\"e\": \"b31e58ac6527f34975ffab13e70a48b6d2b0d35abc4b03f0151f09ee1a9763d4\",\"s\": \"8fbae004c59e754d71df67e392b6ae4e29293113ddc2ec86592a0431d16306d8\",\"r\": \"a6d13fcd7a18442e6076f5e1e7c887ad5de40a019824bdfa9fe740d302e8d861\"}}"
        );
        Assert.NotNull(proof?.DLEQ);
        Assert.Equal(
            "024369d2d22a80ecf78f3937da9d5f30c1b9f74f0c32684d583cca0fa6a61cdcfc".ToPubKey(),
            proof.Secret.ToCurve()
        );
        Assert.True(proof.Verify(A));
    }

    [Fact]
    public void Nut14Tests_HTLCSecret()
    {
        var htlcSecretStr =
            "[\n  \"HTLC\",\n  {\n    \"nonce\": \"da62796403af76c80cd6ce9153ed3746\",\n    \"data\": \"023192200a0cfd3867e48eb63b03ff599c7e46c8f4e41146b2d281173ca6c50c\",\n    \"tags\": [\n      [\n        \"pubkeys\",\n        \"02698c4e2b5f9534cd0687d87513c759790cf829aa5739184a3e3735471fbda904\"\n      ],\n      [\n        \"locktime\",\n        \"1689418329\"\n      ],                   \n      [\n        \"refund\",\n        \"033281c37677ea273eb7183b783067f5244933ef78d8c3f15b1a77cb246099c26e\"\n      ]\n    ]\n  }\n]";
        var secret = JsonSerializer.Deserialize<ISecret>(htlcSecretStr);
        var nut10Secret = Assert.IsType<Nut10Secret>(secret);
        Assert.Equal(HTLCProofSecret.Key, nut10Secret.Key);
        var htlcSecret = Assert.IsType<HTLCProofSecret>(nut10Secret.ProofSecret);
        Assert.Single(htlcSecret.GetAllowedPubkeys(out var requiredSignatures));
        Assert.Equal(1, requiredSignatures);
        var rebuiltHtlcSecret = htlcSecret.Builder.Build();
        var rebuiltNut10 = new Nut10Secret(HTLCProofSecret.Key, rebuiltHtlcSecret);
        Assert.Equal(JsonSerializer.Serialize(nut10Secret), JsonSerializer.Serialize(rebuiltNut10));
    }

    [Fact]
    public void Nut18Tests()
    {
        var creqA =
            "creqApWF0gaNhdGVub3N0cmFheKlucHJvZmlsZTFxeTI4d3VtbjhnaGo3dW45ZDNzaGp0bnl2OWtoMnVld2Q5aHN6OW1od2RlbjV0ZTB3ZmprY2N0ZTljdXJ4dmVuOWVlaHFjdHJ2NWhzenJ0aHdkZW41dGUwZGVoaHh0bnZkYWtxcWd5ZGFxeTdjdXJrNDM5eWtwdGt5c3Y3dWRoZGh1NjhzdWNtMjk1YWtxZWZkZWhrZjBkNDk1Y3d1bmw1YWeBgmFuYjE3YWloYjdhOTAxNzZhYQphdWNzYXRhbYF4Imh0dHBzOi8vbm9mZWVzLnRlc3RudXQuY2FzaHUuc3BhY2U=";
        var pr = PaymentRequest.Parse(creqA);
        Assert.Equal("https://nofees.testnut.cashu.space", Assert.Single(pr.Mints));
        Assert.Equal((ulong)10, pr.Amount);
        Assert.Equal("b7a90176", pr.PaymentId);
        Assert.Equal("sat", pr.Unit);
        var t = Assert.Single(pr.Transports);
        Assert.Equal("nostr", t.Type);
        Assert.Equal(
            "nprofile1qy28wumn8ghj7un9d3shjtnyv9kh2uewd9hsz9mhwden5te0wfjkccte9curxven9eehqctrv5hszrthwden5te0dehhxtnvdakqqgydaqy7curk439ykptkysv7udhdhu68sucm295akqefdehkf0d495cwunl5",
            t.Target
        );
        Assert.Equal("n", Assert.Single(t.Tags).Key);
        Assert.Equal("17", Assert.Single(t.Tags).Value);
        // Assert.Equal(creqA, pr.ToString());
    }

    internal readonly struct TestCase(in string path, in string keyHex, in string ccHex)
    {
        internal static readonly ReadOnlyMemory<byte> Seed = Convert.FromHexString(
            "e4a964f4973ce5750a6a5a5126e8258442c197b2e71b683ccba58688f21242eae1b0f12bee21d6e983d4a5c61f081bf3f0669546eb576dec1b22ec8d481b00fb"
        );

        internal readonly ReadOnlyMemory<byte> Key = Convert.FromHexString(keyHex);
        internal readonly ReadOnlyMemory<byte> ChainCode = Convert.FromHexString(ccHex);

        internal readonly KeyPath Path = path;
    }

    private static readonly TestCase Case1SecP256K1 = new(
        "m/0'/0/0",
        "6144c1daf8222d6dab77e7a20c2f338519b83bd1423602c56c7dfb5e9ea99c02",
        "55b36970e7ab8434f9b04f1c2e52da7422d2bce7e284ca353419dddfa2e34bdb"
    );

    [Fact]
    public void Bip32Test()
    {
        var masterKeyFromSeed = BIP32.Instance.GetMasterKeyFromSeed(TestCase.Seed.Span);

        Assert.Equal(
            "5A876CC4B4AB2F6717951AEE7F97AB69844DBFFFF7074E6E6F71D2BA04BD6EC9",
            Convert.ToHexString(masterKeyFromSeed.ChainCode)
        );
        Assert.Equal(
            "8D18D3F0CF9D74B53A935D97E8DE85955ED9F6EEFC6D6D45F0C169031A11B669",
            Convert.ToHexString(masterKeyFromSeed.PrivateKey)
        );

        Assert.Equal(
            "026cf0d14fcfa930347e7da26281319ac5959d02f1b6331812261efdb7e347788b",
            ECPrivKey.Create(masterKeyFromSeed.PrivateKey).CreatePubKey().ToHex()
        );

        var der1 = BIP32.Instance.DerivePath(Case1SecP256K1.Path, TestCase.Seed.Span);
        Assert.True(der1.PrivateKey.SequenceEqual(Case1SecP256K1.Key.Span));
        Assert.True(der1.ChainCode.SequenceEqual(Case1SecP256K1.ChainCode.Span));
    }

    [Fact]
    public void Nut13Tests()
    {
        var keysetId = new KeysetId("009a1f293253e41e");

        Assert.Equal(864559728, Nut13.GetKeysetIdInt(keysetId));
        var path = "m/129372'/0'/864559728'/{counter}'";
        var mnemonicPhrase =
            "half depart obvious quality work element tank gorilla view sugar picture humble";
        var mnemonic = new Mnemonic(mnemonicPhrase);
        Assert.Equal(
            "dd44ee516b0647e80b488e8dcc56d736a148f15276bef588b37057476d4b2b25780d3688a32b37353d6995997842c0fd8b412475c891c16310471fbc86dcbda8",
            Convert.ToHexString(mnemonic.DeriveSeed()).ToLowerInvariant()
        );

        Assert.Equal(
            "m/129372'/0'/864559728'/0'/0",
            Nut13.GetNut13DerivationPath(keysetId, 0, true)
        );
        Assert.Equal(
            "m/129372'/0'/864559728'/0'/1",
            Nut13.GetNut13DerivationPath(keysetId, 0, false)
        );

        Assert.Equal(
            "485875df74771877439ac06339e284c3acfcd9be7abf3bc20b516faeadfe77ae",
            mnemonic.DeriveSecret(keysetId, 0).Secret
        );
        Assert.Equal(
            "8f2b39e8e594a4056eb1e6dbb4b0c38ef13b1b2c751f64f810ec04ee35b77270",
            mnemonic.DeriveSecret(keysetId, 1).Secret
        );
        Assert.Equal(
            "bc628c79accd2364fd31511216a0fab62afd4a18ff77a20deded7b858c9860c8",
            mnemonic.DeriveSecret(keysetId, 2).Secret
        );
        Assert.Equal(
            "59284fd1650ea9fa17db2b3acf59ecd0f2d52ec3261dd4152785813ff27a33bf",
            mnemonic.DeriveSecret(keysetId, 3).Secret
        );
        Assert.Equal(
            "576c23393a8b31cc8da6688d9c9a96394ec74b40fdaf1f693a6bb84284334ea0",
            mnemonic.DeriveSecret(keysetId, 4).Secret
        );

        Assert.Equal(
            "ad00d431add9c673e843d4c2bf9a778a5f402b985b8da2d5550bf39cda41d679",
            Convert.ToHexString(mnemonic.DeriveBlindingFactor(keysetId, 0)).ToLowerInvariant()
        );
        Assert.Equal(
            "967d5232515e10b81ff226ecf5a9e2e2aff92d66ebc3edf0987eb56357fd6248",
            Convert.ToHexString(mnemonic.DeriveBlindingFactor(keysetId, 1)).ToLowerInvariant()
        );
        Assert.Equal(
            "b20f47bb6ae083659f3aa986bfa0435c55c6d93f687d51a01f26862d9b9a4899",
            Convert.ToHexString(mnemonic.DeriveBlindingFactor(keysetId, 2)).ToLowerInvariant()
        );
        Assert.Equal(
            "fb5fca398eb0b1deb955a2988b5ac77d32956155f1c002a373535211a2dfdc29",
            Convert.ToHexString(mnemonic.DeriveBlindingFactor(keysetId, 3)).ToLowerInvariant()
        );
        Assert.Equal(
            "5f09bfbfe27c439a597719321e061e2e40aad4a36768bb2bcc3de547c9644bf9",
            Convert.ToHexString(mnemonic.DeriveBlindingFactor(keysetId, 4)).ToLowerInvariant()
        );
    }

    [Fact]
    public void Nut13HMACTests()
    {
        KeysetId keysetId = new KeysetId(
            "015ba18a8adcd02e715a58358eb618da4a4b3791151a4bee5e968bb88406ccf76a"
        );
        Mnemonic mnemonic = new Mnemonic(
            "half depart obvious quality work element tank gorilla view sugar picture humble"
        );

        Assert.Equal(
            "db5561a07a6e6490f8dadeef5be4e92f7cebaecf2f245356b5b2a4ec40687298",
            mnemonic.DeriveSecret(keysetId, 0).Secret
        );
        Assert.Equal(
            "b70e7b10683da3bf1cdf0411206f8180c463faa16014663f39f2529b2fda922e",
            mnemonic.DeriveSecret(keysetId, 1).Secret
        );
        Assert.Equal(
            "78a7ac32ccecc6b83311c6081b89d84bb4128f5a0d0c5e1af081f301c7a513f5",
            mnemonic.DeriveSecret(keysetId, 2).Secret
        );
        Assert.Equal(
            "094a2b6c63bfa7970bc09cda0e1cfc9cd3d7c619b8e98fabcfc60aea9e4963e5",
            mnemonic.DeriveSecret(keysetId, 3).Secret
        );
        Assert.Equal(
            "5e89fc5d30d0bf307ddf0a3ac34aa7a8ee3702169dafa3d3fe1d0cae70ecd5ef",
            mnemonic.DeriveSecret(keysetId, 4).Secret
        );

        Assert.Equal(
            "6d26181a3695e32e9f88b80f039ba1ae2ab5a200ad4ce9dbc72c6d3769f2b035",
            Convert.ToHexString(mnemonic.DeriveBlindingFactor(keysetId, 0)).ToLowerInvariant()
        );
        Assert.Equal(
            "bde4354cee75545bea1a2eee035a34f2d524cee2bb01613823636e998386952e",
            Convert.ToHexString(mnemonic.DeriveBlindingFactor(keysetId, 1)).ToLowerInvariant()
        );
        Assert.Equal(
            "f40cc1218f085b395c8e1e5aaa25dccc851be3c6c7526a0f4e57108f12d6dac4",
            Convert.ToHexString(mnemonic.DeriveBlindingFactor(keysetId, 2)).ToLowerInvariant()
        );
        Assert.Equal(
            "099ed70fc2f7ac769bc20b2a75cb662e80779827b7cc358981318643030577d0",
            Convert.ToHexString(mnemonic.DeriveBlindingFactor(keysetId, 3)).ToLowerInvariant()
        );
        Assert.Equal(
            "5550337312d223ba62e3f75cfe2ab70477b046d98e3e71804eade3956c7b98cf",
            Convert.ToHexString(mnemonic.DeriveBlindingFactor(keysetId, 4)).ToLowerInvariant()
        );
    }

    [Fact]
    public void NullExpiryTests_PostMintQuoteBolt11Response()
    {
        // Test JSON with null expiry field
        var jsonWithNullExpiry = """
            {
                "quote": "test-quote-id",
                "request": "test-request",
                "state": "PAID",
                "expiry": null,
                "amount": 1000,
                "unit": "sat"
            }
            """;

        var response = JsonSerializer.Deserialize<PostMintQuoteBolt11Response>(jsonWithNullExpiry);

        Assert.NotNull(response);
        Assert.Equal("test-quote-id", response.Quote);
        Assert.Equal("test-request", response.Request);
        Assert.Equal("PAID", response.State);
        Assert.Null(response.Expiry);
        Assert.Equal((ulong)1000, response.Amount);
        Assert.Equal("sat", response.Unit);

        // Test JSON without expiry field (should also be null)
        var jsonWithoutExpiry = """
            {
                "quote": "test-quote-id-2",
                "request": "test-request-2",
                "state": "UNPAID",
                "amount": 500,
                "unit": "sat"
            }
            """;

        var response2 = JsonSerializer.Deserialize<PostMintQuoteBolt11Response>(jsonWithoutExpiry);

        Assert.NotNull(response2);
        Assert.Equal("test-quote-id-2", response2.Quote);
        Assert.Equal("test-request-2", response2.Request);
        Assert.Equal("UNPAID", response2.State);
        Assert.Null(response2.Expiry);
        Assert.Equal((ulong)500, response2.Amount);
        Assert.Equal("sat", response2.Unit);

        // Test JSON with valid expiry value (should still work)
        var jsonWithExpiry = """
            {
                "quote": "test-quote-id-3",
                "request": "test-request-3",
                "state": "ISSUED",
                "expiry": 1640995200,
                "amount": 2000,
                "unit": "sat"
            }
            """;

        var response3 = JsonSerializer.Deserialize<PostMintQuoteBolt11Response>(jsonWithExpiry);

        Assert.NotNull(response3);
        Assert.Equal("test-quote-id-3", response3.Quote);
        Assert.Equal("test-request-3", response3.Request);
        Assert.Equal("ISSUED", response3.State);
        Assert.Equal(1640995200, response3.Expiry);
        Assert.Equal((ulong)2000, response3.Amount);
        Assert.Equal("sat", response3.Unit);
    }

    [Fact]
    public void NullExpiryTests_PostMeltQuoteBolt11Response()
    {
        // Test JSON with null expiry field
        var jsonWithNullExpiry = """
            {
                "quote": "melt-quote-id",
                "amount": 1000,
                "fee_reserve": 50,
                "state": "PAID",
                "expiry": null,
                "payment_preimage": "test-preimage"
            }
            """;

        var response = JsonSerializer.Deserialize<PostMeltQuoteBolt11Response>(jsonWithNullExpiry);

        Assert.NotNull(response);
        Assert.Equal("melt-quote-id", response.Quote);
        Assert.Equal((ulong)1000, response.Amount);
        Assert.Equal(50, response.FeeReserve);
        Assert.Equal("PAID", response.State);
        Assert.Null(response.Expiry);
        Assert.Equal("test-preimage", response.PaymentPreimage);

        // Test JSON without expiry field (should also be null)
        var jsonWithoutExpiry = """
            {
                "quote": "melt-quote-id-2",
                "amount": 500,
                "fee_reserve": 25,
                "state": "UNPAID"
            }
            """;

        var response2 = JsonSerializer.Deserialize<PostMeltQuoteBolt11Response>(jsonWithoutExpiry);

        Assert.NotNull(response2);
        Assert.Equal("melt-quote-id-2", response2.Quote);
        Assert.Equal((ulong)500, response2.Amount);
        Assert.Equal(25, response2.FeeReserve);
        Assert.Equal("UNPAID", response2.State);
        Assert.Null(response2.Expiry);
        Assert.Null(response2.PaymentPreimage);

        // Test JSON with valid expiry value (should still work)
        var jsonWithExpiry = """
            {
                "quote": "melt-quote-id-3",
                "amount": 2000,
                "fee_reserve": 100,
                "state": "PENDING",
                "expiry": 1640995200
            }
            """;

        var response3 = JsonSerializer.Deserialize<PostMeltQuoteBolt11Response>(jsonWithExpiry);

        Assert.NotNull(response3);
        Assert.Equal("melt-quote-id-3", response3.Quote);
        Assert.Equal((ulong)2000, response3.Amount);
        Assert.Equal(100, response3.FeeReserve);
        Assert.Equal("PENDING", response3.State);
        Assert.Equal(1640995200, response3.Expiry);
        Assert.Null(response3.PaymentPreimage);
    }

    private static readonly byte[] P2BK_PREFIX = "Cashu_P2BK_v1"u8.ToArray();

    [Fact]
    public void Nut28_P2BK_Tests()
    {
        // sender ephemeral keypair
        var e = new PrivKey("1cedb9df0c6872188b560ace9e35fd55c2532d53e19ae65b46159073886482ca");
        var E = new PubKey("02a8cda4cf448bfce9a9e46e588c06ea1780fcb94e3bbdf3277f42995d403a8b0c");
        Assert.Equal(E.Key.ToString()?.ToLowerInvariant(), e.Key.CreatePubKey().ToString()?.ToLowerInvariant());

        // receiver keypair
        var p = new PrivKey("ad37e8abd800be3e8272b14045873f4353327eedeb702b72ddcc5c5adff5129c");
        var P = new PubKey("02771fed6cb88aaac38b8b32104a942bf4b8f4696bc361171b3c7d06fa2ebddf06");

        Assert.Equal(P.Key.ToString()?.ToLowerInvariant(), p.Key.CreatePubKey().ToString()?.ToLowerInvariant());
        
        var zx = "40d6ba4430a6dfa915bb441579b0f4dee032307434e9957a092bbca73151df8b";
        Assert.Equal(zx, Convert.ToHexString(Cashu.ComputeZx(e, P)).ToLowerInvariant());
        Assert.Equal(zx, Convert.ToHexString(Cashu.ComputeZx(p, E)).ToLowerInvariant());

        string[] rs =
        [
            "f43cfecf4d44e109872ed601156a01211c0d9eba0460d5be254a510782a2d4aa",
            "4a57e6acb9db19344af5632aa45000cd2c643550bc63c7d5732221171ab0f5b3",
            "d4a8b84b21f2b0ad31654e96eddbc32bfdedae2d05dc179bdd6cc20236b1104d",
            "ecebf43123d1da3de611a05f5020085d63ca20829242cdc07f7c780e19594798",
            "5f42d463ead44cbb20e51843d9eb3b8b0e0021566fd89852d23ae85f57d60858",
            "a8f1c9d336954997ad571e5a5b59fe340c80902b10b9099d44e17abb3070118c",
            "c39fa43b707215c163593fb8cadc0eddb4fe2f82c0c79c82a6fc2e3b6b051a7e",
            "b17d6a51396eb926f4a901e20ff760a852563f90fd4b85e193888f34fd2ee523",
            "4d4af85ea296457155b7ce328cf9accbe232e8ac23a1dfe901a36ab1b72ea04d",
            "ce311248ea9f42a73fc874b3ce351d55964652840d695382f0018b36bb089dd1",
            "9de35112d62e6343d02301d8f58fef87958e99bb68cfdfa855e04fe18b95b114",
        ];

        for (int i = 0; i <= 10; i++)
        {
            var ri = (PrivKey)Cashu.ComputeRi(Convert.FromHexString(zx),  i);
            Assert.Equal(rs[i], ri.ToString());
        }

        string[] blindedPublicKeys =
        [
            "03b7c03eb05a0a539cfc438e81bcf38b65b7bb8685e8790f9b853bfe3d77ad5315",
            "0352fb6d93360b7c2538eedf3c861f32ea5883fceec9f3e573d9d84377420da838",
            "03667361ca925065dcafea0a705ba49e75bdd7975751fcc933e05953463c79fff1",
            "02aca3ed09382151250b38c85087ae0a1436a057b40f824a5569ba353d40347d08",
            "02cd397bd6e326677128f1b0e5f1d745ad89b933b1b8671e947592778c9fc2301d",
            "0394140369aae01dbaf74977ccbb09b3a9cf2252c274c791ac734a331716f1f7d4",
            "03480f28e8f8775d56a4254c7e0dfdd5a6ecd6318c757fcec9e84c1b48ada0666d",
            "02f8a7be813f7ba2253d09705cc68c703a9fd785a055bf8766057fc6695ec80efc",
            "03aa5446aaf07ca9730b233f5c404fd024ef92e3787cd1c34c81c0778fe23c59e9",
            "037f82d4e0a79b0624a58ef7181344b95afad8acf4275dad49bcd39c189b73ece2",
            "032371fc0eef6885062581a3852494e2eab8f384b7dd196281b85b77f94770fac5",
        ];
        //it's the same blinding as with computeB_
        for (int i = 0; i <= 10; i++)
        {
            Assert.Equal(
                blindedPublicKeys[i],
                ((PubKey)Cashu.ComputeB_(P, new PrivKey(rs[i]))).ToString()
            );
        }

        string[] skStd =
        [
            "a174e77b25459f4809a187415af14065b49140c1408860f543444ed59261a605",
            "f78fcf5891dbd772cd68146ae9d740107f96b43ea7d3f34850ee7d71faa6084f",
            "81e0a0f6f9f36eebb3d7ffd733630270967150344203a2d2fb66bfd0466fe1a8",
            "9a23dcdcfbd2987c6884519f95a747a1fc4dc289ce6a58f79d7675dc291818f3",
            "0c7abd0fc2d50af9a357c9841f727acfa683c35dac002389f034e62d6794d9b3",
            "5629b27f0e9607d62fc9cf9aa0e13d78a50432324ce094d462db7889402ee2e7",
            "70d78ce74872d3ffe5cbf0f910634e224d81d189fcef27b9c4f62c097ac3ebd9",
            "5eb552fd116f7765771bb322557e9fecead9e19839731118b1828d030cedb67e",
            "fa82e10a7a9703afd82a7f72d280ec0f3565679a0f120b5bdf6fc70c9723b2e9",
            "7b68faf4c2a000e5c23b25f413bc5c9a2ec9f48b4990deba0dfb8904cac76f2c",
            "4b1b39beae2f21825295b3193b172ecc2e123bc2a4f76adf73da4daf9b54826f",
        ];

        for (int i = 0; i <= 10; i++)
        {
            var ri = Cashu.ComputeRi(Convert.FromHexString(zx), i);
            var derivedKey = p.Key.TweakAdd(ri.ToBytes());

            Assert.Equal(skStd[i], Convert.ToHexString(derivedKey.ToBytes()).ToLowerInvariant());
        }

        string[] skNeg =
        [
            "47051623754422cb04bc24c0cfe2c1ddc8db1fcc18f0aa4b477df4aca2adc20e",
            "9d1ffe00e1da5af5c882b1ea5ec8c18893e09349803c3c9e552823490af22458",
            "2770cf9f49f1f26eaef29d56a85483e8aabb2f3f1a6bec28ffa065a756bbfdb1",
            "3fb40b854bd11bff639eef1f0a98c91a1097a194a6d2a24da1b01bb3396434fc",
            "b20aebb812d38e7c9e7267039463fc46757c7f4f33b10d1bb440ea91481736fd",
            "fbb9e1275e948b592ae46d1a15d2beef73fcee23d4917e6626e77ced20b14031",
            "1667bb8f98715782e0e68e788554cf9a61cbb094d557710fc92fd1e08b1007e2",
            "044581a5616dfae8723650a1ca702164ff23c0a311db5a6eb5bc32da1d39d287",
            "a0130fb2ca958732d3451cf247726d8749af46a4e77a54b1e3a96ce3a76fcef2",
            "20f9299d129e8468bd55c37388adde124313d39621f9281012352edbdb138b35",
            "f0ab6866fe2da5054db05098b008b042fd0af7b42ca8547137e652137bd6dfb9"
        ];

        for (int i = 0; i <= 10; i++)
        {
            var ri = Cashu.ComputeRi(Convert.FromHexString(zx), i);
            var derivedKeyNeg = p.Key.sec.Negate().Add(ri.sec).ToPrivateKey();

            Assert.Equal(skNeg[i], Convert.ToHexString(derivedKeyNeg.ToBytes()).ToLowerInvariant());
        }
    }

    [Fact]
    public void Nut28_P2BK_Flow()
    {
        // sender generates ephermal keypair
        var e = new PrivKey("1cedb9df0c6872188b560ace9e35fd55c2532d53e19ae65b46159073886482ca");
        var E = new PubKey("02a8cda4cf448bfce9a9e46e588c06ea1780fcb94e3bbdf3277f42995d403a8b0c");

        // receiver privkeys, with corresponding pubkeys that will get blinded
        var signing_key = ECPrivKey.Create(
            Convert.FromHexString(
                "0000000000000000000000000000000000000000000000000000000000000001"
            )
        );
        var signing_key_two = ECPrivKey.Create(
            Convert.FromHexString(
                "7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f"
            )
        );

        var refundPubkey = ECPrivKey
            .Create(
                Convert.FromHexString(
                    "99590802251e78ee1051648439eedb003dc539093a48a44e7b8f2642c909ea37"
                )
            )
            .CreatePubKey();

        var keysetId = new KeysetId("009a1f293253e41e");
        
        var conditions = new P2PKBuilder()
        {
            Lock = DateTimeOffset.FromUnixTimeSeconds(21000000000),
            Pubkeys = new[] { signing_key.CreatePubKey(), signing_key_two.CreatePubKey() },
            RefundPubkeys = new[] { refundPubkey },
            SignatureThreshold = 2,
            SigFlag = "SIG_INPUTS",
        };
        var p2pkProofSecret = conditions.BuildBlinded(e);

        var secret = new Nut10Secret(P2PKProofSecret.Key, p2pkProofSecret);

        var proof = new Proof()
        {
            Id = keysetId,
            Amount = 0,
            Secret = secret,
            C = "02698c4e2b5f9534cd0687d87513c759790cf829aa5739184a3e3735471fbda904".ToPubKey(),
            P2PkE = E,
        };
        var witness = p2pkProofSecret.GenerateBlindWitness(
            proof,
            new[] { signing_key, signing_key_two },
            E
        );

        Assert.True(p2pkProofSecret.VerifyWitness(secret, witness));
    }

    [Fact]
    public void Nut26_Flow_WithRandomE()
    {
        var signing_key = ECPrivKey.Create(
            Convert.FromHexString(
                "0000000000000000000000000000000000000000000000000000000000000001"
            )
        );
        var signing_key_two = ECPrivKey.Create(
            Convert.FromHexString(
                "7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f"
            )
        );

        var refundPubkey = ECPrivKey
            .Create(
                Convert.FromHexString(
                    "99590802251e78ee1051648439eedb003dc539093a48a44e7b8f2642c909ea37"
                )
            )
            .CreatePubKey();

        var keysetId = new KeysetId("009a1f293253e41e");

        var conditions = new P2PKBuilder()
        {
            Lock = DateTimeOffset.FromUnixTimeSeconds(21000000000),
            Pubkeys = new[] { signing_key.CreatePubKey(), signing_key_two.CreatePubKey() },
            RefundPubkeys = new[] { refundPubkey },
            SignatureThreshold = 2,
            SigFlag = "SIG_INPUTS",
        };
        var p2pkProofSecret = conditions.BuildBlinded(out var E);

        var secret = new Nut10Secret(P2PKProofSecret.Key, p2pkProofSecret);

        var proof = new Proof()
        {
            Id = keysetId,
            Amount = 0,
            Secret = secret,
            C = "02698c4e2b5f9534cd0687d87513c759790cf829aa5739184a3e3735471fbda904".ToPubKey(),
            P2PkE = E,
        };
        var witness = p2pkProofSecret.GenerateBlindWitness(
            proof,
            new[] { signing_key, signing_key_two },
            E
        );

        Assert.True(p2pkProofSecret.VerifyWitness(secret, witness));
    }
}
