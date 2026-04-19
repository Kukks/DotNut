using System.Text.Json;
using DotNut.ApiModels;
using DotNut.NBitcoin.BIP39;
using DotNut.NUT13;
using NBip32Fast;
using NBitcoin.Secp256k1;
using Xunit.Abstractions;

namespace DotNut.Tests;

public class UnitTest1
{
    private readonly ITestOutputHelper _testOutputHelper;

    public UnitTest1(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }
    
    [Fact]
    public void NullExpiryTests_PostMintQuoteBolt11Response()
    {
        // Test JSON with null expiry field
        var jsonWithNullExpiry = """
            {
                "quote": "test-quote-id",
                "request": "test-request",
                "state": "PAID",
                "expiry": null,
                "amount": 1000,
                "unit": "sat"
            }
            """;

        var response = JsonSerializer.Deserialize<PostMintQuoteBolt11Response>(jsonWithNullExpiry);

        Assert.NotNull(response);
        Assert.Equal("test-quote-id", response.Quote);
        Assert.Equal("test-request", response.Request);
        Assert.Equal("PAID", response.State);
        Assert.Null(response.Expiry);
        Assert.Equal((ulong)1000, response.Amount);
        Assert.Equal("sat", response.Unit);

        // Test JSON without expiry field (should also be null)
        var jsonWithoutExpiry = """
            {
                "quote": "test-quote-id-2",
                "request": "test-request-2",
                "state": "UNPAID",
                "amount": 500,
                "unit": "sat"
            }
            """;

        var response2 = JsonSerializer.Deserialize<PostMintQuoteBolt11Response>(jsonWithoutExpiry);

        Assert.NotNull(response2);
        Assert.Equal("test-quote-id-2", response2.Quote);
        Assert.Equal("test-request-2", response2.Request);
        Assert.Equal("UNPAID", response2.State);
        Assert.Null(response2.Expiry);
        Assert.Equal((ulong)500, response2.Amount);
        Assert.Equal("sat", response2.Unit);

        // Test JSON with valid expiry value (should still work)
        var jsonWithExpiry = """
            {
                "quote": "test-quote-id-3",
                "request": "test-request-3",
                "state": "ISSUED",
                "expiry": 1640995200,
                "amount": 2000,
                "unit": "sat"
            }
            """;

        var response3 = JsonSerializer.Deserialize<PostMintQuoteBolt11Response>(jsonWithExpiry);

        Assert.NotNull(response3);
        Assert.Equal("test-quote-id-3", response3.Quote);
        Assert.Equal("test-request-3", response3.Request);
        Assert.Equal("ISSUED", response3.State);
        Assert.Equal(1640995200, response3.Expiry);
        Assert.Equal((ulong)2000, response3.Amount);
        Assert.Equal("sat", response3.Unit);
    }

    [Fact]
    public void NullExpiryTests_PostMeltQuoteBolt11Response()
    {
        // Test JSON with null expiry field
        var jsonWithNullExpiry = """
            {
                "quote": "melt-quote-id",
                "amount": 1000,
                "fee_reserve": 50,
                "state": "PAID",
                "expiry": null,
                "payment_preimage": "test-preimage"
            }
            """;

        var response = JsonSerializer.Deserialize<PostMeltQuoteBolt11Response>(jsonWithNullExpiry);

        Assert.NotNull(response);
        Assert.Equal("melt-quote-id", response.Quote);
        Assert.Equal((ulong)1000, response.Amount);
        Assert.Equal(50, response.FeeReserve);
        Assert.Equal("PAID", response.State);
        Assert.Null(response.Expiry);
        Assert.Equal("test-preimage", response.PaymentPreimage);

        // Test JSON without expiry field (should also be null)
        var jsonWithoutExpiry = """
            {
                "quote": "melt-quote-id-2",
                "amount": 500,
                "fee_reserve": 25,
                "state": "UNPAID"
            }
            """;

        var response2 = JsonSerializer.Deserialize<PostMeltQuoteBolt11Response>(jsonWithoutExpiry);

        Assert.NotNull(response2);
        Assert.Equal("melt-quote-id-2", response2.Quote);
        Assert.Equal((ulong)500, response2.Amount);
        Assert.Equal(25, response2.FeeReserve);
        Assert.Equal("UNPAID", response2.State);
        Assert.Null(response2.Expiry);
        Assert.Null(response2.PaymentPreimage);

        // Test JSON with valid expiry value (should still work)
        var jsonWithExpiry = """
            {
                "quote": "melt-quote-id-3",
                "amount": 2000,
                "fee_reserve": 100,
                "state": "PENDING",
                "expiry": 1640995200
            }
            """;

        var response3 = JsonSerializer.Deserialize<PostMeltQuoteBolt11Response>(jsonWithExpiry);

        Assert.NotNull(response3);
        Assert.Equal("melt-quote-id-3", response3.Quote);
        Assert.Equal((ulong)2000, response3.Amount);
        Assert.Equal(100, response3.FeeReserve);
        Assert.Equal("PENDING", response3.State);
        Assert.Equal(1640995200, response3.Expiry);
        Assert.Null(response3.PaymentPreimage);
    }
}
