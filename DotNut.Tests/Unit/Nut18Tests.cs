using Xunit;

namespace DotNut.Tests.Unit;

public class Nut18Tests
{
    [Fact]
    public void PreferredMintWithFeeReserveAndSupportedMethods_Cbor()
    {
        // Test vector from NUT-18 PR #381
        const string creqA =
            "creqAp2FpdXByZWZlcnJlZF9mZWVfbWV0aG9kc2FhGGRhdWNzYXRhbYF4GGh0dHBzOi8vbWludC5leGFtcGxlLmNvbWJtc_RiZnICYnNtgmZib2x0MTFmYm9sdDEy";
        var pr = PaymentRequest.Parse(creqA);
        Assert.Equal("preferred_fee_methods", pr.PaymentId);
        Assert.Equal((ulong)100, pr.Amount);
        Assert.Equal("sat", pr.Unit);
        Assert.Equal("https://mint.example.com", Assert.Single(pr.Mints));
        Assert.False(pr.MintsStrict);
        Assert.Equal((ulong)2, pr.FeeReserve);
        Assert.Equal(new[] { "bolt11", "bolt12" }, pr.SupportedMethods);
    }

    [Fact]
    public void PreferredMintWithFeeReserveAndSupportedMethods_Bech32()
    {
        // Test vector from NUT-18 PR `#381`
        const string creqB =
            "CREQB1QYQP2URJV4NX2UNJV4J97EN9V40K6ET5DPHKGUCZQQYQQQQQQQQQQQRYQVQQZQQ9QQVXSAR5WPEN5TE0D45KUAPWV4UXZMTSD3JJUCM0D5YSQQGQPGQQSQQQQQQQQQQQQG9SQPNZDAK8GVF3PVQQVCN0D36RZVSUP24PH";
        var pr = PaymentRequest.Parse(creqB);
        Assert.Equal("preferred_fee_methods", pr.PaymentId);
        Assert.Equal((ulong)100, pr.Amount);
        Assert.Equal("sat", pr.Unit);
        Assert.Equal("https://mint.example.com", Assert.Single(pr.Mints));
        Assert.False(pr.MintsStrict);
        Assert.Equal((ulong)2, pr.FeeReserve);
        Assert.Equal(new[] { "bolt11", "bolt12" }, pr.SupportedMethods);
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