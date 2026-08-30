using System.Text.Json;
using DotNut.Abstractions;
using DotNut.ApiModels;

namespace DotNut.Tests.Unit;

public class Nut22PathMatchingTests
{
    private static MintInfo InfoWith(params (string Method, string Path)[] endpoints)
    {
        var entries = string.Join(
            ",",
            endpoints.Select(e =>
                JsonSerializer.Serialize(
                    new ProtectedEndpointSpec { Method = e.Method, Path = e.Path }
                )
            )
        );

        var info = JsonSerializer.Deserialize<GetInfoResponse>(
            "{\"nuts\":{\"22\":{\"protected_endpoints\":[" + entries + "]}}}"
        );

        return new MintInfo(info!);
    }

    [Fact]
    public void ExactPathMatchesOnlyItself()
    {
        var info = InfoWith(("POST", "/v1/auth/blind/mint"));

        Assert.True(info.RequiresBlindAuthToken("/v1/auth/blind/mint"));
        Assert.False(info.RequiresBlindAuthToken("/v1/auth/blind/mint/extra"));
        Assert.False(info.RequiresBlindAuthToken("/prefix/v1/auth/blind/mint"));
    }

    [Fact]
    public void TrailingStarMatchesByPrefix()
    {
        var info = InfoWith(("POST", "/v1/mint/*"));

        Assert.True(info.RequiresBlindAuthToken("/v1/mint/quote/bolt11"));
        Assert.True(info.RequiresBlindAuthToken("/v1/mint/bolt11"));
        Assert.False(info.RequiresBlindAuthToken("/v1/melt/bolt11"));
        // The prefix includes the slash, so /v1/mintxyz is a different endpoint.
        Assert.False(info.RequiresBlindAuthToken("/v1/mintxyz"));
    }

    [Fact]
    public void StarNeedNotFollowASlash()
    {
        var info = InfoWith(("POST", "/v1/mint/bolt*"));

        Assert.True(info.RequiresBlindAuthToken("/v1/mint/bolt11"));
        Assert.True(info.RequiresBlindAuthToken("/v1/mint/bolt12"));
        Assert.False(info.RequiresBlindAuthToken("/v1/mint/onchain"));
    }

    [Fact]
    public void OnlyTheNamedMethodIsProtected()
    {
        var info = InfoWith(("POST", "/v1/mint/*"));

        Assert.True(info.RequiresBlindAuthToken("/v1/mint/quote/bolt11", "POST"));
        // Reading a quote is a GET and is not covered by a POST entry.
        Assert.False(info.RequiresBlindAuthToken("/v1/mint/quote/bolt11", "GET"));
    }

    [Fact]
    public void PathsAreDataNotPatterns()
    {
        // A mint sending regex metacharacters gets them treated literally. Under the old
        // regex matching "." matched any character and the match was unanchored, so this
        // would have protected far more than the mint asked for.
        var info = InfoWith(("POST", "/v1/mint/."));

        Assert.True(info.RequiresBlindAuthToken("/v1/mint/."));
        Assert.False(info.RequiresBlindAuthToken("/v1/mint/x"));
    }

    [Fact]
    public void CatastrophicPatternsAreJustStrings()
    {
        // (a+)+$ against a long non-matching input is the classic backtracking bomb.
        var info = InfoWith(("POST", "/v1/(a+)+$"));

        Assert.False(info.RequiresBlindAuthToken("/v1/" + new string('a', 40) + "!"));
    }

    [Fact]
    public void NothingIsProtectedWithoutTheNut()
    {
        var info = new MintInfo(JsonSerializer.Deserialize<GetInfoResponse>("{}")!);

        Assert.False(info.RequiresBlindAuthToken("/v1/mint/quote/bolt11"));
    }
}
