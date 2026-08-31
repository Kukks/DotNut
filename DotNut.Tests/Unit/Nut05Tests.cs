using System.Text.Json;
using DotNut.Abstractions;
using DotNut.ApiModels;

namespace DotNut.Tests.Unit;

public class Nut05Tests
{
    [Fact]
    public void PreferAsyncIsOnlySentWhenAsked()
    {
        var request = new PostMeltRequest { Quote = "q", Inputs = [] };
        Assert.DoesNotContain("prefer_async", JsonSerializer.Serialize(request));

        request.PreferAsync = true;
        Assert.Contains("\"prefer_async\":true", JsonSerializer.Serialize(request));
    }

    [Fact]
    public void ParsesMethodAndRequestOnTheMeltQuote()
    {
        // Example from 23.md after the NUT-05 change.
        const string json = """
            {
              "quote": "019e6d5a-2347-7000-8449-370fefb42fed",
              "request": "lnbc100n1p3kdrv5sp5...",
              "amount": 10,
              "unit": "sat",
              "fee_reserve": 2,
              "method": "bolt11",
              "state": "PENDING",
              "expiry": 1701704757
            }
            """;

        var quote = JsonSerializer.Deserialize<PostMeltQuoteBolt11Response>(json);
        Assert.NotNull(quote);

        Assert.Equal("bolt11", quote.Method);
        Assert.Equal("lnbc100n1p3kdrv5sp5...", quote.Request);
        Assert.Equal((ulong)2, quote.FeeReserve);
        Assert.Equal("sat", quote.Unit);
    }

    [Fact]
    public void FeeReserveMayBeOmitted()
    {
        const string json = """
            {
              "quote": "019e6d5a-2347-7000-8449-370fefb42fed",
              "amount": 10,
              "state": "UNPAID"
            }
            """;

        var quote = JsonSerializer.Deserialize<PostMeltQuoteBolt11Response>(json);
        Assert.NotNull(quote);

        // Absent means no reserve, which is what the melt builder treats it as.
        Assert.Null(quote.FeeReserve);
        Assert.Equal(0, Utils.CalculateNumberOfBlankOutputs(quote.FeeReserve ?? 0));
    }
}
