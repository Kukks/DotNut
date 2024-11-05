using System.Text.Json;
using DotNut.NUT13;
using NBip32Fast;
using NBitcoin;
using NBitcoin.Secp256k1;

namespace DotNut.Tests;

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
        var result = Cashu.HexToCurve(message);
        Assert.Equal(point, result.ToHex());
    }


    [InlineData("d341ee4871f1f889041e63cf0d3823c713eea6aff01e80f1719f08f9e5be98f6",
        "99fce58439fc37412ab3468b73db0569322588f62fb3a49182d67e23d877824a",
        "033b1a9737a40cc3fd9b6af4b723632b76a67a36782596304612a6c2bfb5197e6d")]
    [InlineData("f1aaf16c2239746f369572c0784d9dd3d032d952c2d992175873fb58fae31a60",
        "f78476ea7cc9ade20f9e05e58a804cf19533f03ea805ece5fee88c8e2874ba50",
        "029bdf2d716ee366eddf599ba252786c1033f47e230248a4612a5670ab931f1763")]
    [Theory]
    public void Nut00Tests_BlindedMessages(string x, string r, string b)
    {
        var y = Cashu.HexToCurve(x);
        var blindingFactor = ECPrivKey.Create(Convert.FromHexString(r));

        var computedB = Cashu.ComputeB_(y, blindingFactor);
        Assert.Equal(b, computedB.ToHex());
    }

    [InlineData("0000000000000000000000000000000000000000000000000000000000000001",
        "02a9acc1e48c25eeeb9289b5031cc57da9fe72f3fe2861d264bdc074209b107ba2",
        "02a9acc1e48c25eeeb9289b5031cc57da9fe72f3fe2861d264bdc074209b107ba2")]
    [InlineData("7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f",
        "02a9acc1e48c25eeeb9289b5031cc57da9fe72f3fe2861d264bdc074209b107ba2",
        "0398bc70ce8184d27ba89834d19f5199c84443c31131e48d3c1214db24247d005d")]
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
                Assert.Equal(new KeysetId("009a1f293253e41e"), proof.Id);

                Assert.Equal("407915bc212be61a77e3e6d2aeb4c727980bda51cd06a6afc29e2861768a7837",
                    Assert.IsType<StringSecret>(proof.Secret).Secret);
                Assert.Equal("02bc9097997d81afb2cc7346b5e4345a9346bd2a506eb7958598a72f0cf85163ea".ToPubKey(),
                    (ECPubKey) proof.C);
            }, proof =>
            {
                Assert.Equal(8, proof.Amount);
                Assert.Equal(new KeysetId("009a1f293253e41e"), proof.Id);
                Assert.Equal("fe15109314e61d7756b0f8ee0f23a624acaa3f4e042f61433c728c7057b931be",
                    Assert.IsType<StringSecret>(proof.Secret).Secret);
                Assert.Equal("029e8e5050b890a7d6c0968db16bc1d5d5fa040ea1de284f6ec69d61299f671059".ToPubKey(),
                    (ECPubKey) proof.C);
            }
        );

        Assert.Equal(originalToken, result.Encode("A", false));

        Assert.Throws<FormatException>(() => CashuTokenHelper.Decode(
            "casshuAeyJ0b2tlbiI6W3sibWludCI6Imh0dHBzOi8vODMzMy5zcGFjZTozMzM4IiwicHJvb2ZzIjpbeyJhbW91bnQiOjIsImlkIjoiMDA5YTFmMjkzMjUzZTQxZSIsInNlY3JldCI6IjQwNzkxNWJjMjEyYmU2MWE3N2UzZTZkMmFlYjRjNzI3OTgwYmRhNTFjZDA2YTZhZmMyOWUyODYxNzY4YTc4MzciLCJDIjoiMDJiYzkwOTc5OTdkODFhZmIyY2M3MzQ2YjVlNDM0NWE5MzQ2YmQyYTUwNmViNzk1ODU5OGE3MmYwY2Y4NTE2M2VhIn0seyJhbW91bnQiOjgsImlkIjoiMDA5YTFmMjkzMjUzZTQxZSIsInNlY3JldCI6ImZlMTUxMDkzMTRlNjFkNzc1NmIwZjhlZTBmMjNhNjI0YWNhYTNmNGUwNDJmNjE0MzNjNzI4YzcwNTdiOTMxYmUiLCJDIjoiMDI5ZThlNTA1MGI4OTBhN2Q2YzA5NjhkYjE2YmMxZDVkNWZhMDQwZWExZGUyODRmNmVjNjlkNjEyOTlmNjcxMDU5In1dfV0sInVuaXQiOiJzYXQiLCJtZW1vIjoiVGhhbmsgeW91LiJ9",
            out _));
        Assert.Throws<FormatException>(() => CashuTokenHelper.Decode(
            "eyJ0b2tlbiI6W3sibWludCI6Imh0dHBzOi8vODMzMy5zcGFjZTozMzM4IiwicHJvb2ZzIjpbeyJhbW91bnQiOjIsImlkIjoiMDA5YTFmMjkzMjUzZTQxZSIsInNlY3JldCI6IjQwNzkxNWJjMjEyYmU2MWE3N2UzZTZkMmFlYjRjNzI3OTgwYmRhNTFjZDA2YTZhZmMyOWUyODYxNzY4YTc4MzciLCJDIjoiMDJiYzkwOTc5OTdkODFhZmIyY2M3MzQ2YjVlNDM0NWE5MzQ2YmQyYTUwNmViNzk1ODU5OGE3MmYwY2Y4NTE2M2VhIn0seyJhbW91bnQiOjgsImlkIjoiMDA5YTFmMjkzMjUzZTQxZSIsInNlY3JldCI6ImZlMTUxMDkzMTRlNjFkNzc1NmIwZjhlZTBmMjNhNjI0YWNhYTNmNGUwNDJmNjE0MzNjNzI4YzcwNTdiOTMxYmUiLCJDIjoiMDI5ZThlNTA1MGI4OTBhN2Q2YzA5NjhkYjE2YmMxZDVkNWZhMDQwZWExZGUyODRmNmVjNjlkNjEyOTlmNjcxMDU5In1dfV0sInVuaXQiOiJzYXQiLCJtZW1vIjoiVGhhbmsgeW91LiJ9",
            out _));



        
        var v4Token =
            "cashuBo2F0gqJhaUgA_9SLj17PgGFwgaNhYQFhc3hAYWNjMTI0MzVlN2I4NDg0YzNjZjE4NTAxNDkyMThhZjkwZjcxNmE1MmJmNGE1ZWQzNDdlNDhlY2MxM2Y3NzM4OGFjWCECRFODGd5IXVW-07KaZCvuWHk3WrnnpiDhHki6SCQh88-iYWlIAK0mjE0fWCZhcIKjYWECYXN4QDEzMjNkM2Q0NzA3YTU4YWQyZTIzYWRhNGU5ZjFmNDlmNWE1YjRhYzdiNzA4ZWIwZDYxZjczOGY0ODMwN2U4ZWVhY1ghAjRWqhENhLSsdHrr2Cw7AFrKUL9Ffr1XN6RBT6w659lNo2FhAWFzeEA1NmJjYmNiYjdjYzY0MDZiM2ZhNWQ1N2QyMTc0ZjRlZmY4YjQ0MDJiMTc2OTI2ZDNhNTdkM2MzZGNiYjU5ZDU3YWNYIQJzEpxXGeWZN5qXSmJjY8MzxWyvwObQGr5G1YCCgHicY2FtdWh0dHA6Ly9sb2NhbGhvc3Q6MzMzOGF1Y3NhdA";
        result = CashuTokenHelper.Decode(v4Token, out v);
        
        Assert.Equal("B", v);
        Assert.Null(result.Memo);
        Assert.Equal("sat", result.Unit);
        token = Assert.Single(result.Tokens);
        Assert.Equal("http://localhost:3338", token.Mint);
        Assert.Equal(3, token.Proofs.Count);
        Assert.Collection(token.Proofs, proof =>
            {
                Assert.Equal(1, proof.Amount);
                Assert.Equal(new KeysetId("00ffd48b8f5ecf80"), proof.Id);

                Assert.Equal("acc12435e7b8484c3cf1850149218af90f716a52bf4a5ed347e48ecc13f77388",
                    Assert.IsType<StringSecret>(proof.Secret).Secret);
                Assert.Equal("0244538319de485d55bed3b29a642bee5879375ab9e7a620e11e48ba482421f3cf".ToPubKey(),
                    (ECPubKey) proof.C);
            }, proof =>
            {
                Assert.Equal(2, proof.Amount);
                Assert.Equal(new KeysetId("00ad268c4d1f5826"), proof.Id);
                Assert.Equal("1323d3d4707a58ad2e23ada4e9f1f49f5a5b4ac7b708eb0d61f738f48307e8ee",
                    Assert.IsType<StringSecret>(proof.Secret).Secret);
                Assert.Equal("023456aa110d84b4ac747aebd82c3b005aca50bf457ebd5737a4414fac3ae7d94d".ToPubKey(),
                    (ECPubKey) proof.C);
            }, proof =>
            {
                Assert.Equal(1, proof.Amount);
                Assert.Equal(new KeysetId("00ad268c4d1f5826"), proof.Id);
                Assert.Equal("56bcbcbb7cc6406b3fa5d57d2174f4eff8b4402b176926d3a57d3c3dcbb59d57",
                    Assert.IsType<StringSecret>(proof.Secret).Secret);
                Assert.Equal("0273129c5719e599379a974a626363c333c56cafc0e6d01abe46d5808280789c63".ToPubKey(),
                    (ECPubKey) proof.C);
            }
        );
        Assert.Equal(v4Token, result.Encode("B", false));


    }

    [Theory]
    [InlineData(
        "{\n  \"1\":\"03a40f20667ed53513075dc51e715ff2046cad64eb68960632269ba7f0210e38\",\"2\":\"03fd4ce5a16b65576145949e6f99f445f8249fee17c606b688b504a849cdc452de\",\"4\":\"02648eccfa4c026960966276fa5a4cae46ce0fd432211a4f449bf84f13aa5f8303\",\"8\":\"02fdfd6796bfeac490cbee12f778f867f0a2c68f6508d17c649759ea0dc3547528\"\n}")]
    [InlineData(
        "{\n  \"1\":\"03a40f20667ed53513075dc51e715ff2046cad64eb68960632269ba7f0210e38bc\",\"2\":\"04fd4ce5a16b65576145949e6f99f445f8249fee17c606b688b504a849cdc452de3625246cb2c27dac965cb7200a5986467eee92eb7d496bbf1453b074e223e481\",\"4\":\"02648eccfa4c026960966276fa5a4cae46ce0fd432211a4f449bf84f13aa5f8303\",\"8\":\"02fdfd6796bfeac490cbee12f778f867f0a2c68f6508d17c649759ea0dc3547528\"\n}")]
    public void Nut01Tests_Keysets_Invalid(string keyset)
    {
        Assert.ThrowsAny<Exception>(() => JsonSerializer.Deserialize<Keyset>(keyset));
    }

    [Theory]
    [InlineData(
        "{\n  \"1\":\"03a40f20667ed53513075dc51e715ff2046cad64eb68960632269ba7f0210e38bc\",\"2\":\"03fd4ce5a16b65576145949e6f99f445f8249fee17c606b688b504a849cdc452de\",\"4\":\"02648eccfa4c026960966276fa5a4cae46ce0fd432211a4f449bf84f13aa5f8303\",\"8\":\"02fdfd6796bfeac490cbee12f778f867f0a2c68f6508d17c649759ea0dc3547528\"\n}")]
    [InlineData(
        "{\n  \"1\":\"03ba786a2c0745f8c30e490288acd7a72dd53d65afd292ddefa326a4a3fa14c566\",\"2\":\"03361cd8bd1329fea797a6add1cf1990ffcf2270ceb9fc81eeee0e8e9c1bd0cdf5\",\"4\":\"036e378bcf78738ddf68859293c69778035740e41138ab183c94f8fee7572214c7\",\"8\":\"03909d73beaf28edfb283dbeb8da321afd40651e8902fcf5454ecc7d69788626c0\",\"16\":\"028a36f0e6638ea7466665fe174d958212723019ec08f9ce6898d897f88e68aa5d\",\"32\":\"03a97a40e146adee2687ac60c2ba2586a90f970de92a9d0e6cae5a4b9965f54612\",\"64\":\"03ce86f0c197aab181ddba0cfc5c5576e11dfd5164d9f3d4a3fc3ffbbf2e069664\",\"128\":\"0284f2c06d938a6f78794814c687560a0aabab19fe5e6f30ede38e113b132a3cb9\",\"256\":\"03b99f475b68e5b4c0ba809cdecaae64eade2d9787aa123206f91cd61f76c01459\",\"512\":\"03d4db82ea19a44d35274de51f78af0a710925fe7d9e03620b84e3e9976e3ac2eb\",\"1024\":\"031fbd4ba801870871d46cf62228a1b748905ebc07d3b210daf48de229e683f2dc\",\"2048\":\"0276cedb9a3b160db6a158ad4e468d2437f021293204b3cd4bf6247970d8aff54b\",\"4096\":\"02fc6b89b403ee9eb8a7ed457cd3973638080d6e04ca8af7307c965c166b555ea2\",\"8192\":\"0320265583e916d3a305f0d2687fcf2cd4e3cd03a16ea8261fda309c3ec5721e21\",\"16384\":\"036e41de58fdff3cb1d8d713f48c63bc61fa3b3e1631495a444d178363c0d2ed50\",\"32768\":\"0365438f613f19696264300b069d1dad93f0c60a37536b72a8ab7c7366a5ee6c04\",\"65536\":\"02408426cfb6fc86341bac79624ba8708a4376b2d92debdf4134813f866eb57a8d\",\"131072\":\"031063e9f11c94dc778c473e968966eac0e70b7145213fbaff5f7a007e71c65f41\",\"262144\":\"02f2a3e808f9cd168ec71b7f328258d0c1dda250659c1aced14c7f5cf05aab4328\",\"524288\":\"038ac10de9f1ff9395903bb73077e94dbf91e9ef98fd77d9a2debc5f74c575bc86\",\"1048576\":\"0203eaee4db749b0fc7c49870d082024b2c31d889f9bc3b32473d4f1dfa3625788\",\"2097152\":\"033cdb9d36e1e82ae652b7b6a08e0204569ec7ff9ebf85d80a02786dc7fe00b04c\",\"4194304\":\"02c8b73f4e3a470ae05e5f2fe39984d41e9f6ae7be9f3b09c9ac31292e403ac512\",\"8388608\":\"025bbe0cfce8a1f4fbd7f3a0d4a09cb6badd73ef61829dc827aa8a98c270bc25b0\",\"16777216\":\"037eec3d1651a30a90182d9287a5c51386fe35d4a96839cf7969c6e2a03db1fc21\",\"33554432\":\"03280576b81a04e6abd7197f305506476f5751356b7643988495ca5c3e14e5c262\",\"67108864\":\"03268bfb05be1dbb33ab6e7e00e438373ca2c9b9abc018fdb452d0e1a0935e10d3\",\"134217728\":\"02573b68784ceba9617bbcc7c9487836d296aa7c628c3199173a841e7a19798020\",\"268435456\":\"0234076b6e70f7fbf755d2227ecc8d8169d662518ee3a1401f729e2a12ccb2b276\",\"536870912\":\"03015bd88961e2a466a2163bd4248d1d2b42c7c58a157e594785e7eb34d880efc9\",\"1073741824\":\"02c9b076d08f9020ebee49ac8ba2610b404d4e553a4f800150ceb539e9421aaeee\",\"2147483648\":\"034d592f4c366afddc919a509600af81b489a03caf4f7517c2b3f4f2b558f9a41a\",\"4294967296\":\"037c09ecb66da082981e4cbdb1ac65c0eb631fc75d85bed13efb2c6364148879b5\",\"8589934592\":\"02b4ebb0dda3b9ad83b39e2e31024b777cc0ac205a96b9a6cfab3edea2912ed1b3\",\"17179869184\":\"026cc4dacdced45e63f6e4f62edbc5779ccd802e7fabb82d5123db879b636176e9\",\"34359738368\":\"02b2cee01b7d8e90180254459b8f09bbea9aad34c3a2fd98c85517ecfc9805af75\",\"68719476736\":\"037a0c0d564540fc574b8bfa0253cca987b75466e44b295ed59f6f8bd41aace754\",\"137438953472\":\"021df6585cae9b9ca431318a713fd73dbb76b3ef5667957e8633bca8aaa7214fb6\",\"274877906944\":\"02b8f53dde126f8c85fa5bb6061c0be5aca90984ce9b902966941caf963648d53a\",\"549755813888\":\"029cc8af2840d59f1d8761779b2496623c82c64be8e15f9ab577c657c6dd453785\",\"1099511627776\":\"03e446fdb84fad492ff3a25fc1046fb9a93a5b262ebcd0151caa442ea28959a38a\",\"2199023255552\":\"02d6b25bd4ab599dd0818c55f75702fde603c93f259222001246569018842d3258\",\"4398046511104\":\"03397b522bb4e156ec3952d3f048e5a986c20a00718e5e52cd5718466bf494156a\",\"8796093022208\":\"02d1fb9e78262b5d7d74028073075b80bb5ab281edcfc3191061962c1346340f1e\",\"17592186044416\":\"030d3f2ad7a4ca115712ff7f140434f802b19a4c9b2dd1c76f3e8e80c05c6a9310\",\"35184372088832\":\"03e325b691f292e1dfb151c3fb7cad440b225795583c32e24e10635a80e4221c06\",\"70368744177664\":\"03bee8f64d88de3dee21d61f89efa32933da51152ddbd67466bef815e9f93f8fd1\",\"140737488355328\":\"0327244c9019a4892e1f04ba3bf95fe43b327479e2d57c25979446cc508cd379ed\",\"281474976710656\":\"02fb58522cd662f2f8b042f8161caae6e45de98283f74d4e99f19b0ea85e08a56d\",\"562949953421312\":\"02adde4b466a9d7e59386b6a701a39717c53f30c4810613c1b55e6b6da43b7bc9a\",\"1125899906842624\":\"038eeda11f78ce05c774f30e393cda075192b890d68590813ff46362548528dca9\",\"2251799813685248\":\"02ec13e0058b196db80f7079d329333b330dc30c000dbdd7397cbbc5a37a664c4f\",\"4503599627370496\":\"02d2d162db63675bd04f7d56df04508840f41e2ad87312a3c93041b494efe80a73\",\"9007199254740992\":\"0356969d6aef2bb40121dbd07c68b6102339f4ea8e674a9008bb69506795998f49\",\"18014398509481984\":\"02f4e667567ebb9f4e6e180a4113bb071c48855f657766bb5e9c776a880335d1d6\",\"36028797018963968\":\"0385b4fe35e41703d7a657d957c67bb536629de57b7e6ee6fe2130728ef0fc90b0\",\"72057594037927936\":\"02b2bc1968a6fddbcc78fb9903940524824b5f5bed329c6ad48a19b56068c144fd\",\"144115188075855872\":\"02e0dbb24f1d288a693e8a49bc14264d1276be16972131520cf9e055ae92fba19a\",\"288230376151711744\":\"03efe75c106f931a525dc2d653ebedddc413a2c7d8cb9da410893ae7d2fa7d19cc\",\"576460752303423488\":\"02c7ec2bd9508a7fc03f73c7565dc600b30fd86f3d305f8f139c45c404a52d958a\",\"1152921504606846976\":\"035a6679c6b25e68ff4e29d1c7ef87f21e0a8fc574f6a08c1aa45ff352c1d59f06\",\"2305843009213693952\":\"033cdc225962c052d485f7cfbf55a5b2367d200fe1fe4373a347deb4cc99e9a099\",\"4611686018427387904\":\"024a4b806cf413d14b294719090a9da36ba75209c7657135ad09bc65328fba9e6f\",\"9223372036854775808\":\"0377a6fe114e291a8d8e991627c38001c8305b23b9e98b1c7b1893f5cd0dda6cad\"\n}")]
    public void Nut01Tests_Keysets_Valid(string keyset)
    {
        JsonSerializer.Deserialize<Keyset>(keyset);
    }

    [Theory]
    [InlineData("00456a94ab4e1c46",
        "{\n  \"1\":\"03a40f20667ed53513075dc51e715ff2046cad64eb68960632269ba7f0210e38bc\",\"2\":\"03fd4ce5a16b65576145949e6f99f445f8249fee17c606b688b504a849cdc452de\",\"4\":\"02648eccfa4c026960966276fa5a4cae46ce0fd432211a4f449bf84f13aa5f8303\",\"8\":\"02fdfd6796bfeac490cbee12f778f867f0a2c68f6508d17c649759ea0dc3547528\"\n}")]
    [InlineData("000f01df73ea149a",
        "{\n  \"1\":\"03ba786a2c0745f8c30e490288acd7a72dd53d65afd292ddefa326a4a3fa14c566\",\"2\":\"03361cd8bd1329fea797a6add1cf1990ffcf2270ceb9fc81eeee0e8e9c1bd0cdf5\",\"4\":\"036e378bcf78738ddf68859293c69778035740e41138ab183c94f8fee7572214c7\",\"8\":\"03909d73beaf28edfb283dbeb8da321afd40651e8902fcf5454ecc7d69788626c0\",\"16\":\"028a36f0e6638ea7466665fe174d958212723019ec08f9ce6898d897f88e68aa5d\",\"32\":\"03a97a40e146adee2687ac60c2ba2586a90f970de92a9d0e6cae5a4b9965f54612\",\"64\":\"03ce86f0c197aab181ddba0cfc5c5576e11dfd5164d9f3d4a3fc3ffbbf2e069664\",\"128\":\"0284f2c06d938a6f78794814c687560a0aabab19fe5e6f30ede38e113b132a3cb9\",\"256\":\"03b99f475b68e5b4c0ba809cdecaae64eade2d9787aa123206f91cd61f76c01459\",\"512\":\"03d4db82ea19a44d35274de51f78af0a710925fe7d9e03620b84e3e9976e3ac2eb\",\"1024\":\"031fbd4ba801870871d46cf62228a1b748905ebc07d3b210daf48de229e683f2dc\",\"2048\":\"0276cedb9a3b160db6a158ad4e468d2437f021293204b3cd4bf6247970d8aff54b\",\"4096\":\"02fc6b89b403ee9eb8a7ed457cd3973638080d6e04ca8af7307c965c166b555ea2\",\"8192\":\"0320265583e916d3a305f0d2687fcf2cd4e3cd03a16ea8261fda309c3ec5721e21\",\"16384\":\"036e41de58fdff3cb1d8d713f48c63bc61fa3b3e1631495a444d178363c0d2ed50\",\"32768\":\"0365438f613f19696264300b069d1dad93f0c60a37536b72a8ab7c7366a5ee6c04\",\"65536\":\"02408426cfb6fc86341bac79624ba8708a4376b2d92debdf4134813f866eb57a8d\",\"131072\":\"031063e9f11c94dc778c473e968966eac0e70b7145213fbaff5f7a007e71c65f41\",\"262144\":\"02f2a3e808f9cd168ec71b7f328258d0c1dda250659c1aced14c7f5cf05aab4328\",\"524288\":\"038ac10de9f1ff9395903bb73077e94dbf91e9ef98fd77d9a2debc5f74c575bc86\",\"1048576\":\"0203eaee4db749b0fc7c49870d082024b2c31d889f9bc3b32473d4f1dfa3625788\",\"2097152\":\"033cdb9d36e1e82ae652b7b6a08e0204569ec7ff9ebf85d80a02786dc7fe00b04c\",\"4194304\":\"02c8b73f4e3a470ae05e5f2fe39984d41e9f6ae7be9f3b09c9ac31292e403ac512\",\"8388608\":\"025bbe0cfce8a1f4fbd7f3a0d4a09cb6badd73ef61829dc827aa8a98c270bc25b0\",\"16777216\":\"037eec3d1651a30a90182d9287a5c51386fe35d4a96839cf7969c6e2a03db1fc21\",\"33554432\":\"03280576b81a04e6abd7197f305506476f5751356b7643988495ca5c3e14e5c262\",\"67108864\":\"03268bfb05be1dbb33ab6e7e00e438373ca2c9b9abc018fdb452d0e1a0935e10d3\",\"134217728\":\"02573b68784ceba9617bbcc7c9487836d296aa7c628c3199173a841e7a19798020\",\"268435456\":\"0234076b6e70f7fbf755d2227ecc8d8169d662518ee3a1401f729e2a12ccb2b276\",\"536870912\":\"03015bd88961e2a466a2163bd4248d1d2b42c7c58a157e594785e7eb34d880efc9\",\"1073741824\":\"02c9b076d08f9020ebee49ac8ba2610b404d4e553a4f800150ceb539e9421aaeee\",\"2147483648\":\"034d592f4c366afddc919a509600af81b489a03caf4f7517c2b3f4f2b558f9a41a\",\"4294967296\":\"037c09ecb66da082981e4cbdb1ac65c0eb631fc75d85bed13efb2c6364148879b5\",\"8589934592\":\"02b4ebb0dda3b9ad83b39e2e31024b777cc0ac205a96b9a6cfab3edea2912ed1b3\",\"17179869184\":\"026cc4dacdced45e63f6e4f62edbc5779ccd802e7fabb82d5123db879b636176e9\",\"34359738368\":\"02b2cee01b7d8e90180254459b8f09bbea9aad34c3a2fd98c85517ecfc9805af75\",\"68719476736\":\"037a0c0d564540fc574b8bfa0253cca987b75466e44b295ed59f6f8bd41aace754\",\"137438953472\":\"021df6585cae9b9ca431318a713fd73dbb76b3ef5667957e8633bca8aaa7214fb6\",\"274877906944\":\"02b8f53dde126f8c85fa5bb6061c0be5aca90984ce9b902966941caf963648d53a\",\"549755813888\":\"029cc8af2840d59f1d8761779b2496623c82c64be8e15f9ab577c657c6dd453785\",\"1099511627776\":\"03e446fdb84fad492ff3a25fc1046fb9a93a5b262ebcd0151caa442ea28959a38a\",\"2199023255552\":\"02d6b25bd4ab599dd0818c55f75702fde603c93f259222001246569018842d3258\",\"4398046511104\":\"03397b522bb4e156ec3952d3f048e5a986c20a00718e5e52cd5718466bf494156a\",\"8796093022208\":\"02d1fb9e78262b5d7d74028073075b80bb5ab281edcfc3191061962c1346340f1e\",\"17592186044416\":\"030d3f2ad7a4ca115712ff7f140434f802b19a4c9b2dd1c76f3e8e80c05c6a9310\",\"35184372088832\":\"03e325b691f292e1dfb151c3fb7cad440b225795583c32e24e10635a80e4221c06\",\"70368744177664\":\"03bee8f64d88de3dee21d61f89efa32933da51152ddbd67466bef815e9f93f8fd1\",\"140737488355328\":\"0327244c9019a4892e1f04ba3bf95fe43b327479e2d57c25979446cc508cd379ed\",\"281474976710656\":\"02fb58522cd662f2f8b042f8161caae6e45de98283f74d4e99f19b0ea85e08a56d\",\"562949953421312\":\"02adde4b466a9d7e59386b6a701a39717c53f30c4810613c1b55e6b6da43b7bc9a\",\"1125899906842624\":\"038eeda11f78ce05c774f30e393cda075192b890d68590813ff46362548528dca9\",\"2251799813685248\":\"02ec13e0058b196db80f7079d329333b330dc30c000dbdd7397cbbc5a37a664c4f\",\"4503599627370496\":\"02d2d162db63675bd04f7d56df04508840f41e2ad87312a3c93041b494efe80a73\",\"9007199254740992\":\"0356969d6aef2bb40121dbd07c68b6102339f4ea8e674a9008bb69506795998f49\",\"18014398509481984\":\"02f4e667567ebb9f4e6e180a4113bb071c48855f657766bb5e9c776a880335d1d6\",\"36028797018963968\":\"0385b4fe35e41703d7a657d957c67bb536629de57b7e6ee6fe2130728ef0fc90b0\",\"72057594037927936\":\"02b2bc1968a6fddbcc78fb9903940524824b5f5bed329c6ad48a19b56068c144fd\",\"144115188075855872\":\"02e0dbb24f1d288a693e8a49bc14264d1276be16972131520cf9e055ae92fba19a\",\"288230376151711744\":\"03efe75c106f931a525dc2d653ebedddc413a2c7d8cb9da410893ae7d2fa7d19cc\",\"576460752303423488\":\"02c7ec2bd9508a7fc03f73c7565dc600b30fd86f3d305f8f139c45c404a52d958a\",\"1152921504606846976\":\"035a6679c6b25e68ff4e29d1c7ef87f21e0a8fc574f6a08c1aa45ff352c1d59f06\",\"2305843009213693952\":\"033cdc225962c052d485f7cfbf55a5b2367d200fe1fe4373a347deb4cc99e9a099\",\"4611686018427387904\":\"024a4b806cf413d14b294719090a9da36ba75209c7657135ad09bc65328fba9e6f\",\"9223372036854775808\":\"0377a6fe114e291a8d8e991627c38001c8305b23b9e98b1c7b1893f5cd0dda6cad\"\n}")]
    public void Nut02Tests_KeysetIdMatch(string keysetId, string keyset)
    {
        var keysetIdParsed = new KeysetId(keysetId);
        var keysetParsed = JsonSerializer.Deserialize<Keyset>(keyset);
        Assert.Equal(keysetIdParsed, keysetParsed.GetKeysetId());
    }

    [Fact]
    public void Nut04Tests_Proofs_1()
    {
        var a = "0000000000000000000000000000000000000000000000000000000000000001".ToPrivKey();
        var A = a.CreatePubKey();
        Assert.Equal("0279be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798".ToPubKey(), A);
        var message = "secret_msg";
        var blindingFactor = "0000000000000000000000000000000000000000000000000000000000000001".ToPrivKey();
        var Y = Cashu.MessageToCurve(message);
        var B_ = Cashu.ComputeB_(Y, blindingFactor);
        var C_ = Cashu.ComputeC_(B_, a);
        var proof = Cashu.ComputeProof(B_, a, blindingFactor);
        Cashu.VerifyProof(B_, C_, proof.e, proof.s, A);
        var C = Cashu.ComputeC(C_, blindingFactor, A);

        Cashu.VerifyProof(Y, blindingFactor, C, proof.e, proof.s, A);
    }


    [Fact]
    public void Nut04Tests_Proofs_2()
    {
        var A = "0279be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798".ToPubKey();
        var proof = JsonSerializer.Deserialize<Proof>(@"

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

");

        Assert.NotNull(proof?.DLEQ);
        Cashu.VerifyProof(Cashu.HexToCurve(Assert.IsType<StringSecret>(proof.Secret).Secret), proof.DLEQ.R, proof.C,
            proof.DLEQ.E, proof.DLEQ.S, A);
    }

    [Fact]
    public void Nut11_Signatures()
    {
        var secretKey =
            ECPrivKey.Create(Convert.FromHexString("99590802251e78ee1051648439eedb003dc539093a48a44e7b8f2642c909ea37"));

        var signing_key_two =
            ECPrivKey.Create(Convert.FromHexString("0000000000000000000000000000000000000000000000000000000000000001"));

        var signing_key_three =
            ECPrivKey.Create(Convert.FromHexString("7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f"));

        var conditions = new P2PkBuilder()
        {
            Lock = DateTimeOffset.FromUnixTimeSeconds(21000000000),
            Pubkeys = new[] {signing_key_two.CreatePubKey(), signing_key_three.CreatePubKey()},
            RefundPubkeys = new[] {secretKey.CreatePubKey()},
            SignatureThreshold = 2,
            SigFlag = "SIG_INPUTS"
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
        var witness = p2pkProofSecret.GenerateWitness(proof, new[] {signing_key_two, signing_key_three});
        proof.Witness = JsonSerializer.Serialize(witness);
        Assert.True(p2pkProofSecret.VerifyWitness(proof.Secret, witness));


        var valid1 =
            "{\"amount\":1,\"secret\":\"[\\\"P2PK\\\",{\\\"nonce\\\":\\\"859d4935c4907062a6297cf4e663e2835d90d97ecdd510745d32f6816323a41f\\\",\\\"data\\\":\\\"0249098aa8b9d2fbec49ff8598feb17b592b986e62319a4fa488a3dc36387157a7\\\",\\\"tags\\\":[[\\\"sigflag\\\",\\\"SIG_INPUTS\\\"]]}]\",\"C\":\"02698c4e2b5f9534cd0687d87513c759790cf829aa5739184a3e3735471fbda904\",\"id\":\"009a1f293253e41e\",\"witness\":\"{\\\"signatures\\\":[\\\"60f3c9b766770b46caac1d27e1ae6b77c8866ebaeba0b9489fe6a15a837eaa6fcd6eaa825499c72ac342983983fd3ba3a8a41f56677cc99ffd73da68b59e1383\\\"]}\"}";
        var valid1Proof = JsonSerializer.Deserialize<Proof>(valid1);
        var valid1ProofSecret = Assert.IsType<Nut10Secret>(valid1Proof.Secret);
        Assert.Equal(P2PKProofSecret.Key, valid1ProofSecret!.Key);
        var valid1ProofSecretp2pkValue = Assert.IsType<P2PKProofSecret>(valid1ProofSecret.ProofSecret);
        var valid1ProofWitnessP2pk = JsonSerializer.Deserialize<P2PKWitness>(valid1Proof.Witness);
        Assert.True(valid1ProofSecretp2pkValue.VerifyWitness(valid1Proof.Secret, valid1ProofWitnessP2pk));

        var invalid1 =
            @"{""amount"":1,""secret"":""[\""P2PK\"",{\""nonce\"":\""0ed3fcb22c649dd7bbbdcca36e0c52d4f0187dd3b6a19efcc2bfbebb5f85b2a1\"",\""data\"":\""0249098aa8b9d2fbec49ff8598feb17b592b986e62319a4fa488a3dc36387157a7\"",\""tags\"":[[\""pubkeys\"",\""0279be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798\"",\""02142715675faf8da1ecc4d51e0b9e539fa0d52fdd96ed60dbe99adb15d6b05ad9\""],[\""n_sigs\"",\""2\""],[\""sigflag\"",\""SIG_INPUTS\""]]}]"",""C"":""02698c4e2b5f9534cd0687d87513c759790cf829aa5739184a3e3735471fbda904"",""id"":""009a1f293253e41e"",""witness"":""{\""signatures\"":[\""83564aca48c668f50d022a426ce0ed19d3a9bdcffeeaee0dc1e7ea7e98e9eff1840fcc821724f623468c94f72a8b0a7280fa9ef5a54a1b130ef3055217f467b3\""]}""}";

        var invalid1Proof = JsonSerializer.Deserialize<Proof>(invalid1);
        var invalid1ProofSecret = Assert.IsType<Nut10Secret>(invalid1Proof.Secret);
        Assert.Equal(P2PKProofSecret.Key, invalid1ProofSecret!.Key);
        var invalid1ProofSecretp2pkValue = Assert.IsType<P2PKProofSecret>(invalid1ProofSecret.ProofSecret);
        var invalid1ProofWitnessP2pk = JsonSerializer.Deserialize<P2PKWitness>(invalid1Proof.Witness);
        Assert.False(invalid1ProofSecretp2pkValue.VerifyWitness(invalid1Proof.Secret, invalid1ProofWitnessP2pk));

        var validMultisig =
            "{\"amount\":1,\"secret\":\"[\\\"P2PK\\\",{\\\"nonce\\\":\\\"0ed3fcb22c649dd7bbbdcca36e0c52d4f0187dd3b6a19efcc2bfbebb5f85b2a1\\\",\\\"data\\\":\\\"0249098aa8b9d2fbec49ff8598feb17b592b986e62319a4fa488a3dc36387157a7\\\",\\\"tags\\\":[[\\\"pubkeys\\\",\\\"0279be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798\\\",\\\"02142715675faf8da1ecc4d51e0b9e539fa0d52fdd96ed60dbe99adb15d6b05ad9\\\"],[\\\"n_sigs\\\",\\\"2\\\"],[\\\"sigflag\\\",\\\"SIG_INPUTS\\\"]]}]\",\"C\":\"02698c4e2b5f9534cd0687d87513c759790cf829aa5739184a3e3735471fbda904\",\"id\":\"009a1f293253e41e\",\"witness\":\"{\\\"signatures\\\":[\\\"83564aca48c668f50d022a426ce0ed19d3a9bdcffeeaee0dc1e7ea7e98e9eff1840fcc821724f623468c94f72a8b0a7280fa9ef5a54a1b130ef3055217f467b3\\\",\\\"9a72ca2d4d5075be5b511ee48dbc5e45f259bcf4a4e8bf18587f433098a9cd61ff9737dc6e8022de57c76560214c4568377792d4c2c6432886cc7050487a1f22\\\"]}\"}";
        var validMultisigProof = JsonSerializer.Deserialize<Proof>(validMultisig);
        var validMultisigProofSecret = Assert.IsType<Nut10Secret>(validMultisigProof.Secret);
        Assert.Equal(P2PKProofSecret.Key, validMultisigProofSecret!.Key);
        var validMultisigProofSecretp2pkValue = Assert.IsType<P2PKProofSecret>(validMultisigProofSecret.ProofSecret);
        var validMultisigProofWitnessP2pk = JsonSerializer.Deserialize<P2PKWitness>(validMultisigProof.Witness);
        Assert.True(
            validMultisigProofSecretp2pkValue.VerifyWitness(validMultisigProof.Secret, validMultisigProofWitnessP2pk));

        var invalidMultisig =
            "{\"amount\":1,\"secret\":\"[\\\"P2PK\\\",{\\\"nonce\\\":\\\"0ed3fcb22c649dd7bbbdcca36e0c52d4f0187dd3b6a19efcc2bfbebb5f85b2a1\\\",\\\"data\\\":\\\"0249098aa8b9d2fbec49ff8598feb17b592b986e62319a4fa488a3dc36387157a7\\\",\\\"tags\\\":[[\\\"pubkeys\\\",\\\"0279be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798\\\",\\\"02142715675faf8da1ecc4d51e0b9e539fa0d52fdd96ed60dbe99adb15d6b05ad9\\\"],[\\\"n_sigs\\\",\\\"2\\\"],[\\\"sigflag\\\",\\\"SIG_INPUTS\\\"]]}]\",\"C\":\"02698c4e2b5f9534cd0687d87513c759790cf829aa5739184a3e3735471fbda904\",\"id\":\"009a1f293253e41e\",\"witness\":\"{\\\"signatures\\\":[\\\"83564aca48c668f50d022a426ce0ed19d3a9bdcffeeaee0dc1e7ea7e98e9eff1840fcc821724f623468c94f72a8b0a7280fa9ef5a54a1b130ef3055217f467b3\\\"]}\"}";
        var invalidMultisigProof = JsonSerializer.Deserialize<Proof>(invalidMultisig);
        var invalidMultisigProofSecret = Assert.IsType<Nut10Secret>(invalidMultisigProof.Secret);
        Assert.Equal(P2PKProofSecret.Key, invalidMultisigProofSecret!.Key);
        var invalidMultisigProofSecretp2pkValue =
            Assert.IsType<P2PKProofSecret>(invalidMultisigProofSecret.ProofSecret);
        var invalidMultisigProofWitnessP2pk = JsonSerializer.Deserialize<P2PKWitness>(invalidMultisigProof.Witness);
        Assert.False(invalidMultisigProofSecretp2pkValue.VerifyWitness(invalidMultisigProof.Secret,
            invalidMultisigProofWitnessP2pk));

        var validProofRefund =
            "{\"amount\":1,\"id\":\"009a1f293253e41e\",\"secret\":\"[\\\"P2PK\\\",{\\\"nonce\\\":\\\"902685f492ef3bb2ca35a47ddbba484a3365d143b9776d453947dcbf1ddf9689\\\",\\\"data\\\":\\\"026f6a2b1d709dbca78124a9f30a742985f7eddd894e72f637f7085bf69b997b9a\\\",\\\"tags\\\":[[\\\"pubkeys\\\",\\\"0279be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798\\\",\\\"03142715675faf8da1ecc4d51e0b9e539fa0d52fdd96ed60dbe99adb15d6b05ad9\\\"],[\\\"locktime\\\",\\\"21\\\"],[\\\"n_sigs\\\",\\\"2\\\"],[\\\"refund\\\",\\\"026f6a2b1d709dbca78124a9f30a742985f7eddd894e72f637f7085bf69b997b9a\\\"],[\\\"sigflag\\\",\\\"SIG_INPUTS\\\"]]}]\",\"C\":\"02698c4e2b5f9534cd0687d87513c759790cf829aa5739184a3e3735471fbda904\",\"witness\":\"{\\\"signatures\\\":[\\\"710507b4bc202355c91ea3c147c0d0189c75e179d995e566336afd759cb342bcad9a593345f559d9b9e108ac2c9b5bd9f0b4b6a295028a98606a0a2e95eb54f7\\\"]}\"}";
        var validProofRefundParsed = JsonSerializer.Deserialize<Proof>(validProofRefund);
        var validProofRefundSecret = Assert.IsType<Nut10Secret>(validProofRefundParsed.Secret);
        Assert.Equal(P2PKProofSecret.Key, validProofRefundSecret!.Key);
        var validProofRefundSecretp2pkValue = Assert.IsType<P2PKProofSecret>(validProofRefundSecret.ProofSecret);
        var validProofRefundWitnessP2pk = JsonSerializer.Deserialize<P2PKWitness>(validProofRefundParsed.Witness);
        Assert.True(
            validProofRefundSecretp2pkValue.VerifyWitness(validProofRefundParsed.Secret, validProofRefundWitnessP2pk));


        var invalidProofRefund =
            "{\"amount\":1,\"id\":\"009a1f293253e41e\",\"secret\":\"[\\\"P2PK\\\",{\\\"nonce\\\":\\\"64c46e5d30df27286166814b71b5d69801704f23a7ad626b05688fbdb48dcc98\\\",\\\"data\\\":\\\"026f6a2b1d709dbca78124a9f30a742985f7eddd894e72f637f7085bf69b997b9a\\\",\\\"tags\\\":[[\\\"pubkeys\\\",\\\"0279be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798\\\",\\\"03142715675faf8da1ecc4d51e0b9e539fa0d52fdd96ed60dbe99adb15d6b05ad9\\\"],[\\\"locktime\\\",\\\"21\\\"],[\\\"n_sigs\\\",\\\"2\\\"],[\\\"refund\\\",\\\"0279be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798\\\"],[\\\"sigflag\\\",\\\"SIG_INPUTS\\\"]]}]\",\"C\":\"02698c4e2b5f9534cd0687d87513c759790cf829aa5739184a3e3735471fbda904\",\"witness\":\"{\\\"signatures\\\":[\\\"f661d3dc046d636d47cb3d06586da42c498f0300373d1c2a4f417a44252cdf3809bce207c8888f934dba0d2b1671f1b8622d526840f2d5883e571b462630c1ff\\\"]}\"}";
        var invalidProofRefundParsed = JsonSerializer.Deserialize<Proof>(invalidProofRefund);
        var invalidProofRefundSecret = Assert.IsType<Nut10Secret>(invalidProofRefundParsed.Secret);
        Assert.Equal(P2PKProofSecret.Key, invalidProofRefundSecret!.Key);
        var invalidProofRefundSecretp2pkValue = Assert.IsType<P2PKProofSecret>(invalidProofRefundSecret.ProofSecret);
        var invalidProofRefundWitnessP2pk = JsonSerializer.Deserialize<P2PKWitness>(invalidProofRefundParsed.Witness);
        Assert.False(invalidProofRefundSecretp2pkValue.VerifyWitness(invalidProofRefundParsed.Secret,
            invalidProofRefundWitnessP2pk));
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
            "{\n  \"amount\": 8,\n  \"id\": \"00882760bfa2eb41\",\n  \"C_\": \"02a9acc1e48c25eeeb9289b5031cc57da9fe72f3fe2861d264bdc074209b107ba2\",\n  \"dleq\": {\n    \"e\": \"9818e061ee51d5c8edc3342369a554998ff7b4381c8652d724cdf46429be73d9\",\n    \"s\": \"9818e061ee51d5c8edc3342369a554998ff7b4381c8652d724cdf46429be73da\"\n  }\n}");

        Assert.NotNull(blindSig?.DLEQ);
        blindSig.Verify(A, B_);
    }

    [Fact]
    public void Nut12Tests_ProofDLEQ()
    {
        var A = "0279be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798".ToPubKey();
        var proof = JsonSerializer.Deserialize<Proof>(
            "{\"amount\": 1,\"id\": \"00882760bfa2eb41\",\"secret\": \"daf4dd00a2b68a0858a80450f52c8a7d2ccf87d375e43e216e0c571f089f63e9\",\"C\": \"024369d2d22a80ecf78f3937da9d5f30c1b9f74f0c32684d583cca0fa6a61cdcfc\",\"dleq\": {\"e\": \"b31e58ac6527f34975ffab13e70a48b6d2b0d35abc4b03f0151f09ee1a9763d4\",\"s\": \"8fbae004c59e754d71df67e392b6ae4e29293113ddc2ec86592a0431d16306d8\",\"r\": \"a6d13fcd7a18442e6076f5e1e7c887ad5de40a019824bdfa9fe740d302e8d861\"}}");
        Assert.NotNull(proof?.DLEQ);
        Assert.Equal("024369d2d22a80ecf78f3937da9d5f30c1b9f74f0c32684d583cca0fa6a61cdcfc".ToPubKey(),
            proof.Secret.ToCurve());
        Assert.True(proof.Verify(A));
    }

    [Fact]
    public void Nut14Tests_HTLCSecret()
    {
        var htlcSecretStr =
            "[\n  \"HTLC\",\n  {\n    \"nonce\": \"da62796403af76c80cd6ce9153ed3746\",\n    \"data\": \"023192200a0cfd3867e48eb63b03ff599c7e46c8f4e41146b2d281173ca6c50c54\",\n    \"tags\": [\n      [\n        \"pubkeys\",\n        \"02698c4e2b5f9534cd0687d87513c759790cf829aa5739184a3e3735471fbda904\"\n      ],\n      [\n        \"locktime\",\n        \"1689418329\"\n      ],                   \n      [\n        \"refund\",\n        \"033281c37677ea273eb7183b783067f5244933ef78d8c3f15b1a77cb246099c26e\"\n      ]\n    ]\n  }\n]";
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
        Assert.Equal("https://nofees.testnut.cashu.space", Assert.Single( pr.Mints));
        Assert.Equal(10, pr.Amount);
        Assert.Equal("b7a90176", pr.PaymentId);
        Assert.Equal("sat", pr.Unit);
        var t = Assert.Single(pr.Transports);
        Assert.Equal("nostr", t.Type);
        Assert.Equal("nprofile1qy28wumn8ghj7un9d3shjtnyv9kh2uewd9hsz9mhwden5te0wfjkccte9curxven9eehqctrv5hszrthwden5te0dehhxtnvdakqqgydaqy7curk439ykptkysv7udhdhu68sucm295akqefdehkf0d495cwunl5", t.Target);
        Assert.Equal("n",Assert.Single(t.Tags).Key );
        Assert.Equal("17",Assert.Single(t.Tags).Value );
        Assert.Equal(creqA, pr.ToString());

    }
    
    internal readonly struct TestCase(in string path, in string keyHex, in string ccHex)
    {
        internal static readonly ReadOnlyMemory<byte> Seed = Convert.FromHexString(
            "e4a964f4973ce5750a6a5a5126e8258442c197b2e71b683ccba58688f21242eae1b0f12bee21d6e983d4a5c61f081bf3f0669546eb576dec1b22ec8d481b00fb");

        internal readonly ReadOnlyMemory<byte> Key = Convert.FromHexString(keyHex);
        internal readonly ReadOnlyMemory<byte> ChainCode = Convert.FromHexString(ccHex);

        internal readonly KeyPath Path = path;
    }
    
    private static readonly TestCase Case1SecP256K1 = new(
        "m/0'/0/0",
        "6144c1daf8222d6dab77e7a20c2f338519b83bd1423602c56c7dfb5e9ea99c02",
        "55b36970e7ab8434f9b04f1c2e52da7422d2bce7e284ca353419dddfa2e34bdb");

    [Fact]
    public void Bip32Test()
    {
        var masterKeyFromSeed = BIP32.Instance.GetMasterKeyFromSeed(TestCase.Seed.Span);
        
        Assert.Equal("5A876CC4B4AB2F6717951AEE7F97AB69844DBFFFF7074E6E6F71D2BA04BD6EC9", Convert.ToHexString( masterKeyFromSeed.ChainCode));
        Assert.Equal("8D18D3F0CF9D74B53A935D97E8DE85955ED9F6EEFC6D6D45F0C169031A11B669", Convert.ToHexString( masterKeyFromSeed.PrivateKey));
        
        
        Assert.Equal("026cf0d14fcfa930347e7da26281319ac5959d02f1b6331812261efdb7e347788b",ECPrivKey.Create(masterKeyFromSeed.PrivateKey).CreatePubKey().ToHex());
        
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
        var mnemonicPhrase = "half depart obvious quality work element tank gorilla view sugar picture humble";
        var mnemonic = new Mnemonic(mnemonicPhrase);
        Assert.Equal("dd44ee516b0647e80b488e8dcc56d736a148f15276bef588b37057476d4b2b25780d3688a32b37353d6995997842c0fd8b412475c891c16310471fbc86dcbda8", 
            Convert.ToHexString(mnemonic.DeriveSeed()).ToLowerInvariant());
        
        Assert.Equal("m/129372'/0'/864559728'/0'/0", Nut13.GetNut13DerivationPath(keysetId, 0, true));
        Assert.Equal("m/129372'/0'/864559728'/0'/1", Nut13.GetNut13DerivationPath(keysetId, 0, false));
        
        Assert.Equal("485875df74771877439ac06339e284c3acfcd9be7abf3bc20b516faeadfe77ae",
            mnemonic.DeriveSecret(keysetId, 0).Secret);
        Assert.Equal("8f2b39e8e594a4056eb1e6dbb4b0c38ef13b1b2c751f64f810ec04ee35b77270",
            mnemonic.DeriveSecret(keysetId, 1).Secret);
        Assert.Equal("bc628c79accd2364fd31511216a0fab62afd4a18ff77a20deded7b858c9860c8",
            mnemonic.DeriveSecret(keysetId, 2).Secret);
        Assert.Equal("59284fd1650ea9fa17db2b3acf59ecd0f2d52ec3261dd4152785813ff27a33bf",
            mnemonic.DeriveSecret(keysetId, 3).Secret);
        Assert.Equal("576c23393a8b31cc8da6688d9c9a96394ec74b40fdaf1f693a6bb84284334ea0",
            mnemonic.DeriveSecret(keysetId, 4).Secret);

        Assert.Equal("ad00d431add9c673e843d4c2bf9a778a5f402b985b8da2d5550bf39cda41d679",
            Convert.ToHexString(mnemonic.DeriveBlindingFactor(keysetId, 0)).ToLowerInvariant());
        Assert.Equal("967d5232515e10b81ff226ecf5a9e2e2aff92d66ebc3edf0987eb56357fd6248",
            Convert.ToHexString(mnemonic.DeriveBlindingFactor(keysetId, 1)).ToLowerInvariant());
        Assert.Equal("b20f47bb6ae083659f3aa986bfa0435c55c6d93f687d51a01f26862d9b9a4899",
            Convert.ToHexString(mnemonic.DeriveBlindingFactor(keysetId, 2)).ToLowerInvariant());
        Assert.Equal("fb5fca398eb0b1deb955a2988b5ac77d32956155f1c002a373535211a2dfdc29",
            Convert.ToHexString(mnemonic.DeriveBlindingFactor(keysetId, 3)).ToLowerInvariant());
        Assert.Equal("5f09bfbfe27c439a597719321e061e2e40aad4a36768bb2bcc3de547c9644bf9",
            Convert.ToHexString(mnemonic.DeriveBlindingFactor(keysetId, 4)).ToLowerInvariant());
    }
}