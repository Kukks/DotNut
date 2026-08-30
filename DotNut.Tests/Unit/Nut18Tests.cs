namespace DotNut.Tests.Unit;

public class Nut18Tests
{
    [Fact]
    public void PreferredMintWithSupportedMethods_Cbor()
    {
        // Test vector from NUT-18 "Preferred Mint List with Supported Methods"
        const string creqA =
            "creqApmFpdXByZWZlcnJlZF9mZWVfbWV0aG9kc2FhGGRhdWNzYXRhbYF4GGh0dHBzOi8vbWludC5leGFtcGxlLmNvbWJtcPVic22CoWJtbmZib2x0MTGiYm1uZmJvbHQxMmJtZgU=";
        var pr = PaymentRequest.Parse(creqA);
        AssertPreferredMintRequest(pr);

        // The CBOR encoder emits canonically ordered keys, so re-encoding is only
        // asserted to be semantically equivalent, not byte-identical to the vector.
        AssertPreferredMintRequest(PaymentRequest.Parse(pr.ToString()));
    }

    [Fact]
    public void PreferredMintWithSupportedMethods_Bech32()
    {
        // Test vector from NUT-26 "Preferred Mint List with Supported Methods"
        const string creqB =
            "CREQB1QYQP2URJV4NX2UNJV4J97EN9V40K6ET5DPHKGUCZQQYQQQQQQQQQQQRYQVQQZQQ9QQVXSAR5WPEN5TE0D45KUAPWV4UXZMTSD3JJUCM0D5YSQQGPPGQQJQGQQE3X7MR5XYCS5QQ5QYQQVCN0D36RZVSZQQYQQQQQQQQQQQQ9FJ2568";
        var pr = PaymentRequest.Parse(creqB);
        AssertPreferredMintRequest(pr);

        Assert.Equal(creqB, pr.ToBech32String());
    }

    private static void AssertPreferredMintRequest(PaymentRequest pr)
    {
        Assert.Equal("preferred_fee_methods", pr.PaymentId);
        Assert.Equal((ulong)100, pr.Amount);
        Assert.Equal("sat", pr.Unit);
        Assert.Equal("https://mint.example.com", Assert.Single(pr.Mints));
        Assert.True(pr.MintPreferred);

        Assert.Equal(2, pr.SupportedMethods?.Length);
        Assert.Equal("bolt11", pr.SupportedMethods[0].Method);
        Assert.Null(pr.SupportedMethods[0].Fee);
        Assert.Equal("bolt12", pr.SupportedMethods[1].Method);
        Assert.Equal((ulong)5, pr.SupportedMethods[1].Fee);
    }


    [Fact]
    public void ValidPr()
    {
        var creqA =
            "creqApWF0gaNhdGVub3N0cmFheKlucHJvZmlsZTFxeTI4d3VtbjhnaGo3dW45ZDNzaGp0bnl2OWtoMnVld2Q5aHN6OW1od2RlbjV0ZTB3ZmprY2N0ZTljdXJ4dmVuOWVlaHFjdHJ2NWhzenJ0aHdkZW41dGUwZGVoaHh0bnZkYWtxcWd5ZGFxeTdjdXJrNDM5eWtwdGt5c3Y3dWRoZGh1NjhzdWNtMjk1YWtxZWZkZWhrZjBkNDk1Y3d1bmw1YWeBgmFuYjE3YWloYjdhOTAxNzZhYQphdWNzYXRhbYF4Imh0dHBzOi8vbm9mZWVzLnRlc3RudXQuY2FzaHUuc3BhY2U=";
        var pr = PaymentRequest.Parse(creqA);
        Assert.Equal("https://nofees.testnut.cashu.space", Assert.Single(pr.Mints));
        Assert.Equal((ulong)10, pr.Amount);
        Assert.Equal("b7a90176", pr.PaymentId);
        Assert.Equal("sat", pr.Unit);
        var t = Assert.Single(pr.Transports);
        Assert.Equal("nostr", t.Type);
        Assert.Equal(
            "nprofile1qy28wumn8ghj7un9d3shjtnyv9kh2uewd9hsz9mhwden5te0wfjkccte9curxven9eehqctrv5hszrthwden5te0dehhxtnvdakqqgydaqy7curk439ykptkysv7udhdhu68sucm295akqefdehkf0d495cwunl5",
            t.Target
        );
        Assert.Equal("n", Assert.Single(t.Tags).Key);
        Assert.Equal("17", Assert.Single(t.Tags).Value[0]);
        // Assert.Equal(creqA, pr.ToString());
    }
}