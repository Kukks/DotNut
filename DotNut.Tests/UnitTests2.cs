using System.Diagnostics.Metrics;
using System.Text.Json;
using System.Text.Json.Serialization;
using DotNut.Abstractions;
using DotNut.Api;
using DotNut.ApiModels;
using DotNut.NBitcoin.BIP39;
using NBitcoin.Secp256k1;

namespace DotNut.Tests;
public class UnitTests2
{
    private static string MintUrl = "http://localhost:3338";

    [Fact]
    public void CreatesWalletSuccesfully()
    {
        var wallet = Wallet.Create();
        Assert.NotNull(wallet);
    }
    
    [Fact]
    public async Task ThrowsWhenMintNotFound()
    {
        var wallet = Wallet.Create();
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await wallet.GetInfo());
        await Assert.ThrowsAsync<ArgumentNullException>(async () => wallet.Restore());
        await Assert.ThrowsAsync<ArgumentNullException>(async () => wallet.Swap());
        await Assert.ThrowsAsync<ArgumentNullException>(async () => wallet.CreateMeltQuote());
        await Assert.ThrowsAsync<ArgumentNullException>(async () => wallet.CreateMintQuote());
    }
    
    [Fact]
    public void BuilderChainingPreservesAllSettings()
    {
        var counter = new InMemoryCounter();
        var info = new MintInfo(new GetInfoResponse { Version = "0.15.0" });
        var keysets = new GetKeysetsResponse { Keysets = [] };
        var keys = new GetKeysResponse { Keysets = [] };
        var selector = new ProofSelector(new Dictionary<KeysetId, ulong>());
        var mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
    
        var wallet = Wallet.Create()
            .WithMint(MintUrl)
            .WithInfo(info)
            .WithKeysets(keysets)
            .WithKeys(keys)
            .WithSelector(selector)
            .WithMnemonic(mnemonic)
            .WithCounter(counter)
            .WithKeysetSync(true)
            .ShouldBumpCounter(false);
    
        var mnemonicField = wallet.GetType()
            .GetField("_mnemonic", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
        var mnemonicRef = (Mnemonic?)mnemonicField?.GetValue(wallet);
        
        var counterField = wallet.GetType()
            .GetField("_counter",System.Reflection.BindingFlags.NonPublic | 
                                 System.Reflection.BindingFlags.Instance);
        var counterRef = (InMemoryCounter?)counterField?.GetValue(wallet);
        
        Assert.Equal(mnemonic, mnemonicRef.ToString());
        Assert.Same(counter, counterRef);
        Assert.NotNull(wallet.GetInfo());
    }
    [Fact]
    public void WithMintStringVariantCreatesHttpClient()
    {
        var wallet = Wallet.Create().WithMint(MintUrl);
        var api = wallet.GetMintApi().Result;
        Assert.NotNull(api);
    }

    [Fact]
    public async Task InMemoryCounter()
    {
        var ctr = new InMemoryCounter();
        Assert.NotNull(ctr);
        var testId1 = new KeysetId("00qwertyuiopasdf");
        var ctrNum = await ctr.GetCounterForId(testId1);
        Assert.Equal((uint)0, ctrNum);
        
        await ctr.IncrementCounter(testId1);
        Assert.Equal((uint)0, ctrNum);
        ctrNum = await ctr.GetCounterForId(testId1);
        Assert.Equal((uint)1, ctrNum);
        
        await ctr.IncrementCounter(testId1, 5);
        ctrNum = await ctr.GetCounterForId(testId1);
        Assert.Equal((uint)6, ctrNum);
        
        await ctr.SetCounter(testId1, 1337);
        ctrNum = await ctr.GetCounterForId(testId1);
        Assert.Equal((uint)1337, ctrNum);
    }
    
    [Fact]
    public void SplitAmountsForPayment_ExactAmount_ReturnsCorrectSplit()
    {
        var amounts = Utils.SplitToProofsAmounts(30, _testKeyset);
        Assert.Equal(new List<ulong>(){16, 8, 4, 2}, amounts);

    }

    private Keyset? _testKeyset = JsonSerializer.Deserialize<Keyset>(
        "{\n  \"1\": \"03ba786a2c0745f8c30e490288acd7a72dd53d65afd292ddefa326a4a3fa14c566\",\n  \"2\": \"03361cd8bd1329fea797a6add1cf1990ffcf2270ceb9fc81eeee0e8e9c1bd0cdf5\",\n  \"4\": \"036e378bcf78738ddf68859293c69778035740e41138ab183c94f8fee7572214c7\",\n  \"8\": \"03909d73beaf28edfb283dbeb8da321afd40651e8902fcf5454ecc7d69788626c0\",\n  \"16\": \"028a36f0e6638ea7466665fe174d958212723019ec08f9ce6898d897f88e68aa5d\",\n  \"32\": \"03a97a40e146adee2687ac60c2ba2586a90f970de92a9d0e6cae5a4b9965f54612\",\n  \"64\": \"03ce86f0c197aab181ddba0cfc5c5576e11dfd5164d9f3d4a3fc3ffbbf2e069664\",\n  \"128\": \"0284f2c06d938a6f78794814c687560a0aabab19fe5e6f30ede38e113b132a3cb9\",\n  \"256\": \"03b99f475b68e5b4c0ba809cdecaae64eade2d9787aa123206f91cd61f76c01459\",\n  \"512\": \"03d4db82ea19a44d35274de51f78af0a710925fe7d9e03620b84e3e9976e3ac2eb\",\n  \"1024\": \"031fbd4ba801870871d46cf62228a1b748905ebc07d3b210daf48de229e683f2dc\",\n  \"2048\": \"0276cedb9a3b160db6a158ad4e468d2437f021293204b3cd4bf6247970d8aff54b\",\n  \"4096\": \"02fc6b89b403ee9eb8a7ed457cd3973638080d6e04ca8af7307c965c166b555ea2\",\n  \"8192\": \"0320265583e916d3a305f0d2687fcf2cd4e3cd03a16ea8261fda309c3ec5721e21\",\n  \"16384\": \"036e41de58fdff3cb1d8d713f48c63bc61fa3b3e1631495a444d178363c0d2ed50\",\n  \"32768\": \"0365438f613f19696264300b069d1dad93f0c60a37536b72a8ab7c7366a5ee6c04\",\n  \"65536\": \"02408426cfb6fc86341bac79624ba8708a4376b2d92debdf4134813f866eb57a8d\",\n  \"131072\": \"031063e9f11c94dc778c473e968966eac0e70b7145213fbaff5f7a007e71c65f41\",\n  \"262144\": \"02f2a3e808f9cd168ec71b7f328258d0c1dda250659c1aced14c7f5cf05aab4328\",\n  \"524288\": \"038ac10de9f1ff9395903bb73077e94dbf91e9ef98fd77d9a2debc5f74c575bc86\",\n  \"1048576\": \"0203eaee4db749b0fc7c49870d082024b2c31d889f9bc3b32473d4f1dfa3625788\",\n  \"2097152\": \"033cdb9d36e1e82ae652b7b6a08e0204569ec7ff9ebf85d80a02786dc7fe00b04c\",\n  \"4194304\": \"02c8b73f4e3a470ae05e5f2fe39984d41e9f6ae7be9f3b09c9ac31292e403ac512\",\n  \"8388608\": \"025bbe0cfce8a1f4fbd7f3a0d4a09cb6badd73ef61829dc827aa8a98c270bc25b0\",\n  \"16777216\": \"037eec3d1651a30a90182d9287a5c51386fe35d4a96839cf7969c6e2a03db1fc21\",\n  \"33554432\": \"03280576b81a04e6abd7197f305506476f5751356b7643988495ca5c3e14e5c262\",\n  \"67108864\": \"03268bfb05be1dbb33ab6e7e00e438373ca2c9b9abc018fdb452d0e1a0935e10d3\",\n  \"134217728\": \"02573b68784ceba9617bbcc7c9487836d296aa7c628c3199173a841e7a19798020\",\n  \"268435456\": \"0234076b6e70f7fbf755d2227ecc8d8169d662518ee3a1401f729e2a12ccb2b276\",\n  \"536870912\": \"03015bd88961e2a466a2163bd4248d1d2b42c7c58a157e594785e7eb34d880efc9\",\n  \"1073741824\": \"02c9b076d08f9020ebee49ac8ba2610b404d4e553a4f800150ceb539e9421aaeee\",\n  \"2147483648\": \"034d592f4c366afddc919a509600af81b489a03caf4f7517c2b3f4f2b558f9a41a\",\n  \"4294967296\": \"037c09ecb66da082981e4cbdb1ac65c0eb631fc75d85bed13efb2c6364148879b5\",\n  \"8589934592\": \"02b4ebb0dda3b9ad83b39e2e31024b777cc0ac205a96b9a6cfab3edea2912ed1b3\",\n  \"17179869184\": \"026cc4dacdced45e63f6e4f62edbc5779ccd802e7fabb82d5123db879b636176e9\",\n  \"34359738368\": \"02b2cee01b7d8e90180254459b8f09bbea9aad34c3a2fd98c85517ecfc9805af75\",\n  \"68719476736\": \"037a0c0d564540fc574b8bfa0253cca987b75466e44b295ed59f6f8bd41aace754\",\n  \"137438953472\": \"021df6585cae9b9ca431318a713fd73dbb76b3ef5667957e8633bca8aaa7214fb6\",\n  \"274877906944\": \"02b8f53dde126f8c85fa5bb6061c0be5aca90984ce9b902966941caf963648d53a\",\n  \"549755813888\": \"029cc8af2840d59f1d8761779b2496623c82c64be8e15f9ab577c657c6dd453785\",\n  \"1099511627776\": \"03e446fdb84fad492ff3a25fc1046fb9a93a5b262ebcd0151caa442ea28959a38a\",\n  \"2199023255552\": \"02d6b25bd4ab599dd0818c55f75702fde603c93f259222001246569018842d3258\",\n  \"4398046511104\": \"03397b522bb4e156ec3952d3f048e5a986c20a00718e5e52cd5718466bf494156a\",\n  \"8796093022208\": \"02d1fb9e78262b5d7d74028073075b80bb5ab281edcfc3191061962c1346340f1e\",\n  \"17592186044416\": \"030d3f2ad7a4ca115712ff7f140434f802b19a4c9b2dd1c76f3e8e80c05c6a9310\",\n  \"35184372088832\": \"03e325b691f292e1dfb151c3fb7cad440b225795583c32e24e10635a80e4221c06\",\n  \"70368744177664\": \"03bee8f64d88de3dee21d61f89efa32933da51152ddbd67466bef815e9f93f8fd1\",\n  \"140737488355328\": \"0327244c9019a4892e1f04ba3bf95fe43b327479e2d57c25979446cc508cd379ed\",\n  \"281474976710656\": \"02fb58522cd662f2f8b042f8161caae6e45de98283f74d4e99f19b0ea85e08a56d\",\n  \"562949953421312\": \"02adde4b466a9d7e59386b6a701a39717c53f30c4810613c1b55e6b6da43b7bc9a\",\n  \"1125899906842624\": \"038eeda11f78ce05c774f30e393cda075192b890d68590813ff46362548528dca9\",\n  \"2251799813685248\": \"02ec13e0058b196db80f7079d329333b330dc30c000dbdd7397cbbc5a37a664c4f\",\n  \"4503599627370496\": \"02d2d162db63675bd04f7d56df04508840f41e2ad87312a3c93041b494efe80a73\",\n  \"9007199254740992\": \"0356969d6aef2bb40121dbd07c68b6102339f4ea8e674a9008bb69506795998f49\",\n  \"18014398509481984\": \"02f4e667567ebb9f4e6e180a4113bb071c48855f657766bb5e9c776a880335d1d6\",\n  \"36028797018963968\": \"0385b4fe35e41703d7a657d957c67bb536629de57b7e6ee6fe2130728ef0fc90b0\",\n  \"72057594037927936\": \"02b2bc1968a6fddbcc78fb9903940524824b5f5bed329c6ad48a19b56068c144fd\",\n  \"144115188075855872\": \"02e0dbb24f1d288a693e8a49bc14264d1276be16972131520cf9e055ae92fba19a\",\n  \"288230376151711744\": \"03efe75c106f931a525dc2d653ebedddc413a2c7d8cb9da410893ae7d2fa7d19cc\",\n  \"576460752303423488\": \"02c7ec2bd9508a7fc03f73c7565dc600b30fd86f3d305f8f139c45c404a52d958a\",\n  \"1152921504606846976\": \"035a6679c6b25e68ff4e29d1c7ef87f21e0a8fc574f6a08c1aa45ff352c1d59f06\",\n  \"2305843009213693952\": \"033cdc225962c052d485f7cfbf55a5b2367d200fe1fe4373a347deb4cc99e9a099\",\n  \"4611686018427387904\": \"024a4b806cf413d14b294719090a9da36ba75209c7657135ad09bc65328fba9e6f\",\n  \"9223372036854775808\": \"0377a6fe114e291a8d8e991627c38001c8305b23b9e98b1c7b1893f5cd0dda6cad\"\n}");

    private static KeysetId _testKeysetId = new KeysetId("000f01df73ea149a");

    [Fact]
    public void SumProofs_EmptyList_ReturnsZero()
    {
        var proofs = new List<Proof>();
        var sum = Utils.SumProofs(proofs);
        Assert.Equal(0UL, sum);
    }

    [Fact]
    public void SumProofs_SingleProof_ReturnsAmount()
    {
        var proofs = new List<Proof>
        {
            new Proof { Amount = 64 }
        };
        var sum = Utils.SumProofs(proofs);
        Assert.Equal(64UL, sum);
    }

    [Fact]
    public void SumProofs_MultipleProofs_ReturnsCorrectSum()
    {
        var proofs = new List<Proof>
        {
            new Proof { Amount = 1 },
            new Proof { Amount = 2 },
            new Proof { Amount = 4 },
            new Proof { Amount = 8 },
            new Proof { Amount = 16 }
        };
        var sum = Utils.SumProofs(proofs);
        Assert.Equal(31UL, sum);
    }

    [Theory]
    [InlineData(1UL, new ulong[] { 1 })]
    [InlineData(2UL, new ulong[] { 2 })]
    [InlineData(3UL, new ulong[] { 2, 1 })]
    [InlineData(7UL, new ulong[] { 4, 2, 1 })]
    [InlineData(15UL, new ulong[] { 8, 4, 2, 1 })]
    [InlineData(63UL, new ulong[] { 32, 16, 8, 4, 2, 1 })]
    [InlineData(64UL, new ulong[] { 64 })]
    [InlineData(100UL, new ulong[] { 64, 32, 4 })]
    [InlineData(1337UL, new ulong[] { 1024, 256, 32, 16, 8, 1 })]
    public void SplitToProofsAmounts_VariousAmounts_ReturnsCorrectSplit(ulong amount, ulong[] expected)
    {
        var result = Utils.SplitToProofsAmounts(amount, _testKeyset!);
        Assert.Equal(expected.ToList(), result);
    }

    [Fact]
    public void SplitToProofsAmounts_ZeroAmount_ReturnsEmptyList()
    {
        var result = Utils.SplitToProofsAmounts(0, _testKeyset!);
        Assert.Empty(result);
    }

    [Theory]
    [InlineData(0UL, 0)]
    [InlineData(1UL, 1)]
    [InlineData(2UL, 1)]
    [InlineData(3UL, 2)]
    [InlineData(4UL, 2)]
    [InlineData(7UL, 3)]
    [InlineData(8UL, 3)]
    [InlineData(15UL, 4)]
    [InlineData(16UL, 4)]
    [InlineData(100UL, 7)]
    [InlineData(1000UL, 10)]
    public void CalculateNumberOfBlankOutputs_VariousAmounts(ulong amount, int expected)
    {
        var result = Utils.CalculateNumberOfBlankOutputs(amount);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CreateOutputs_ValidAmounts_ReturnsCorrectOutputData()
    {
        var amounts = new List<ulong> { 1, 2, 4 };
        var outputs = Utils.CreateOutputs(amounts, _testKeysetId, _testKeyset!);
        
        Assert.Equal(3, outputs.Count);
        
        Assert.Equal(1UL, outputs[0].BlindedMessage.Amount);
        Assert.Equal(2UL, outputs[1].BlindedMessage.Amount);
        Assert.Equal(4UL, outputs[2].BlindedMessage.Amount);
        
        Assert.All(outputs, o => Assert.Equal(_testKeysetId, o.BlindedMessage.Id));
    }

    [Fact]
    public void CreateOutputs_InvalidAmount_ThrowsException()
    {
        var amounts = new List<ulong> { 1, 3 }; // 3 is not a valid amount
        Assert.Throws<ArgumentException>(() => Utils.CreateOutputs(amounts, _testKeysetId, _testKeyset!));
    }

    [Fact]
    public void CreateOutputs_DeterministicWithMnemonic()
    {
        var mnemonic = new Mnemonic("abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about");
        var amounts = new List<ulong> { 1, 2, 4 };
        
        var outputs1 = Utils.CreateOutputs(amounts, _testKeysetId, _testKeyset!, mnemonic, 0);
        var outputs2 = Utils.CreateOutputs(amounts, _testKeysetId, _testKeyset!, mnemonic, 0);
        
        // Same mnemonic and counter should produce same outputs
        for (int i = 0; i < outputs1.Count; i++)
        {
            Assert.Equal(
                ((StringSecret)outputs1[i].Secret).Secret, 
                ((StringSecret)outputs2[i].Secret).Secret
            );
        }
    }

    [Fact]
    public void CreateOutputs_RandomWithoutMnemonic()
    {
        var amounts = new List<ulong> { 1 };
        
        var outputs1 = Utils.CreateOutputs(amounts, _testKeysetId, _testKeyset!);
        var outputs2 = Utils.CreateOutputs(amounts, _testKeysetId, _testKeyset!);
        
        // without mnemonic, outputs should be random (different)
        Assert.NotEqual(
            ((StringSecret)outputs1[0].Secret).Secret,
            ((StringSecret)outputs2[0].Secret).Secret
        );
    }

    private static PubKey CreateTestPubKey(int seed)
    {
        var seedBytes = new byte[32];
        BitConverter.GetBytes(seed).CopyTo(seedBytes, 0);
        seedBytes[31] = 1; 
        var privKey = ECPrivKey.Create(seedBytes);
        ECPubKey ecPubKey = privKey.CreatePubKey();
        return ecPubKey; 
    }
    
    [Fact]
    public async Task ProofSelector_ExactMatch_SelectsCorrectProofs()
    {
        var keysetId = _testKeysetId;
        var fees = new Dictionary<KeysetId, ulong> { { keysetId, 0 } };
        var selector = new ProofSelector(fees);
        
        var proofs = new List<Proof>
        {
            new Proof { Amount = 1, Id = keysetId, C = CreateTestPubKey(1) },
            new Proof { Amount = 2, Id = keysetId, C = CreateTestPubKey(2) },
            new Proof { Amount = 4, Id = keysetId, C = CreateTestPubKey(3) },
            new Proof { Amount = 8, Id = keysetId, C = CreateTestPubKey(4) },
        };
        
        var result = await selector.SelectProofsToSend(proofs, 7, false);
        
        Assert.Equal(7UL, Utils.SumProofs(result.Send));
        Assert.Equal(8UL, Utils.SumProofs(result.Keep));
    }

    [Fact]
    public async Task ProofSelector_InsufficientFunds_ReturnsEmptySend()
    {
        var keysetId = _testKeysetId;
        var fees = new Dictionary<KeysetId, ulong> { { keysetId, 0 } };
        var selector = new ProofSelector(fees);
        
        var proofs = new List<Proof>
        {
            new Proof { Amount = 1, Id = keysetId, C = CreateTestPubKey(1) },
            new Proof { Amount = 2, Id = keysetId, C = CreateTestPubKey(2) },
        };
        
        var result = await selector.SelectProofsToSend(proofs, 100, false);
        
        Assert.Empty(result.Send);
        Assert.Equal(2, result.Keep.Count);
    }

    [Fact]
    public async Task ProofSelector_ZeroAmount_ReturnsEmptySend()
    {
        var keysetId = _testKeysetId;
        var fees = new Dictionary<KeysetId, ulong> { { keysetId, 0 } };
        var selector = new ProofSelector(fees);
        
        var proofs = new List<Proof>
        {
            new Proof { Amount = 8, Id = keysetId, C = CreateTestPubKey(1) },
        };
        
        var result = await selector.SelectProofsToSend(proofs, 0, false);
        
        Assert.Empty(result.Send);
    }

    [Fact]
    public async Task ProofSelector_WithFees_AccountsForFees()
    {
        var keysetId = _testKeysetId;
        var fees = new Dictionary<KeysetId, ulong> { { keysetId, 1000 } }; // 1 sat per proof
        var selector = new ProofSelector(fees);
        
        var proofs = new List<Proof>
        {
            new Proof { Amount = 1, Id = keysetId, C = CreateTestPubKey(1) },
            new Proof { Amount = 2, Id = keysetId, C = CreateTestPubKey(2) },
            new Proof { Amount = 4, Id = keysetId, C = CreateTestPubKey(3) },
            new Proof { Amount = 8, Id = keysetId, C = CreateTestPubKey(4) },
            new Proof { Amount = 16, Id = keysetId, C = CreateTestPubKey(5) },
        };
        
        var result = await selector.SelectProofsToSend(proofs, 10, true);
        
        Assert.True(Utils.SumProofs(result.Send) >= 10);
    }

    [Fact]
    public async Task ProofSelector_SingleLargeProof_SelectsIt()
    {
        var keysetId = _testKeysetId;
        var fees = new Dictionary<KeysetId, ulong> { { keysetId, 0 } };
        var selector = new ProofSelector(fees);
        
        var proofs = new List<Proof>
        {
            new Proof { Amount = 100, Id = keysetId, C = CreateTestPubKey(1) },
        };
        
        var result = await selector.SelectProofsToSend(proofs, 50, false);
        
        Assert.Single(result.Send);
        Assert.Equal(100UL, result.Send[0].Amount);
        Assert.Empty(result.Keep);
    }

    [Fact]
    public void TokenEncode_V4_RoundTrip()
    {
        var keysetId = new KeysetId("00ffd48b8f5ecf80");
        var proofs = new List<Proof>
        {
            new Proof 
            { 
                Amount = 1, 
                Id = keysetId,
                Secret = new StringSecret("acc12435e7b8484c3cf1850149218af90f716a52bf4a5ed347e48ecc13f77388"),
                C = "0244538319de485d55bed3b29a642bee5879375ab9e7a620e11e48ba482421f3cf".ToPubKey()
            }
        };
        
        var token = new CashuToken
        {
            Unit = "sat",
            Tokens = new List<CashuToken.Token>
            {
                new CashuToken.Token
                {
                    Mint = "http://localhost:3338",
                    Proofs = proofs
                }
            }
        };
        
        var encoded = token.Encode("B", false);
        Assert.StartsWith("cashuB", encoded);
        
        var decoded = CashuTokenHelper.Decode(encoded, out var version);
        Assert.Equal("B", version);
        Assert.Equal("sat", decoded.Unit);
        Assert.Single(decoded.Tokens);
        Assert.Equal("http://localhost:3338", decoded.Tokens[0].Mint);
        Assert.Single(decoded.Tokens[0].Proofs);
        Assert.Equal(1UL, decoded.Tokens[0].Proofs[0].Amount);
    }

    [Fact]
    public void TokenDecode_InvalidPrefix_ThrowsException()
    {
        var invalidToken = "invalidTokenString123";
        Assert.Throws<FormatException>(() => CashuTokenHelper.Decode(invalidToken, out _));
    }

    [Fact]
    public void KeysetId_Equality()
    {
        var id1 = new KeysetId("009a1f293253e41e");
        var id2 = new KeysetId("009a1f293253e41e");
        var id3 = new KeysetId("000f01df73ea149a");
        
        Assert.Equal(id1, id2);
        Assert.NotEqual(id1, id3);
        Assert.True(id1 == id2);
        Assert.False(id1 == id3);
    }

    [Fact]
    public void KeysetId_GetVersion()
    {
        var v0Id = new KeysetId("009a1f293253e41e");
        var v1Id = new KeysetId("01adc013fa9d85171586660abab27579888611659d357bc86bc09cb26eee8bc035");
        
        Assert.Equal(0x00, v0Id.GetVersion());
        Assert.Equal(0x01, v1Id.GetVersion());
    }

    [Fact]
    public void ComputeFee_NoFees_ReturnsZero()
    {
        var keysetId = _testKeysetId;
        var fees = new Dictionary<KeysetId, ulong> { { keysetId, 0 } };
        
        var proofs = new List<Proof>
        {
            new Proof { Amount = 1, Id = keysetId },
            new Proof { Amount = 2, Id = keysetId },
        };
        
        var fee = proofs.ComputeFee(fees);
        Assert.Equal(0UL, fee);
    }

    [Fact]
    public void ComputeFee_WithFees_ReturnsCorrectFee()
    {
        var keysetId = _testKeysetId;
        var fees = new Dictionary<KeysetId, ulong> { { keysetId, 1000 } }; // 1 sat per proof (1000 ppk)
        
        var proofs = new List<Proof>
        {
            new Proof { Amount = 1, Id = keysetId },
            new Proof { Amount = 2, Id = keysetId },
            new Proof { Amount = 4, Id = keysetId },
        };
        
        var fee = proofs.ComputeFee(fees);
        Assert.Equal(3UL, fee); // 3 proofs * 1 sat
    }

    [Fact]
    public void SendResponse_DefaultsToEmptyLists()
    {
        var response = new SendResponse();
        Assert.NotNull(response.Keep);
        Assert.NotNull(response.Send);
        Assert.Empty(response.Keep);
        Assert.Empty(response.Send);
    }

    [Fact]
    public void Wallet_WithKeysetSyncThreshold_SetsCorrectly()
    {
        var wallet = Wallet.Create()
            .WithMint(MintUrl)
            .WithKeysetSync(true, TimeSpan.FromMinutes(30));
        
        Assert.NotNull(wallet);
    }

    [Fact]
    public void Wallet_ShouldBumpCounter_Default()
    {
        var counter = new InMemoryCounter();
        var wallet = Wallet.Create()
            .WithMint(MintUrl)
            .WithCounter(counter);
        
        Assert.NotNull(wallet);
    }

    [Fact]
    public void Wallet_ShouldBumpCounter_Disabled()
    {
        var counter = new InMemoryCounter();
        var wallet = Wallet.Create()
            .WithMint(MintUrl)
            .WithCounter(counter)
            .ShouldBumpCounter(false);
        
        Assert.NotNull(wallet);
    }

    [Fact]
    public void MintInfo_FromGetInfoResponse()
    {
        var response = new GetInfoResponse
        {
            Version = "0.15.0",
            Name = "Test Mint",
            Description = "A test mint"
        };
        
        var info = new MintInfo(response);
        Assert.NotNull(info);
    }
    

    [Fact]
    public void P2PkBuilder_Build_CreatesValidSecret()
    {
        var privKey = new PrivKey("0000000000000000000000000000000000000000000000000000000000000001");
        var builder = new P2PkBuilder
        {
            Pubkeys = [privKey.Key.CreatePubKey()],
            SignatureThreshold = 1,
            SigFlag = "SIG_INPUTS"
        };
        
        var secret = builder.Build();
        Assert.NotNull(secret);
        
        var allowedPubkeys = secret.GetAllowedPubkeys(out var threshold);
        Assert.Single(allowedPubkeys);
        Assert.Equal(1, threshold);
    }

    [Fact]
    public void P2PkBuilder_WithMultisig_Build()
    {
        var privKey1 = new PrivKey("0000000000000000000000000000000000000000000000000000000000000001");
        var privKey2 = new PrivKey("0000000000000000000000000000000000000000000000000000000000000002");
        
        var builder = new P2PkBuilder
        {
            Pubkeys = [privKey1.Key.CreatePubKey(), privKey2.Key.CreatePubKey()],
            SignatureThreshold = 2,
            SigFlag = "SIG_INPUTS"
        };
        
        var secret = builder.Build();
        var allowedPubkeys = secret.GetAllowedPubkeys(out var threshold);
        
        Assert.Equal(2, allowedPubkeys.Count());
        Assert.Equal(2, threshold);
    }

    [Fact]
    public void HTLCBuilder_Build_CreatesValidSecret()
    {
        var preimage = "0000000000000000000000000000000000000000000000000000000000000001";
        using var sha = System.Security.Cryptography.SHA256.Create();
        var hashLockBytes = sha.ComputeHash(Convert.FromHexString(preimage));
        var hashLock = Convert.ToHexString(hashLockBytes).ToLower();
        var privKey = new PrivKey("0000000000000000000000000000000000000000000000000000000000000001");
        
        var builder = new HTLCBuilder
        {
            HashLock = hashLock,
            Pubkeys = [privKey.Key.CreatePubKey()],
            SignatureThreshold = 1
        };
        
        var secret = builder.Build();
        Assert.NotNull(secret);
        
        var allowedPubkeys = secret.GetAllowedPubkeys(out var threshold);
        Assert.Single(allowedPubkeys);
    }

    [Fact]
    public async Task Wallet_ThrowsOnMissingMint_ForAllOperations()
    {
        var wallet = Wallet.Create();
        
        await Assert.ThrowsAsync<ArgumentNullException>(() => wallet.GetInfo());
        Assert.Throws<ArgumentNullException>(() => wallet.CreateMintQuote());
        Assert.Throws<ArgumentNullException>(() => wallet.CreateMeltQuote());
        Assert.Throws<ArgumentNullException>(() => wallet.Swap());
        Assert.Throws<ArgumentNullException>(() => wallet.Restore());
    }

    [Fact]
    public async Task Counter_ReturnsZeroForUnknownKeysetId()
    {
        var counter = new InMemoryCounter();
        var unknownKeysetId = new KeysetId("00unknown1234567");
        
        var value = await counter.GetCounterForId(unknownKeysetId);
        Assert.Equal((uint)0, value);
    }

    [Fact]
    public async Task Counter_MultipleKeysets_IndependentCounters()
    {
        var counter = new InMemoryCounter();
        var keysetId1 = new KeysetId("00keyset11234567");
        var keysetId2 = new KeysetId("00keyset21234567");
        
        await counter.IncrementCounter(keysetId1, 10);
        await counter.IncrementCounter(keysetId2, 20);
        
        Assert.Equal((uint)10, await counter.GetCounterForId(keysetId1));
        Assert.Equal((uint)20, await counter.GetCounterForId(keysetId2));
    }
}


