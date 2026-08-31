using NBitcoin.Secp256k1;

namespace DotNut.Tests.Unit;

public class Nut11ValidationTests
{
    private const string KeyA =
        "02698c4e2b5f9534cd0687d87513c759790cf829aa5739184a3e3735471fbda904";
    private const string KeyB =
        "0379c3e1a2fbd7b1d0f9f0d4a4e5b64e0b4c8fa7d2c0f96a7c5f2b0a9e8d7c6b5a";

    private static P2PKProofSecret Secret(string data, params string[][] tags) =>
        new()
        {
            Data = data,
            Nonce = "0000000000000000000000000000000000000000000000000000000000000000",
            Tags = tags,
        };

    [Theory]
    // Each tag may appear exactly once.
    [InlineData("pubkeys")]
    [InlineData("locktime")]
    [InlineData("refund")]
    [InlineData("n_sigs")]
    [InlineData("n_sigs_refund")]
    [InlineData("sigflag")]
    public void RepeatedTagIsMalformed(string tag)
    {
        var value = tag switch
        {
            "locktime" => "1700000000",
            "n_sigs" or "n_sigs_refund" => "1",
            "sigflag" => "SIG_INPUTS",
            _ => KeyB,
        };

        var secret = Secret(KeyA, [tag, value], [tag, value]);

        Assert.Contains("appears more than once", Assert.Throws<FormatException>(
            () => P2PkBuilder.Load(secret)
        ).Message);
    }

    [Fact]
    public void UnknownSigflagIsMalformed()
    {
        var secret = Secret(KeyA, ["sigflag", "SIG_EVERYTHING"]);

        Assert.Contains("Unknown sigflag", Assert.Throws<FormatException>(
            () => P2PkBuilder.Load(secret)
        ).Message);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("many")]
    public void NonPositiveThresholdIsMalformed(string nSigs)
    {
        var secret = Secret(KeyA, ["pubkeys", KeyB], ["n_sigs", nSigs]);

        Assert.Contains("positive integer", Assert.Throws<FormatException>(
            () => P2PkBuilder.Load(secret)
        ).Message);
    }

    [Fact]
    public void ThresholdAboveKeyCountIsMalformed()
    {
        // Two keys in the main pathway, three signatures demanded.
        var secret = Secret(KeyA, ["pubkeys", KeyB], ["n_sigs", "3"]);

        Assert.Contains("pathway has 2 key", Assert.Throws<FormatException>(
            () => P2PkBuilder.Load(secret)
        ).Message);
    }

    [Fact]
    public void DuplicateKeyInOnePathwayIsMalformed()
    {
        // Same x-coordinate, different parity prefix — still the same key.
        var secret = Secret(KeyA, ["pubkeys", "03" + KeyA[2..]]);

        Assert.Contains("Duplicate pubkey in the main pathway", Assert.Throws<FormatException>(
            () => P2PkBuilder.Load(secret)
        ).Message);
    }

    [Fact]
    public void DuplicateKeyIsCaughtRegardlessOfCase()
    {
        var secret = Secret(KeyA, ["pubkeys", KeyA.ToUpperInvariant()]);

        Assert.Throws<FormatException>(() => P2PkBuilder.Load(secret));
    }

    [Fact]
    public void SameKeyInBothPathwaysIsAllowed()
    {
        var secret = Secret(
            KeyA,
            ["locktime", "1700000000"],
            ["refund", "03" + KeyA[2..]]
        );

        var builder = P2PkBuilder.Load(secret);
        Assert.Single(builder.Pubkeys);
        Assert.Single(builder.RefundPubkeys!);
    }

    [Fact]
    public void MalformedSecretFailsVerificationRatherThanThrowing()
    {
        var secret = Secret(KeyA, ["sigflag", "SIG_EVERYTHING"]);
        var proof = new Proof
        {
            Amount = 1,
            Id = new KeysetId("009a1f293253e41e"),
            Secret = new StringSecret("x"),
            C = new PubKey(KeyA),
            Witness = """{"signatures":["00"]}""",
        };

        Assert.False(secret.VerifyWitness(proof));
    }

    [Fact]
    public void SignsAProofLockedToTheOtherParityOfOurKey()
    {
        var privkey = ECPrivKey.Create(
            Convert.FromHexString(
                "0000000000000000000000000000000000000000000000000000000000000001"
            )
        );
        var ours = privkey.CreatePubKey().ToHex();
        // The lock names the same x-coordinate with the opposite parity prefix. A Schnorr
        // signature from our secret verifies against it, so we must be willing to sign.
        var flipped = (ours.StartsWith("02") ? "03" : "02") + ours[2..];

        var secret = Secret(flipped);
        var witness = secret.GenerateWitness("message"u8.ToArray(), [privkey]);

        Assert.NotNull(witness);
        Assert.Single(witness.Signatures);
        Assert.True(secret.VerifyWitness("message"u8.ToArray(), witness));
    }
}
