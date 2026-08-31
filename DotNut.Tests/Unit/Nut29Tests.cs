using System.Text.Json;
using DotNut.ApiModels;
using NBitcoin.Secp256k1;
using SHA256 = System.Security.Cryptography.SHA256;

namespace DotNut.Tests.Unit;

public class Nut29Tests
{
    // Test vector from tests/29-tests.md, "Batch mint with valid signature". sk = 1.
    private const string BatchRequest = """
        {
          "quotes": ["019e6d5a-2347-7000-8c81-a1e0dbf3299f"],
          "outputs": [
            {
              "amount": 1,
              "id": "010000000000000000000000000000000000000000000000000000000000000000",
              "B_": "036d6caac248af96f6afa7f904f550253a0f3ef3f5aa2fe6838a95b216691468e2"
            },
            {
              "amount": 1,
              "id": "010000000000000000000000000000000000000000000000000000000000000000",
              "B_": "021f8a566c205633d029094747d2e18f44e05993dda7a5f88f496078205f656e59"
            }
          ]
        }
        """;

    private const string QuoteId = "019e6d5a-2347-7000-8c81-a1e0dbf3299f";

    private const string QuotePubkey =
        "0279be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798";

    private static readonly PrivKey QuotePrivkey = new(
        "0000000000000000000000000000000000000000000000000000000000000001"
    );

    [Fact]
    public void BatchMessageIsTheNut20MessageOverEveryOutput()
    {
        var request = JsonSerializer.Deserialize<PostBatchedMintRequest>(BatchRequest);
        Assert.NotNull(request);

        var msg = MintQuoteSigner.GetMessageToSign(QuoteId, request.Outputs);

        Assert.Equal(
            "43617368755f4d696e7451756f74655369675f7631"
                + "00000024"
                + "30313965366435612d323334372d373030302d386338312d613165306462663332393966"
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
            "dad25acc587637206d73398894d337f983a0ca644746e8673727eaa0b29fa9b4",
            Convert.ToHexString(SHA256.HashData(msg)).ToLowerInvariant()
        );
    }

    [Fact]
    public void VectorSignatureVerifies()
    {
        var request = JsonSerializer.Deserialize<PostBatchedMintRequest>(BatchRequest);
        Assert.NotNull(request);

        const string vectorSignature =
            "0c39431338a0202568b9a1d4215c99f179cbb8ee5472ac5ae7133fbb8f99cafb"
            + "b9e425ad33c60224c96b8f9f984f004379a18e9558468d129b6b03f0da6de162";

        Assert.True(VerifyBatchSignature(request, QuoteId, vectorSignature, QuotePubkey));
    }

    [Fact]
    public void SignsOnlyTheLockedQuotes()
    {
        var request = JsonSerializer.Deserialize<PostBatchedMintRequest>(BatchRequest);
        Assert.NotNull(request);
        request.QuoteIds = [QuoteId, "unlocked-quote"];

        request.Sign(new Dictionary<string, PrivKey> { [QuoteId] = QuotePrivkey });

        Assert.NotNull(request.Signatures);
        // One entry per quote, null where the quote carries no pubkey.
        Assert.Equal(2, request.Signatures.Length);
        Assert.Null(request.Signatures[1]);
        Assert.True(
            VerifyBatchSignature(request, QuoteId, request.Signatures[0]!, QuotePubkey)
        );
    }

    [Fact]
    public void LeavesSignaturesUnsetWhenNoQuoteIsLocked()
    {
        var request = JsonSerializer.Deserialize<PostBatchedMintRequest>(BatchRequest);
        Assert.NotNull(request);

        request.Sign(new Dictionary<string, PrivKey>());

        Assert.Null(request.Signatures);
    }

    [Fact]
    public void SignatureCoversEveryOutputInTheBatch()
    {
        var request = JsonSerializer.Deserialize<PostBatchedMintRequest>(BatchRequest);
        Assert.NotNull(request);

        request.Sign(new Dictionary<string, PrivKey> { [QuoteId] = QuotePrivkey });
        var signature = request.Signatures![0]!;

        // Dropping an output from the consolidated set invalidates every quote's signature.
        request.Outputs = [request.Outputs[0]];

        Assert.False(VerifyBatchSignature(request, QuoteId, signature, QuotePubkey));
    }

    private static bool VerifyBatchSignature(
        PostBatchedMintRequest request,
        string quoteId,
        string signature,
        string pubkey
    )
    {
        var hash = SHA256.HashData(MintQuoteSigner.GetMessageToSign(quoteId, request.Outputs));
        if (!SecpSchnorrSignature.TryCreate(Convert.FromHexString(signature), out var sig))
        {
            return false;
        }
        return new PubKey(pubkey).Key.ToXOnlyPubKey().SigVerifyBIP340(sig, hash);
    }
}
