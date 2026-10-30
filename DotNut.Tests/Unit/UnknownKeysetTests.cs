using System.Text.Json;
using DotNut.ApiModels;
using PeterO.Cbor;

namespace DotNut.Tests.Unit;

public class UnknownKeysetTests
{
    private const string C = "0279be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798";
    private const string KnownId = "009a1f293253e41e";

    private static string ProofJson(ulong amount, string id) =>
        $$"""{ "amount": {{amount}}, "id": "{{id}}", "secret": "s{{amount}}", "C": "{{C}}" }""";

    [Fact]
    public void GetKeysetsResponse_SkipsUnknownKeysetIds()
    {
        var json = $$"""
            { "keysets": [
              { "id": "{{KnownId}}", "unit": "sat", "active": true },
              { "id": "02abcdef", "unit": "sat", "active": true }
            ] }
            """;

        var response = JsonSerializer.Deserialize<GetKeysetsResponse>(json)!;

        Assert.Equal(KnownId, Assert.Single(response.Keysets).Id.ToString());
    }

    [Fact]
    public void PaymentRequestPayload_SkipsProofsWithUnknownKeysetIds()
    {
        var json = $$"""
            { "id": "pay1", "mint": "https://mint.example", "unit": "sat",
              "proofs": [ {{ProofJson(1, KnownId)}}, {{ProofJson(2, "02abcdef")}} ] }
            """;

        var payload = JsonSerializer.Deserialize<PaymentRequestPayload>(json)!;

        Assert.Equal(1UL, Assert.Single(payload.Proofs).Amount);
    }

    [Fact]
    public void TokenV3_SkipsProofsWithUnknownKeysetIds()
    {
        var json = $$"""
            { "token": [ { "mint": "https://mint.example",
              "proofs": [ {{ProofJson(1, KnownId)}}, {{ProofJson(2, "02abcdef")}} ] } ] }
            """;

        var token = JsonSerializer.Deserialize<CashuToken>(json)!;

        Assert.Equal(1UL, Assert.Single(token.Tokens[0].Proofs).Amount);
    }

    [Fact]
    public void TokenV4_SkipsProofSetsWithUnknownKeysetIds()
    {
        var token = new CashuToken
        {
            Unit = "sat",
            Tokens =
            [
                new CashuToken.Token(
                    "https://mint.example",
                    [
                        new Proof { Amount = 1, Id = new KeysetId(KnownId), Secret = new StringSecret("a"), C = new PubKey(C) },
                        new Proof { Amount = 2, Id = new KeysetId("00ffffffffffffff"), Secret = new StringSecret("b"), C = new PubKey(C) },
                    ]
                ),
            ],
        };
        var encoder = new CashuTokenV4Encoder();
        var cbor = encoder.ToCBORObject(token);
        cbor["t"][1]["i"] = CBORObject.FromObject(new byte[] { 0x02, 0xab, 0xcd, 0xef });

        var decoded = encoder.FromCBORObject(cbor);

        Assert.Equal(1UL, Assert.Single(decoded.Tokens[0].Proofs).Amount);
    }
}
