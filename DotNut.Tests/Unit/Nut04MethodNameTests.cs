using System.Text.Json;

namespace DotNut.Tests.Unit;

public class Nut04MethodNameTests
{
    [Theory]
    // Examples straight from 04.md.
    [InlineData("bolt11", "Bolt11")]
    [InlineData("apple-pay", "Apple Pay")]
    [InlineData("onchain", "Onchain")]
    [InlineData("onchain_payment_processor", "Onchain Payment Processor")]
    public void DerivesADisplayNameWhenTheMintSendsNone(string method, string expected)
    {
        Assert.Equal(expected, MethodNameFallback.DisplayNameFor(method, null));
        Assert.Equal(expected, MethodNameFallback.DisplayNameFor(method, ""));
    }

    [Fact]
    public void PrefersWhatTheMintSent()
    {
        Assert.Equal("Lightning", MethodNameFallback.DisplayNameFor("bolt11", "Lightning"));
    }

    [Theory]
    [InlineData("bolt11", true)]
    [InlineData("onchain-payment_processor", true)]
    [InlineData("", false)]
    [InlineData("Bolt11", false)]
    [InlineData("bolt 11", false)]
    [InlineData("bolt.11", false)]
    public void ValidatesTheMethodIdentifier(string method, bool valid)
    {
        Assert.Equal(valid, MethodNameFallback.IsValidMethod(method));
    }

    [Fact]
    public void MintSettingRoundTrips()
    {
        var setting = JsonSerializer.Deserialize<MintMethodSetting>(
            """{"method":"apple-pay","unit":"usd","method_name":null,"min_amount":1}"""
        );
        Assert.NotNull(setting);

        Assert.Null(setting.MethodName);
        Assert.Equal("Apple Pay", setting.DisplayName);

        // method_name is omitted rather than written as null.
        Assert.DoesNotContain("method_name", JsonSerializer.Serialize(setting));
    }

    [Fact]
    public void MeltSettingCarriesOptions()
    {
        var setting = JsonSerializer.Deserialize<MeltMethodSetting>(
            """{"method":"bolt11","unit":"sat","method_name":"Lightning","options":{"amountless":true}}"""
        );
        Assert.NotNull(setting);

        Assert.Equal("Lightning", setting.DisplayName);
        Assert.NotNull(setting.Options);
        Assert.True(setting.Options.RootElement.GetProperty("amountless").GetBoolean());
    }
}
