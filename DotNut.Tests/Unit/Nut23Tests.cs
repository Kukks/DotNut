using System.Text.Json;
using DotNut.ApiModels;
using DotNut.ApiModels.Mint.bolt12;

namespace DotNut.Tests.Unit;

public class Nut23Tests
{
    [Fact]
    public void ParsesTheAccountingFields()
    {
        // Example from 23.md, after the NUT-04 accounting change.
        const string json = """
            {
              "quote": "019e6d5a-2347-7000-8322-05d51d498303",
              "request": "lnbc100n1pj4apw9...",
              "unit": "sat",
              "method": "bolt11",
              "amount_paid": 100,
              "amount_issued": 40,
              "updated_at": 1701704657,
              "state": "PAID",
              "expiry": 1701704757
            }
            """;

        var quote = JsonSerializer.Deserialize<PostMintQuoteBolt11Response>(json);
        Assert.NotNull(quote);

        Assert.Equal((ulong)100, quote.AmountPaid);
        Assert.Equal((ulong)40, quote.AmountIssued);
        Assert.Equal(1701704657, quote.UpdatedAt);
        Assert.Equal("bolt11", quote.Method);

        // Partially issued: 60 left, which "PAID" alone cannot express.
        Assert.Equal((ulong)60, quote.Mintable);
    }

    [Fact]
    public void MintableIsUnknownWithoutTheAccountingFields()
    {
        // A mint that has not caught up sends state only.
        const string json = """
            {
              "quote": "019e6d5a-2347-7000-8322-05d51d498303",
              "request": "lnbc100n1pj4apw9...",
              "state": "UNPAID"
            }
            """;

        var quote = JsonSerializer.Deserialize<PostMintQuoteBolt11Response>(json);
        Assert.NotNull(quote);

        Assert.Null(quote.Mintable);
        Assert.Equal("UNPAID", quote.State);
    }

    [Fact]
    public void MintableIsZeroWhenEverythingWasIssued()
    {
        var quote = new PostMintQuoteBolt11Response { AmountPaid = 100, AmountIssued = 100 };
        Assert.Equal((ulong)0, quote.Mintable);

        // amount_issued must never exceed amount_paid, but do not underflow if a mint gets it wrong.
        var broken = new PostMintQuoteBolt11Response { AmountPaid = 10, AmountIssued = 40 };
        Assert.Equal((ulong)0, broken.Mintable);
    }

    [Fact]
    public void Bolt12QuoteExposesTheSameAccounting()
    {
        const string json = """
            {
              "quote": "019e6d5a-2347-7000-8449-370fefb42fed",
              "request": "lno1...",
              "unit": "sat",
              "method": "bolt12",
              "pubkey": "03d56ce4e446a85bbdaa547b4ec2b073d40ff802831352b8272b7dd7a4de5a7cac",
              "amount_paid": 500,
              "amount_issued": 0,
              "updated_at": 1701704657
            }
            """;

        var quote = JsonSerializer.Deserialize<PostMintQuoteBolt12Response>(json);
        Assert.NotNull(quote);

        Assert.Equal((ulong)500, quote.Mintable);
        Assert.Equal(1701704657, quote.UpdatedAt);
        Assert.Equal("bolt12", quote.Method);
    }

    [Fact]
    public void StateStaysOptionalOnTheWire()
    {
        // state is deprecated, so it must not be emitted when unset.
        var json = JsonSerializer.Serialize(
            new PostMintQuoteBolt11Response
            {
                Quote = "q",
                Request = "r",
                AmountPaid = 1,
                AmountIssued = 0,
            }
        );

        Assert.DoesNotContain("state", json);
        Assert.Contains("amount_paid", json);
    }
}
