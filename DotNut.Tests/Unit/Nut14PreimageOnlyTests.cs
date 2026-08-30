using SHA256 = System.Security.Cryptography.SHA256;

namespace DotNut.Tests.Unit;

public class Nut14PreimageOnlyTests
{
    private const string Preimage =
        "0000000000000000000000000000000000000000000000000000000000000000";

    private static HTLCProofSecret PureHashlock()
    {
        var hashLock = Convert
            .ToHexString(SHA256.HashData(Convert.FromHexString(Preimage)))
            .ToLowerInvariant();

        return new HTLCBuilder { HashLock = hashLock }.Build();
    }

    [Fact]
    public void HashlockWithoutPubkeysNeedsNoSignature()
    {
        var secret = PureHashlock();

        // No pubkeys tag at all, so nothing to sign with.
        Assert.Empty(secret.Builder.Pubkeys);
        Assert.DoesNotContain(secret.Tags ?? [], t => t.FirstOrDefault() == "pubkeys");

        secret.GetAllowedPubkeys(out var requiredSignatures);
        Assert.Equal(0, requiredSignatures);
    }

    [Fact]
    public void PreimageAloneSpendsTheProof()
    {
        var secret = PureHashlock();
        var message = "message"u8.ToArray();

        var witness = secret.GenerateWitness(message, [], Convert.FromHexString(Preimage));

        Assert.NotNull(witness);
        Assert.Empty(witness.Signatures);
        Assert.True(secret.VerifyWitness(message, witness));
    }

    [Fact]
    public void WrongPreimageDoesNotSpendIt()
    {
        var secret = PureHashlock();
        var message = "message"u8.ToArray();

        var witness = new HTLCWitness
        {
            Signatures = [],
            Preimage = new string('1', 64),
        };

        Assert.False(secret.VerifyWitness(message, witness));
    }
}
