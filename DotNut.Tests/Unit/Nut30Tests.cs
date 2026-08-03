using System.Text.Json;
using DotNut.ApiModels.Melt.Onchain;
using DotNut.ApiModels.Onchain;

namespace DotNut.Tests.Unit;

public class Nut30Tests
{
    [Fact]
    public void ParsesTheMintQuote()
    {
        // Example from 30.md.
        const string json = """
            {
              "quote": "019e6d5a-2347-7000-8850-39c85ed1b5d3",
              "request": "bc1q...",
              "unit": "sat",
              "method": "onchain",
              "expiry": 1701704757,
              "pubkey": "03d56ce4e446a85bbdaa547b4ec2b073d40ff802831352b8272b7dd7a4de5a7cac",
              "amount_paid": 0,
              "amount_issued": 0,
              "updated_at": 1701704657
            }
            """;

        var quote = JsonSerializer.Deserialize<PostMintQuoteOnchainResponse>(json);
        Assert.NotNull(quote);

        Assert.Equal("bc1q...", quote.Request);
        Assert.Equal("onchain", quote.Method);
        Assert.Equal(1701704657, quote.UpdatedAt);
        Assert.Equal((ulong)0, quote.Mintable);
    }

    [Fact]
    public void AnAddressCanBePaidRepeatedly()
    {
        // Three UTXOs confirmed, one mint already taken against them.
        var quote = new PostMintQuoteOnchainResponse { AmountPaid = 300, AmountIssued = 100 };

        Assert.Equal((ulong)200, quote.Mintable);
    }

    [Fact]
    public void ParsesTheMeltQuoteWithItsFeeOptions()
    {
        const string json = """
            {
              "quote": "019e6d5a-2347-7000-82b3-56e12c3fcdf2",
              "amount": 10000,
              "unit": "sat",
              "method": "onchain",
              "state": "UNPAID",
              "expiry": 1701704757,
              "request": "bc1q...",
              "fee_options": [
                { "fee_index": 0, "fee_reserve": 500, "estimated_blocks": 1 },
                { "fee_index": 1, "fee_reserve": 200, "estimated_blocks": 6 }
              ]
            }
            """;

        var quote = JsonSerializer.Deserialize<PostMeltQuoteOnchainResponse>(json);
        Assert.NotNull(quote);

        Assert.Equal("onchain", quote.Method);
        Assert.Null(quote.SelectedFeeIndex);

        // The wallet picks one option by fee_index when executing the melt; the mint rejects
        // any index it did not offer, and the set is fixed for the quote's lifetime.
        Assert.Equal(2, quote.FeeOptions.Length);
        Assert.Equal((ulong)500, quote.FeeOptions[0].FeeReserve);
        Assert.Equal((ulong)1, quote.FeeOptions[0].EstimatedBlocks);
        Assert.Equal((ulong)6, quote.FeeOptions[1].EstimatedBlocks);
    }
}
