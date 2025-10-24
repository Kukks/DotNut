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
    
    // for now cdk mint returns 500 if there's created melt quote for the same invoice twice
    private static readonly Dictionary<int, string> valuesInvoices = new Dictionary<int, string>()
    {
        {500, "lnbc5u1p5sh0yvsp53seej3qkkxe6xxk9mufaj7y3jc9s9kvfn4g3whppwqcl4vcjraaspp5vtv793xc9ksch8zekkhqtv54a2evh7vq4zuywcmk9nzt69qma5yqhp5uwcvgs5clswpfxhm7nyfjmaeysn6us0yvjdexn9yjkv3k7zjhp2sxq9z0rgqcqpnrzjq0ly0l075re9ltgqzdycartvas6g4c7kwwzpasj7a98c0ss679hdsr080vqqdcgqqqqqqqqnqqqqryqqxv9qxpqysgqwq50283v8asna95fktaeg80kq9evs0chaw44y6y649qsql9vsfc5gfcsp8rdwwyccepwy83n7g0s25n3lpv3hjgcr220n5w806fja8gp2xjvd7"},
        {501, "lnbc5010n1p5shs9rsp5a2qhmn05xsd8vcm5jx9v2aswkz0pxguk4jqlaxsazzcg5rduan2qpp5al2k5zwruvlx34sxxdys2sj696m58uqgjvzxxrxhvuyswhmzg5cqhp5uwcvgs5clswpfxhm7nyfjmaeysn6us0yvjdexn9yjkv3k7zjhp2sxq9z0rgqcqpnrzjqw7c9dkkx4nur9sztw2zzpzj8u8rgsqgdsykylg5pwplh26824lc7rvlqcqqn3gqqyqqqqlgqqqqqqgq2q9qxpqysgqgpj2x2aw2dv5tzhx86th6a5vutpcdxz9htewqgvzjgqkzwmh6xs5mw5xcgrzyq77f35shv0gg5ygtjmn7e73wg8v0a9g836ufszdxmqqqu3642"},
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
        Assert.Equal(1337UL, Utils.SumProofs(mintResponse));
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
         var expectedAmount = Utils.SplitToProofsAmounts(1337UL, keyset).Count;
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
         
         var swappedProofs = await wallet
             .Swap()
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
             .WithInvoice(valuesInvoices[501])
             .WithPrivkeys([privKeyBob, privKeyAlice])
             .ProcessAsyncBolt11();

         var q = await handler.GetQuote();
         
         var amountToPay = q.Amount + (ulong)q.FeeReserve;
         var selectorResponse = await wallet.SelectProofsToSend(proofs, amountToPay, true);
         var change = await handler.Melt(selectorResponse.Send);
         
         Assert.NotEmpty(change);
     }

[Fact]
public async Task SubscribeToMintMeltQuoteUpdates()
{
    await using var service = new WebsocketService();
    var connection = await service.ConnectAsync(MintUrl);
    Assert.NotNull(connection);

    var wallet = Wallet.Create().WithMint(MintUrl);

    var mintHandler = await wallet
        .CreateMintQuote()
        .WithAmount(3338)
        .WithUnit("sat")
        .ProcessAsyncBolt11();

    var quote = await mintHandler.GetQuote();

    var sub = await service.SubscribeToMintQuoteAsync(MintUrl, new[] { quote.Quote });

    int connectedCount = 0;
    int notificationCount = 0;

    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

    var connectedTcs = new TaskCompletionSource();
    var paidTcs = new TaskCompletionSource();

    _ = Task.Run(async () =>
    {
        await connectedTcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await Task.Delay(1000, cts.Token);
        await PayInvoice();
    }, cts.Token);

    await foreach (var msg in sub.NotificationChannel.Reader.ReadAllAsync(cts.Token))
    {
        switch (msg)
        {
            case WsMessage.Response:
                connectedCount++;
                connectedTcs.TrySetResult();
                break;

            case WsMessage.Notification notification:
                notificationCount++;

                if (notificationCount >= 2)
                    paidTcs.TrySetResult();

                break;

            case WsMessage.Error error:
                Assert.Fail($"WebSocket error: {error}");
                break;

            default:
                Assert.Fail($"Unexpected message type: {msg.GetType().Name}");
                break;
        }

        if (paidTcs.Task.IsCompleted)
            break;
    }
    
    Assert.Equal(1, connectedCount);
    Assert.True(notificationCount >= 2, $"Expected >=2 notifications, got {notificationCount}");

    var proofs = await mintHandler.Mint();
    Assert.NotEmpty(proofs);
}


     private async Task PayInvoice()
     {
         //We're using fakewallet, so after 3 secs it will get paid automatically. After 3.5 sec its 1000% paid.
         await Task.Delay(3500);
     }
}


