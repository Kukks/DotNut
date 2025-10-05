using DotNut.Abstractions;
using DotNut.Abstractions.Interfaces;
using DotNut.Api;
using Xunit.Sdk;

namespace DotNut.Tests;

public class Integration
{
    private static string MintUrl = "http://localhost:3338";

    private static string seed =
        "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";

    private static readonly Dictionary<int, string> valuesInvoices = new Dictionary<int, string>()
    {
        {1000, "lnbc10u1p5w6vggsp5gn5xhswgn5299w6elu2z0vzjxhf9hwd6pwjcgfwphaxunyu0dx6spp5a60trrhce2u6tzqjwjczem8rpdesgzkawcqg2xqaesz2kd50z4uqhp5uwcvgs5clswpfxhm7nyfjmaeysn6us0yvjdexn9yjkv3k7zjhp2sxq9z0rgqcqpnrzjqw22kc09xj0dm65ew5h5r003vtn72eyzchdgjag66l0yhwdudfmuzrwvesqq8qgqqgqqqqqqqqqqzhsq2q9qxpqysgq955gcfr95wwz0ehtnk3xraatkyhj88z44ku7yqutnwnt3gkh82jxehvdff7n2js2p54jgpvg6dmwmq8t9d8x05j63mqjrsr4cwd4lpcpnc39ru"}
    };
    private static ICounter counter = new InMemoryCounter();

    [Fact]
    public void CreatesWalletSuccesfully()
    {
        var wallet = Wallet.Create();
        Assert.NotNull(wallet);
    }
    
    [Fact]
    public async Task ThrowsWhenMintNotFound()
    {
        var wallet = Wallet.Create();
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await wallet.GetInfo());
        await Assert.ThrowsAsync<ArgumentNullException>(async () => wallet.Restore());
        await Assert.ThrowsAsync<ArgumentNullException>(async () => wallet.Swap());
        await Assert.ThrowsAsync<ArgumentNullException>(async () => wallet.CreateMeltQuote());
        await Assert.ThrowsAsync<ArgumentNullException>(async () => wallet.CreateMintQuote());
    }
    
    [Fact]
    public async Task FetchesInfoSuccessfully()
    {
        var wallet = Wallet.Create().WithMint(MintUrl);
        var info = await wallet.GetInfo();
        Assert.NotNull(info);
    }

    [Fact]
    public async Task MintsSuccessfully()
    {
        var wallet = Wallet.Create().WithMint(MintUrl);

        var mintQuote = await wallet
            .CreateMintQuote()
            .WithUnit("sat")
            .WithAmount(1337)
            .ProcessAsyncBolt11();
        
        Assert.NotNull(mintQuote);
        
        var paymentRequest = (await mintQuote.GetQuote()).Request;
        Assert.Contains("lnbc1337", paymentRequest);
        
        //We're using fakewallet, so after 3 secs it will get paid automatically
        await Task.Delay(3000);

        var mintResponse = await mintQuote.Mint();
        Assert.NotNull(mintResponse);
        Assert.Equal(1337UL, CashuUtils.SumProofs(mintResponse));
    }

    [Fact]
    public async Task MintsDeterministicSuccessfully()
    {
        var wallet = Wallet
            .Create()
            .WithMint(MintUrl)
            .WithMnemonic(seed)
            .WithCounter(counter);

        var mintQuote = await wallet
            .CreateMintQuote()
            .WithUnit("sat")
            .WithAmount(1337)
            .ProcessAsyncBolt11();
        
        Assert.NotNull(mintQuote);
        
        var paymentRequest = (await mintQuote.GetQuote()).Request;
        Assert.Contains("lnbc1337", paymentRequest);
        
        await Task.Delay(3000);
        var mintedProofs = await mintQuote.Mint();
        
        var keysetId = mintedProofs.First().Id;
        var currentCounter = await counter.GetCounterForId(keysetId);
        // counter is bumped after every use, so its already one more
        Assert.Equal(currentCounter, mintedProofs.Count);
    }
    
    [Fact]
    public async Task RestoresSuccessfully()
    {
        var wallet = Wallet
            .Create()
            .WithMint(MintUrl)
            .WithMnemonic(seed);
        var restoredProofs = await wallet
            .Restore()
            .WithSwap(false)
            .ProcessAsync();
         var keyset = (await wallet.GetKeys()).First().Keys;
         var expectedAmount = CashuUtils.SplitToProofsAmounts(1337UL, keyset).Count;
         Assert.Equal(expectedAmount, restoredProofs.Count());
    }
    
    [Fact]
    public async Task SwapsSuccessfully()
    {
        var wallet = Wallet
            .Create()
            .WithMint(MintUrl);
        
        // 1. mint some proofs (deterministic, because why not)
        var mintQuote = await wallet
            .CreateMintQuote()
            .WithAmount(64)
            .WithUnit("sat")
            .ProcessAsyncBolt11();
        
        await Task.Delay(3000);
        var mintedProofs = await mintQuote.Mint();
        Assert.NotEmpty(mintedProofs);
        
        //2. Swap them
        var newProofs = await wallet
            .Swap()
            .FromInputs(mintedProofs)
            .ProcessAsync();
        
        Assert.NotEmpty(newProofs);
    }

    public async Task SwapsDeterministicSuccessfully()
    {
        var wallet = Wallet
            .Create()
            .WithMint(MintUrl)
            .WithMnemonic(seed)
            .WithCounter(counter);
        
        // 1. mint some proofs (deterministic, because why not)
        var mintQuote = await wallet
            .CreateMintQuote()
            .WithAmount(64)
            .WithUnit("sat")
            .ProcessAsyncBolt11();
        
        await Task.Delay(3000);
        var mintedProofs = await mintQuote.Mint();
        Assert.NotEmpty(mintedProofs);
        
        //2. Swap them
        var newProofs = await wallet
            .Swap()
            .FromInputs(mintedProofs)
            .ProcessAsync();
        
        Assert.NotEmpty(newProofs);
    }

     [Fact]
     public async Task MeltsSuccessfully()
     {
         // mint proofs
         var wallet = Wallet
             .Create()
             .WithMint(MintUrl)
             .WithMnemonic(seed)
             .WithCounter(counter);

         var mintQuote = await wallet
             .CreateMintQuote()
             .WithUnit("sat")
             .WithAmount(1337)
             .ProcessAsyncBolt11();
         await Task.Delay(3000);
         var mintedProofs = await mintQuote.Mint();
         Assert.NotEmpty(mintedProofs);

         var Ids = mintedProofs.Select(proof => proof.Id).Count();
         
         Console.WriteLine($"amounts {Ids}");
         // create melt quote
         var meltQuote = await wallet
             .CreateMeltQuote()
             .WithInvoice(valuesInvoices[1000])
             .WithUnit("sat")
             .ProcessAsyncBolt11();

         // select proofs to send 
         var q = await meltQuote.GetQuote();
         var selectedProofs = await wallet.SelectProofsToSend(mintedProofs, q.Amount + (ulong)q.FeeReserve, true);
         
         //melt proofs 
         var change = await meltQuote.Melt(selectedProofs.Send);
         
         Assert.NotEmpty(change);
     }
}


