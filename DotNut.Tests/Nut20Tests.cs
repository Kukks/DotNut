using System.Text.Json;
using DotNut.ApiModels;
using SHA256 = System.Security.Cryptography.SHA256;

namespace DotNut.Tests;

public class Nut20Tests
{
    // Test vector from tests/20-test.md. The quote pubkey belongs to secret key 0x01.
    private const string SignedMintQuote = """
        {
          "quote": "0192d3c0-7e8a-7c3d-8e9f-1a2b3c4d5e6f",
          "outputs": [
            {
              "amount": 1,
              "id": "009a1f293253e41e",
              "B_": "036d6caac248af96f6afa7f904f550253a0f3ef3f5aa2fe6838a95b216691468e2"
            },
            {
              "amount": 1,
              "id": "009a1f293253e41e",
              "B_": "021f8a566c205633d029094747d2e18f44e05993dda7a5f88f496078205f656e59"
            }
          ],
          "signature": "4881093a332ff7c79f3e598ce5b249d64978b47165a0b19c18adf0ced0246228e61e702f0abaf1bf27b92be4336bdbabacfbe4c914076386b3c66fdcd0b3480e"
        }
        """;

    private const string QuotePubkey =
        "0279be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798";

    [Fact]
    public void MessageToSignMatchesTestVector()
    {
        var parsed = JsonSerializer.Deserialize<PostMintRequest>(SignedMintQuote);
        Assert.NotNull(parsed);

        var msg = MintQuoteSigner.GetMessageToSign(parsed.Quote, parsed.Outputs);

        // "Cashu_MintQuoteSig_v1" || len32(quote) || quote || (len32(amount) || amount
        // || len32(B_) || B_) per output.
        Assert.Equal(
            "43617368755f4d696e7451756f74655369675f7631"
                + "00000024"
                + "30313932643363302d376538612d376333642d386539662d316132623363346435653666"
                + "00000001"
                + "01"
                + "00000021"
                + "036d6caac248af96f6afa7f904f550253a0f3ef3f5aa2fe6838a95b216691468e2"
                + "00000001"
                + "01"
                + "00000021"
                + "021f8a566c205633d029094747d2e18f44e05993dda7a5f88f496078205f656e59",
            Convert.ToHexString(msg).ToLowerInvariant()
        );

        Assert.Equal(
            "c164fd384879f74ab6ea2e7cf13d90ed42e6df9d5de607eeb5c9cc7d36fb1c21",
            Convert.ToHexString(SHA256.HashData(msg)).ToLowerInvariant()
        );
    }

    [Fact]
    public void ValidSignatureOnMintQuote()
    {
        var parsed = JsonSerializer.Deserialize<PostMintRequest>(SignedMintQuote);
        Assert.NotNull(parsed);

        Assert.True(parsed.VerifySignature(new PubKey(QuotePubkey)));
    }

    [Fact]
    public void InvalidSignatureOnMintQuote()
    {
        var parsed = JsonSerializer.Deserialize<PostMintRequest>(SignedMintQuote);
        Assert.NotNull(parsed);

        // Same request, signature off by its last byte.
        parsed.Signature = parsed.Signature![..^2] + "0f";

        Assert.False(parsed.VerifySignature(new PubKey(QuotePubkey)));
    }

    [Fact]
    public void SignatureCoversTheOutputs()
    {
        var parsed = JsonSerializer.Deserialize<PostMintRequest>(SignedMintQuote);
        Assert.NotNull(parsed);

        // Swapping two outputs keeps the same set but changes the order they are committed in.
        (parsed.Outputs[0], parsed.Outputs[1]) = (parsed.Outputs[1], parsed.Outputs[0]);

        Assert.False(parsed.VerifySignature(new PubKey(QuotePubkey)));
    }

    [Fact]
    public void SignAndVerifyRoundTrip()
    {
        var privkey = new PrivKey(
            "0000000000000000000000000000000000000000000000000000000000000001"
        );
        var parsed = JsonSerializer.Deserialize<PostMintRequest>(SignedMintQuote);
        Assert.NotNull(parsed);

        parsed.Signature = privkey.SignMintQuote(parsed.Quote, parsed.Outputs.ToList());

        Assert.True(parsed.VerifySignature(new PubKey(QuotePubkey)));
    }

    [Fact]
    public void LegacySignatureIsAcceptedUnlessRefused()
    {
        var privkey = new PrivKey(
            "0000000000000000000000000000000000000000000000000000000000000001"
        );
        var parsed = JsonSerializer.Deserialize<PostMintRequest>(SignedMintQuote);
        Assert.NotNull(parsed);

        parsed.Signature = privkey.SignMintQuoteLegacy(parsed.Quote, parsed.Outputs.ToList());

        // Mints that understand the current message still accept the superseded one, the way
        // cdk and nutshell do, but a caller can insist on the current format.
        Assert.True(parsed.VerifySignature(new PubKey(QuotePubkey)));
        Assert.False(parsed.VerifySignature(new PubKey(QuotePubkey), allowLegacy: false));
    }

    [Fact]
    public void LegacyMessageIsTheSupersededFormat()
    {
        var parsed = JsonSerializer.Deserialize<PostMintRequest>(SignedMintQuote);
        Assert.NotNull(parsed);

        var msg = MintQuoteSigner.GetLegacyMessageToSign(parsed.Quote, parsed.Outputs);

        // Quote id and the hex of every output, concatenated as UTF-8. No domain tag, no lengths.
        Assert.Equal(
            "0192d3c0-7e8a-7c3d-8e9f-1a2b3c4d5e6f"
                + "036d6caac248af96f6afa7f904f550253a0f3ef3f5aa2fe6838a95b216691468e2"
                + "021f8a566c205633d029094747d2e18f44e05993dda7a5f88f496078205f656e59",
            System.Text.Encoding.UTF8.GetString(msg)
        );
    }

    [Theory]
    // Canonical minimal big-endian: zero is empty, no leading zero bytes.
    [InlineData(0UL, "")]
    [InlineData(1UL, "01")]
    [InlineData(255UL, "ff")]
    [InlineData(256UL, "0100")]
    [InlineData(ulong.MaxValue, "ffffffffffffffff")]
    public void AmountsUseMinimalBigEndian(ulong amount, string expected)
    {
        Assert.Equal(
            expected,
            Convert.ToHexString(MintQuoteSigner.ToMinimalBigEndian(amount)).ToLowerInvariant()
        );
    }
}
