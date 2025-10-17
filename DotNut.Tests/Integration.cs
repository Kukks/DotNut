using System.Security.Cryptography;
using DotNut.Abstractions;
using DotNut.Abstractions.Interfaces;
using DotNut.Abstractions.Websockets;
using DotNut.Api;
using DotNut.ApiModels;
using Newtonsoft.Json;
using NuGet.Frameworks;
using Xunit.Sdk;

namespace DotNut.Tests;

public class Integration
{
    private static string MintUrl = "http://localhost:3338";

    private static string seed =
        "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";

    private static readonly Dictionary<int, string> valuesInvoices = new Dictionary<int, string>()
    {
        {500, "lnbc5u1p50xs65sp5rqewm2jqcddhnynncdx7gtz8qh7q6c9a2tlv6u2efa5qrltla9jqpp5raqnnlucn27y3lswuqafutrnsctcglr5ldv74009jp86cfv6pjyqhp5fszwn06y05csgs2mnn7yn6kn6j9d7m5fv6rw72m8hkp7re0zfflqxq9z0rgqcqpnrzjqdq8jm79ttkfnk83gfjee4n7ryyqzq9f36s5azgk2ftcndt7q48txr0hdyqqdcgqqqqqqqlgqqqqzycqyg9qxpqysgqthz50sp4xdtv2afwj294fd45e4s8q4ptqrn092v36zrs57wyur65lcdkxp53cza9an8z0drxw5lgdcay78plgmfle72vrtjp5266xlgqzsn4ph"},
        {1000, "lnbc10u1p5w6vggsp5gn5xhswgn5299w6elu2z0vzjxhf9hwd6pwjcgfwphaxunyu0dx6spp5a60trrhce2u6tzqjwjczem8rpdesgzkawcqg2xqaesz2kd50z4uqhp5uwcvgs5clswpfxhm7nyfjmaeysn6us0yvjdexn9yjkv3k7zjhp2sxq9z0rgqcqpnrzjqw22kc09xj0dm65ew5h5r003vtn72eyzchdgjag66l0yhwdudfmuzrwvesqq8qgqqgqqqqqqqqqqzhsq2q9qxpqysgq955gcfr95wwz0ehtnk3xraatkyhj88z44ku7yqutnwnt3gkh82jxehvdff7n2js2p54jgpvg6dmwmq8t9d8x05j63mqjrsr4cwd4lpcpnc39ru"},
        {2000, "lnbc20u1p5094fksp54vrdcymel5awhrpc0m6z4kvhhyvqlwkshkyt2wr6eyljkz8c798qpp59f2vc8td8tu62gtf4qfwzkrkxedsey7a5ajrd48a25z2kkwg407shp5nklhn663zgwcdnh7pe5jxt6td0cchhre6hxzdxrjdlfwtpq60f5sxq9z0rgqcqpnrzjqw0de9yc0j8n4hpgm269tm7qph4gwcyf5ys02uaapvpugrva87c7zr045uqq4jsqpsqqqqlgqqqqrcgq2q9qxpqysgq6g2pamgjumh6uw5k5rj2ket44wh8nfzs5gzyygl54hu5cefuxdhxp9h5mrg64rh07znktn9x9d5vg6fc0rw7m63x8rg4qk3kw6d8sycpywn48m"},
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
        
        await PayInvoice();

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

        await PayInvoice();
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

        await PayInvoice();
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

        await PayInvoice();
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

     [Fact]
     public async Task InvoiceWithDescription()
     {
         var wallet = Wallet
             .Create()
             .WithMint(MintUrl);

         var quote = await wallet.CreateMintQuote()
             .WithDescription("Test Description")
             .WithAmount(1337)
             .ProcessAsyncBolt11();
         
         Assert.NotNull(quote);
     }

     [Fact]
     public async Task FeeForExternalInvoice()
     {
         var wallet = Wallet
             .Create()
             .WithMint(MintUrl);

         var meltHandler = await wallet.CreateMeltQuote()
             .WithInvoice(valuesInvoices[2000])
             .ProcessAsyncBolt11();
         
         Assert.NotNull(meltHandler);
         
         var quote = await meltHandler.GetQuote();
         
         Assert.NotNull(quote);
         Assert.True(quote.FeeReserve > 0);
     }

     [Fact]
     public async Task SwapP2Pk()
     {
         // p2pk aren't deterministic, so wallet is initialized without mnemonic and counter
         var wallet = Wallet
             .Create()
             .WithMint(MintUrl);

         var privKeyAlice = new PrivKey(RandomNumberGenerator.GetHexString(64, true));
         var privKeyBob = new PrivKey(RandomNumberGenerator.GetHexString(64, true));

         var mintHandler = await wallet.CreateMintQuote()
             .WithAmount(1337)
             .WithP2PkLock(new P2PkBuilder()
                {
                    Pubkeys = [privKeyBob.Key.CreatePubKey()],
                    SignatureThreshold = 1
                }
             ).ProcessAsyncBolt11();

         await PayInvoice();
         var proofs = await mintHandler.Mint();
         
         // no privkeys
         await Assert.ThrowsAsync<CashuProtocolException>(
             async () => await wallet
                 .Swap()
                 .FromInputs(proofs)
                 .ProcessAsync()
         );

         // wrong privkey
         await Assert.ThrowsAsync<InvalidOperationException>(
             async () => await wallet
                 .Swap()
                 .FromInputs(proofs)
                 .WithPrivkeys([privKeyAlice.Key])
                 .ProcessAsync()
         );
         
         var swappedProofs = await wallet.
             Swap()
             .FromInputs(proofs)
             .WithPrivkeys([privKeyBob])
             .ProcessAsync();
         
         Assert.NotEmpty(swappedProofs);
     }


     [Fact]
     public async Task MintMeltP2PkMultisig()
     {
         var wallet = Wallet
             .Create()
             .WithMint(MintUrl);

         var privKeyAlice = new PrivKey(RandomNumberGenerator.GetHexString(64, true));
         var privKeyBob = new PrivKey(RandomNumberGenerator.GetHexString(64, true));

         var mintHandler = await wallet.CreateMintQuote()
             .WithAmount(1337)
             .WithP2PkLock(new P2PkBuilder()
                 {
                     Pubkeys = [privKeyBob.Key.CreatePubKey(), privKeyAlice.Key.CreatePubKey()],
                     SignatureThreshold = 2
                 }
             ).ProcessAsyncBolt11();
         await PayInvoice();
         
         var proofs = await mintHandler.Mint();
         
         Assert.NotEmpty(proofs);

         // no privkeys
         await Assert.ThrowsAsync<CashuProtocolException>(async () =>
         {
             var meltHandler = await wallet
                 .CreateMeltQuote()
                 .WithInvoice(valuesInvoices[500])
                 .ProcessAsyncBolt11();
             await meltHandler.Melt(proofs);
         });
         
         var handler = await wallet
             .CreateMeltQuote()
             .WithInvoice(valuesInvoices[500])
             .WithPrivkeys([privKeyBob, privKeyAlice])
             .ProcessAsyncBolt11();

         var selectorResponse = await wallet.SelectProofsToSend(proofs, 500UL, true);
         var change = await handler.Melt(selectorResponse.Send);
         
         Assert.NotEmpty(change);
     }

     [Fact]
     public async Task SubscribeToMintMeltQuoteUpdates()
     {
         // initialize websocket service. it will be a singleton normally.
         WebsocketService service = new WebsocketService();
         var connection = await service.ConnectAsync(MintUrl);
         Assert.NotNull(connection);
         
         // create mint quote
         var wallet = Wallet
             .Create()
             .WithMint(MintUrl);

         var mintHandler = await wallet
             .CreateMintQuote()
             .WithAmount(3338)
             .WithUnit("sat")
             .ProcessAsyncBolt11();

         var quote = await mintHandler.GetQuote();

         var sub = await service.SubscribeToMintQuoteAsync(MintUrl, [quote.Quote]);

         int ctr = 0;
         var callback = () => ctr++;
         await foreach (var msg in sub.NotificationChannel.Reader.ReadAllAsync())
         {
             callback();
             if (ctr > 1)
             {
                 Assert.Equal(sub.Id, msg.SubId);
                 Assert.True(msg.Payload is PostMeltQuoteBolt11Response);
                 break;
             }
         }
         
         // payQuote
         await PayInvoice();
         
         Assert.True(ctr > 1);
         
         var proofs = await mintHandler.Mint();
         
         
     }


     private async Task PayInvoice()
     {
         //We're using fakewallet, so after 3 secs it will get paid automatically. After 3.5 sec its 1000% paid.
         await Task.Delay(3500);
     }
}


