using System.Text;
using System.Text.Json;
using DotNut.Abstractions;
using DotNut.BLS12_381.Curve.G1;
using DotNut.BLS12_381.Curve.G2;
using DotNut.Crypto;
using DotNut.NUT13;
using DotNut.NBitcoin.BIP39;

namespace DotNut.Tests.Unit;

public class BlsTests
{
    // Deterministic 32-byte mint private key: 0x01020304...1f20
    private static readonly byte[] MintPrivKey =
        Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();


    [Fact]
    public void HashToCurveG1_IsDeterministic()
    {
        var msg = "hello world"u8.ToArray();
        var p1 = BlsCashu.HashToCurveG1(msg);
        var p2 = BlsCashu.HashToCurveG1(msg);
        Assert.Equal(p1.ToCompressed(), p2.ToCompressed());
    }

    [Fact]
    public void HashToCurveG1_DifferentMessages_DifferentPoints()
    {
        var p1 = BlsCashu.HashToCurveG1("hello"u8.ToArray());
        var p2 = BlsCashu.HashToCurveG1("world"u8.ToArray());
        Assert.NotEqual(p1.ToCompressed(), p2.ToCompressed());
    }

    [Fact]
    public void HashToCurveG1_ResultIsNotAtInfinity()
    {
        var p = BlsCashu.HashToCurveG1("cashu bls test"u8.ToArray());
        Assert.False(p.IsInfinity);
        Assert.Equal(48, p.ToCompressed().Length);
    }

    [Fact]
    public void BlindMessage_RandomR_DifferentEachTime()
    {
        var secret = "random blind"u8.ToArray();
        var r1 = BlsCashu.GenerateRandomScalar();
        var b1 = BlsCashu.BlindMessage(secret, r1);
        var r2 = BlsCashu.GenerateRandomScalar();
        var b2 = BlsCashu.BlindMessage(secret, r2);
        // Collisions statistically impossible with 255-bit scalars
        Assert.NotEqual(r1, r2);
        Assert.NotEqual(b1.ToCompressed(), b2.ToCompressed());
    }

    [Fact]
    public void BlindThenUnblind_RecoverskOriginalPoint()
    {
        var secretBytes = "unblind round-trip"u8.ToArray();
        var Y = BlsCashu.HashToCurveG1(secretBytes);
        var r = BlsCashu.GenerateRandomScalar();
        // B_ = Y * r, then B_ * r^-1 must equal Y
        var B_ = BlsCashu.BlindMessage(secretBytes, r);
        var recovered = BlsCashu.UnblindSignature(B_, r);
        Assert.Equal(Y.ToCompressed(), recovered.ToCompressed());
    }

    [Fact]
    public void FullBdhke_RoundTrip_VerifiesCorrectly()
    {
        var secretBytes = "the quick brown fox jumps over the lazy dog"u8.ToArray();
        var r = BlsCashu.GenerateRandomScalar();
        var B_ = BlsCashu.BlindMessage(secretBytes, r);
        var C_ = BlsCashu.CreateBlindSignature(B_, MintPrivKey);
        var C = BlsCashu.UnblindSignature(C_, r);
        var K2 = BlsCashu.GetG2PubKeyFromPrivKey(MintPrivKey);

        Assert.True(BlsCashu.VerifySignature(K2, C, secretBytes));
    }

    [Fact]
    public void VerifySignature_WrongMintKey_ReturnsFalse()
    {
        var secretBytes = "test secret"u8.ToArray();
        var r = BlsCashu.GenerateRandomScalar();
        var B_ = BlsCashu.BlindMessage(secretBytes, r);
        var C_ = BlsCashu.CreateBlindSignature(B_, MintPrivKey);
        var C = BlsCashu.UnblindSignature(C_, r);

        var wrongKey = new byte[32];
        wrongKey[31] = 99;
        var wrongK2 = BlsCashu.GetG2PubKeyFromPrivKey(wrongKey);

        Assert.False(BlsCashu.VerifySignature(wrongK2, C, secretBytes));
    }

    [Fact]
    public void VerifySignature_WrongSecret_ReturnsFalse()
    {
        var secretBytes = "correct secret"u8.ToArray();
        var r = BlsCashu.GenerateRandomScalar();
        var B_ = BlsCashu.BlindMessage(secretBytes, r);
        var C_ = BlsCashu.CreateBlindSignature(B_, MintPrivKey);
        var C = BlsCashu.UnblindSignature(C_, r);
        var K2 = BlsCashu.GetG2PubKeyFromPrivKey(MintPrivKey);

        Assert.False(BlsCashu.VerifySignature(K2, C, "wrong secret"u8.ToArray()));
    }

    [Fact]
    public void BatchVerifySignatures_EmptyList_ReturnsTrue()
    {
        Assert.True(BlsCashu.BatchVerifySignatures([]));
    }

    [Fact]
    public void BatchVerifySignatures_SingleValidItem_ReturnsTrue()
    {
        var secretBytes = "batch single"u8.ToArray();
        var r = BlsCashu.GenerateRandomScalar();
        var B_ = BlsCashu.BlindMessage(secretBytes, r);
        var C_ = BlsCashu.CreateBlindSignature(B_, MintPrivKey);
        var C = BlsCashu.UnblindSignature(C_, r);
        var K2 = BlsCashu.GetG2PubKeyFromPrivKey(MintPrivKey);

        Assert.True(BlsCashu.BatchVerifySignatures([(K2, C, secretBytes)]));
    }

    [Fact]
    public void BatchVerifySignatures_MultipleValidItems_ReturnsTrue()
    {
        var K2 = BlsCashu.GetG2PubKeyFromPrivKey(MintPrivKey);
        var items = new List<(G2Affine, G1Affine, byte[])>();
        for (int i = 0; i < 5; i++)
        {
            var secretBytes = Encoding.UTF8.GetBytes($"batch secret {i}");
            var r = BlsCashu.GenerateRandomScalar();
        var B_ = BlsCashu.BlindMessage(secretBytes, r);
            var C_ = BlsCashu.CreateBlindSignature(B_, MintPrivKey);
            var C = BlsCashu.UnblindSignature(C_, r);
            items.Add((K2, C, secretBytes));
        }

        Assert.True(BlsCashu.BatchVerifySignatures(items));
    }

    [Fact]
    public void BatchVerifySignatures_OneForgedSignature_ReturnsFalse()
    {
        var K2 = BlsCashu.GetG2PubKeyFromPrivKey(MintPrivKey);
        var items = new List<(G2Affine, G1Affine, byte[])>();

        // Two valid proofs
        for (int i = 0; i < 2; i++)
        {
            var secretBytes = Encoding.UTF8.GetBytes($"valid {i}");
            var r = BlsCashu.GenerateRandomScalar();
        var B_ = BlsCashu.BlindMessage(secretBytes, r);
            var C_ = BlsCashu.CreateBlindSignature(B_, MintPrivKey);
            var C = BlsCashu.UnblindSignature(C_, r);
            items.Add((K2, C, secretBytes));
        }

        // One proof with mismatched secret (C was signed for different secret)
        var realSecret = "real"u8.ToArray();
        var fakeR = BlsCashu.GenerateRandomScalar();
        var fakeB_ = BlsCashu.BlindMessage(realSecret, fakeR);
        var fakeC_ = BlsCashu.CreateBlindSignature(fakeB_, MintPrivKey);
        var fakeC = BlsCashu.UnblindSignature(fakeC_, fakeR);
        items.Add((K2, fakeC, "different"u8.ToArray()));

        Assert.False(BlsCashu.BatchVerifySignatures(items));
    }

    [Theory]
    [InlineData(10)]
    public void GenerateRandomScalar_IsInFrRange(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            var r = BlsCashu.GenerateRandomScalar();
            Assert.Equal(32, r.Length);
            Assert.NotEqual(new byte[32], r);
            Assert.True(DotNut.BLS12_381.Scalar.TryFromBytesBigEndian(r, out _),
                "GenerateRandomScalar must return a canonical Fr scalar");
        }
    }

    [Fact]
    public void GetG2PubKeyFromPrivKey_IsDeterministic()
    {
        var k1 = BlsCashu.GetG2PubKeyFromPrivKey(MintPrivKey);
        var k2 = BlsCashu.GetG2PubKeyFromPrivKey(MintPrivKey);
        Assert.Equal(k1.ToCompressed(), k2.ToCompressed());
        Assert.Equal(96, k1.ToCompressed().Length); // 96 bytes = 192 hex chars
    }

    [Fact]
    public void PubKey_G1_FromHex_SetsIsBlsG1()
    {
        var Y = BlsCashu.HashToCurveG1("g1 test"u8.ToArray());
        var hex = Convert.ToHexString(Y.ToCompressed()).ToLower();
        Assert.Equal(96, hex.Length);

        var pubKey = new PubKey(hex);
        Assert.True(pubKey.IsBlsG1);
        Assert.False(pubKey.IsBlsG2);
        Assert.Null(pubKey.Key);
        Assert.Equal(hex, pubKey.ToString());
    }

    [Fact]
    public void PubKey_G2_FromHex_SetsIsBlsG2()
    {
        var K2 = BlsCashu.GetG2PubKeyFromPrivKey(MintPrivKey);
        var hex = Convert.ToHexString(K2.ToCompressed()).ToLower();
        Assert.Equal(192, hex.Length);

        var pubKey = new PubKey(hex);
        Assert.True(pubKey.IsBlsG2);
        Assert.False(pubKey.IsBlsG1);
        Assert.Null(pubKey.Key);
        Assert.Equal(hex, pubKey.ToString());
    }

    [Fact]
    public void PubKey_G1_GetBlsG1Point_RoundTrip()
    {
        var Y = BlsCashu.HashToCurveG1("g1 round-trip"u8.ToArray());
        var pubKey = new PubKey(Convert.ToHexString(Y.ToCompressed()).ToLower());
        var recovered = pubKey.GetBlsG1Point();
        Assert.Equal(Y.ToCompressed(), recovered.ToCompressed());
    }

    [Fact]
    public void PubKey_G2_GetBlsG2Point_RoundTrip()
    {
        var K2 = BlsCashu.GetG2PubKeyFromPrivKey(MintPrivKey);
        var pubKey = new PubKey(Convert.ToHexString(K2.ToCompressed()).ToLower());
        var recovered = pubKey.GetBlsG2Point();
        Assert.Equal(K2.ToCompressed(), recovered.ToCompressed());
    }

    [Fact]
    public void PubKey_Secp_GetBlsG1Point_Throws()
    {
        var secpKey = (PubKey)"0279be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798".ToPubKey();
        Assert.Throws<InvalidOperationException>(() => secpKey.GetBlsG1Point());
    }

    [Fact]
    public void PubKey_G1_GetBlsG2Point_Throws()
    {
        var g1Key = new PubKey(
            Convert.ToHexString(BlsCashu.HashToCurveG1("x"u8.ToArray()).ToCompressed()).ToLower());
        Assert.Throws<InvalidOperationException>(() => g1Key.GetBlsG2Point());
    }

    [Fact]
    public void PubKey_G1_EqualityAndHashCode()
    {
        var hex = Convert.ToHexString(BlsCashu.HashToCurveG1("eq"u8.ToArray()).ToCompressed()).ToLower();
        var p1 = new PubKey(hex);
        var p2 = new PubKey(hex);
        Assert.Equal(p1, p2);
        Assert.Equal(p1.GetHashCode(), p2.GetHashCode());
    }

    [Fact]
    public void PubKey_G2_NotEqualToG1_SameBytes()
    {
        // G1 (48 bytes) and G2 (96 bytes) have different hex lengths so they are different types
        var g1Key = new PubKey(Convert.ToHexString(BlsCashu.HashToCurveG1("x"u8.ToArray()).ToCompressed()).ToLower());
        var K2 = BlsCashu.GetG2PubKeyFromPrivKey(MintPrivKey);
        var g2Key = new PubKey(Convert.ToHexString(K2.ToCompressed()).ToLower());
        Assert.NotEqual(g1Key, g2Key);
    }

    // ─────────────────────────── KeysetId ───────────────────────────

    [Theory]
    [InlineData("009a1f293253e41e")]
    [InlineData("015ba18a8adcd02e715a58358eb618da4a4b3791151a4bee5e968bb88406ccf76a")]
    public void KeysetId_IsBlsKeyset_FalseForNonBls(string id)
    {
        Assert.False(new KeysetId(id).IsBlsKeyset());
    }

    [Fact]
    public void KeysetId_IsBlsKeyset_TrueFor02Prefix()
    {
        var id = new KeysetId("02" + new string('a', 64));
        Assert.True(id.IsBlsKeyset());
        Assert.Equal(0x02, id.GetVersion());
    }

    [Fact]
    public void BlsKeyset_InheritsFromKeyset()
    {
        Assert.IsAssignableFrom<Keyset>(new BlsKeyset());
    }

    [Fact]
    public void BlsKeyset_Amounts_AccessibleViaBaseKeyset()
    {
        var keyset = MakeBlsKeyset();
        // SplitToProofsAmounts uses base Keyset.Keys
        var amounts = Utils.SplitToProofsAmounts(7, keyset);
        Assert.Equal(new List<ulong> { 4, 2, 1 }, amounts);
    }

    [Fact]
    public void BlsKeyset_GetKeysetId_IsDeterministic()
    {
        var keyset = MakeBlsKeyset();
        var id1 = keyset.GetKeysetId("sat");
        var id2 = keyset.GetKeysetId("sat");
        Assert.Equal(id1.ToString(), id2.ToString());
    }

    [Fact]
    public void BlsKeyset_GetKeysetId_StartsWithV3Prefix()
    {
        var keyset = MakeBlsKeyset();
        var id = keyset.GetKeysetId("sat");
        Assert.StartsWith("02", id.ToString());
        Assert.True(id.IsBlsKeyset());
    }

    [Fact]
    public void BlsKeyset_GetKeysetId_DifferentUnits_DifferentIds()
    {
        var keyset = MakeBlsKeyset();
        var sat = keyset.GetKeysetId("sat");
        var msat = keyset.GetKeysetId("msat");
        Assert.NotEqual(sat.ToString(), msat.ToString());
    }

    [Fact]
    public void BlsKeyset_GetKeysetId_WithFee_DifferentFromWithout()
    {
        var keyset = MakeBlsKeyset();
        var noFee = keyset.GetKeysetId("sat");
        var withFee = keyset.GetKeysetId("sat", inputFeePpk: 100);
        Assert.NotEqual(noFee.ToString(), withFee.ToString());
    }

    [Fact]
    public void BlsKeyset_VerifyKeysetId_AcceptsCorrectId()
    {
        var keyset = MakeBlsKeyset();
        var id = keyset.GetKeysetId("sat");
        Assert.True(keyset.VerifyKeysetId(id, "sat"));
    }

    [Fact]
    public void BlsKeyset_VerifyKeysetId_RejectsWrongUnit()
    {
        var keyset = MakeBlsKeyset();
        var id = keyset.GetKeysetId("sat");
        Assert.False(keyset.VerifyKeysetId(id, "msat"));
    }

    [Fact]
    public void BlsKeyset_EmptyKeyset_GetKeysetId_Throws()
    {
        var keyset = new BlsKeyset();
        Assert.Throws<InvalidOperationException>(() => keyset.GetKeysetId("sat"));
    }

    [Fact]
    public void BlsKeyset_NullUnit_GetKeysetId_Throws()
    {
        var keyset = MakeBlsKeyset();
        Assert.Throws<ArgumentNullException>(() => keyset.GetKeysetId(null));
    }

    [Fact]
    public void BlsKeyset_JsonRoundTrip()
    {
        var keyset = MakeBlsKeyset();
        var json = JsonSerializer.Serialize(keyset);
        var parsed = JsonSerializer.Deserialize<BlsKeyset>(json)!;

        Assert.Equal(keyset.Count, parsed.Count);
        foreach (var (amount, key) in keyset)
        {
            Assert.True(parsed.ContainsKey(amount));
            Assert.Equal(key.ToString(), parsed[amount].ToString());
            Assert.True(parsed[amount].IsBlsG2);
        }
    }

    [Fact]
    public void AnyKeysetJsonConverter_SecpKeyset_DeserializesAsKeyset()
    {
        // Test via GetKeysResponse, which is where AnyKeysetJsonConverter is used as a property converter
        const string keysetJson = "{\"1\":\"03a40f20667ed53513075dc51e715ff2046cad64eb68960632269ba7f0210e38bc\"}";
        var responseJson = $$"""{"keysets":[{"id":"009a1f293253e41e","unit":"sat","active":true,"keys":{{keysetJson}}}]}""";
        var response = JsonSerializer.Deserialize<DotNut.ApiModels.GetKeysResponse>(responseJson)!;

        Assert.IsNotType<BlsKeyset>(response.Keysets[0].Keys);
        Assert.NotNull(response.Keysets[0].Keys[1].Key);
    }

    [Fact]
    public void AnyKeysetJsonConverter_BlsKeyset_DeserializesAsBlsKeyset()
    {
        var keyset = MakeBlsKeyset();
        var keysetJson = JsonSerializer.Serialize(keyset);
        var keysetId = keyset.GetKeysetId("sat");
        var responseJson = $$"""{"keysets":[{"id":"{{keysetId}}","unit":"sat","active":true,"keys":{{keysetJson}}}]}""";
        var response = JsonSerializer.Deserialize<DotNut.ApiModels.GetKeysResponse>(responseJson)!;

        Assert.IsType<BlsKeyset>(response.Keysets[0].Keys);
        Assert.Equal(keyset.Count, response.Keysets[0].Keys.Count);
        foreach (var (amount, _) in keyset)
            Assert.True(response.Keysets[0].Keys[amount].IsBlsG2);
    }

    [Fact]
    public void BlsKeyset_InvalidHexLength_ThrowsOnDeserialize()
    {
        // 66-char hex (secp) is invalid for a BLS keyset
        var badJson = "{\"1\":\"03a40f20667ed53513075dc51e715ff2046cad64eb68960632269ba7f0210e38bc\"}";
        Assert.ThrowsAny<Exception>(() => JsonSerializer.Deserialize<BlsKeyset>(badJson));
    }

    private static readonly Mnemonic TestMnemonic = new(
        "half depart obvious quality work element tank gorilla view sugar picture humble");
    private static readonly KeysetId BlsKeysetId = new("02" + new string('b', 64));

    [Fact]
    public void Nut13_BLS_BlindingFactor_IsInFrRange()
    {
        for (uint i = 0; i < 5; i++)
        {
            var rBytes = TestMnemonic.DeriveBlindingFactor(BlsKeysetId, i);
            Assert.Equal(32, rBytes.Length);
            Assert.NotEqual(new byte[32], rBytes);
            Assert.True(DotNut.BLS12_381.Scalar.TryFromBytesBigEndian(rBytes, out _),
                $"counter {i}: blinding factor must be a canonical Fr scalar");
        }
    }

    [Fact]
    public void Nut13_BLS_BlindingFactor_IsDeterministic()
    {
        var r0a = TestMnemonic.DeriveBlindingFactor(BlsKeysetId, 0);
        var r0b = TestMnemonic.DeriveBlindingFactor(BlsKeysetId, 0);
        Assert.Equal(r0a, r0b);
    }

    [Fact]
    public void Nut13_BLS_BlindingFactor_DifferentCounters_DifferentResults()
    {
        var r0 = TestMnemonic.DeriveBlindingFactor(BlsKeysetId, 0);
        var r1 = TestMnemonic.DeriveBlindingFactor(BlsKeysetId, 1);
        Assert.NotEqual(r0, r1);
    }

    [Fact]
    public void Nut13_BLS_Secret_IsDeterministic()
    {
        var s0a = TestMnemonic.DeriveSecret(BlsKeysetId, 0).Secret;
        var s0b = TestMnemonic.DeriveSecret(BlsKeysetId, 0).Secret;
        Assert.Equal(s0a, s0b);
    }

    [Fact]
    public void Nut13_BLS_Secret_DifferentCounters_DifferentResults()
    {
        var s0 = TestMnemonic.DeriveSecret(BlsKeysetId, 0).Secret;
        var s1 = TestMnemonic.DeriveSecret(BlsKeysetId, 1).Secret;
        Assert.NotEqual(s0, s1);
    }

    [Fact]
    public void Nut13_BLS_DeriveOutputs_BlindedMessagesAreG1Points()
    {
        var outputs = TestMnemonic.DeriveOutputs(new ulong[] { 1, 2, 4 }, BlsKeysetId, 0);
        Assert.Equal(3, outputs.Count);
        foreach (var o in outputs)
        {
            Assert.True(o.BlindedMessage.B_.IsBlsG1,
                "BLS output's B_ must be a G1 point");
        }
    }

    [Fact]
    public void ConstructBlsProofFromPromise_ProducesVerifiableProof()
    {
        var secretStr = "cashu bls proof secret";
        var secretBytes = Encoding.UTF8.GetBytes(secretStr);

        var r = BlsCashu.GenerateRandomScalar();
        var B_ = BlsCashu.BlindMessage(secretBytes, r);
        var C_ = BlsCashu.CreateBlindSignature(B_, MintPrivKey);
        var K2 = BlsCashu.GetG2PubKeyFromPrivKey(MintPrivKey);
        var amountKey = new BlsG2PubKey(K2);

        var rPriv = new PrivKey(r);
        var blindSig = new BlindSignature
        {
            Amount = 1,
            Id = new KeysetId("02" + new string('0', 64)),
            C_ = new PubKey(Convert.ToHexString(C_.ToCompressed()).ToLower()),
        };

        var proof = Utils.ConstructBlsProofFromPromise(blindSig, rPriv, new StringSecret(secretStr), amountKey);

        Assert.Equal(1UL, proof.Amount);
        Assert.True(proof.C.IsBlsG1);
        Assert.True(BlsCashu.VerifySignature(K2, proof.C.GetBlsG1Point(), secretBytes));
    }

    [Fact]
    public void ConstructBlsProofFromPromise_WrongKey_Throws()
    {
        var secretBytes = "real secret"u8.ToArray();
        var r = BlsCashu.GenerateRandomScalar();
        var B_ = BlsCashu.BlindMessage(secretBytes, r);

        var wrongKey = new byte[32];
        wrongKey[31] = 99;
        var C_ = BlsCashu.CreateBlindSignature(B_, wrongKey);        // signed by wrongKey
        var K2 = BlsCashu.GetG2PubKeyFromPrivKey(MintPrivKey);       // verified against MintPrivKey

        var blindSig = new BlindSignature
        {
            Amount = 1,
            Id = new KeysetId("02" + new string('0', 64)),
            C_ = new PubKey(Convert.ToHexString(C_.ToCompressed()).ToLower()),
        };

        Assert.Throws<InvalidOperationException>(() =>
            Utils.ConstructBlsProofFromPromise(
                blindSig,
                new PrivKey(r),
                new StringSecret("real secret"),
                new BlsG2PubKey(K2)));
    }

    [Fact]
    public void ConstructProofsFromPromises_BlsKeyset_FullRoundTrip()
    {
        var blsKeyset = MakeBlsKeyset();
        var keysetId = blsKeyset.GetKeysetId("sat");

        var outputs = Utils.CreateOutputs(new ulong[] { 1, 2 }, keysetId, blsKeyset);
        Assert.Equal(2, outputs.Count);

        // Simulate mint: sign each blinded message with the per-amount key
        var blindSigs = outputs.Select(o =>
        {
            var B_ = o.BlindedMessage.B_.GetBlsG1Point();
            var mintKey = GetMintKeyForAmount(o.BlindedMessage.Amount);
            var C_ = BlsCashu.CreateBlindSignature(B_, mintKey);
            return new BlindSignature
            {
                Amount = o.BlindedMessage.Amount,
                Id = keysetId,
                C_ = new PubKey(Convert.ToHexString(C_.ToCompressed()).ToLower()),
            };
        }).ToList();

        var proofs = Utils.ConstructProofsFromPromises(blindSigs, outputs, blsKeyset);

        Assert.Equal(2, proofs.Count);
        foreach (var proof in proofs)
        {
            Assert.True(proof.C.IsBlsG1);
            var K2 = blsKeyset[proof.Amount].GetBlsG2Point();
            Assert.True(BlsCashu.VerifySignature(K2, proof.C.GetBlsG1Point(), proof.Secret.GetBytes()));
        }
    }

    private static BlsKeyset MakeBlsKeyset()
    {
        var keyset = new BlsKeyset();
        foreach (var amount in new ulong[] { 1, 2, 4, 8 })
        {
            var K2 = BlsCashu.GetG2PubKeyFromPrivKey(GetMintKeyForAmount(amount));
            keyset[amount] = new PubKey(Convert.ToHexString(K2.ToCompressed()).ToLower());
        }
        return keyset;
    }

    private static byte[] GetMintKeyForAmount(ulong amount)
    {
        var key = new byte[32];
        key[0] = 1; // ensure non-zero
        key[30] = (byte)((amount >> 8) & 0xFF);
        key[31] = (byte)(amount & 0xFF);
        return key;
    }

}
