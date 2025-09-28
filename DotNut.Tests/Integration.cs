using DotNut.Abstractions;
using DotNut.Api;

namespace DotNut.Tests;

public class Integration
{
    const string MintUrl = "http://localhost:3338";

    [Fact]
    public void CreatesWalletSuccesfully()
    {
        var wallet = Wallet.Create();
        Assert.NotNull(wallet);
    }
    [Fact]
    public async Task FetchesInfoSuccessfully()
    {
        var wallet = Wallet.Create().WithMint(MintUrl);
        var info = await wallet.GetInfo();
        Assert.NotNull(info);
    }
    
    [Fact]
    public async Task ThrowsWhenMintNotFound()
    {
        var wallet = Wallet.Create();
        await Assert.ThrowsAsync<Exception>(async () => await wallet.GetInfo());
        await Assert.ThrowsAsync<Exception>(async () => wallet.Restore());
        await Assert.ThrowsAsync<Exception>(async () => wallet.Swap());
        await Assert.ThrowsAsync<Exception>(async () => wallet.CreateMeltQuote());
        await Assert.ThrowsAsync<Exception>(async () => wallet.CreateMintQuote());
    }

    [Fact]
    public async Task MeltsSucessfully()
    {
        var wallet = Wallet.Create().WithMint(MintUrl);

        var quote = wallet
            .CreateMintQuote()
            .WithMethod("bolt11")
            .WithUnit()
            

    }
    
}