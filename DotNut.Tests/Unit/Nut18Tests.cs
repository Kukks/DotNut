namespace DotNut.Tests.Unit;

public class Nut18Tests
{
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