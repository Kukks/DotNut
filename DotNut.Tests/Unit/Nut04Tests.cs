using System.Text.Json;

namespace DotNut.Tests.Unit;

public class Nut04Tests
{
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

}