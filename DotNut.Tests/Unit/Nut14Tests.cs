using System.Text.Json;

namespace DotNut.Tests.Unit;

public class Nut14Tests
{
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
}