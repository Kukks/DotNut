using System.Security.Cryptography;
using DotNut.Abstractions;
using DotNut.Abstractions.Websockets;
using DotNut.Api;

namespace DotNut.Tests;

public class Integration
{
    private static string MintUrl = "http://localhost:3338";
    // private static string MintUrl = "https://fake.thesimplekid.dev";
    // private static string MintUrl = "https://testnut.cashu.space";

    private static string seed =
        "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
    
    // for now cdk mint returns 500 if there's created melt quote for the same invoice twice
    private static readonly Dictionary<int, string> valuesInvoices = new Dictionary<int, string>()
    {
        {500, "lnbc5u1p5sh0yvsp53seej3qkkxe6xxk9mufaj7y3jc9s9kvfn4g3whppwqcl4vcjraaspp5vtv793xc9ksch8zekkhqtv54a2evh7vq4zuywcmk9nzt69qma5yqhp5uwcvgs5clswpfxhm7nyfjmaeysn6us0yvjdexn9yjkv3k7zjhp2sxq9z0rgqcqpnrzjq0ly0l075re9ltgqzdycartvas6g4c7kwwzpasj7a98c0ss679hdsr080vqqdcgqqqqqqqqnqqqqryqqxv9qxpqysgqwq50283v8asna95fktaeg80kq9evs0chaw44y6y649qsql9vsfc5gfcsp8rdwwyccepwy83n7g0s25n3lpv3hjgcr220n5w806fja8gp2xjvd7"},
        {501, "lnbc5010n1p5shs9rsp5a2qhmn05xsd8vcm5jx9v2aswkz0pxguk4jqlaxsazzcg5rduan2qpp5al2k5zwruvlx34sxxdys2sj696m58uqgjvzxxrxhvuyswhmzg5cqhp5uwcvgs5clswpfxhm7nyfjmaeysn6us0yvjdexn9yjkv3k7zjhp2sxq9z0rgqcqpnrzjqw7c9dkkx4nur9sztw2zzpzj8u8rgsqgdsykylg5pwplh26824lc7rvlqcqqn3gqqyqqqqlgqqqqqqgq2q9qxpqysgqgpj2x2aw2dv5tzhx86th6a5vutpcdxz9htewqgvzjgqkzwmh6xs5mw5xcgrzyq77f35shv0gg5ygtjmn7e73wg8v0a9g836ufszdxmqqqu3642"},
        {502, "lnbc5020n1p5j3nxasp5qz7utfrp954nxp8049tqzg0t23krdj59thfcrc2g5h6lsemzvyfqpp5ms6xd7grtak0nr8lwytsclmq3d233v7gy7j0kuw32txhjq0f8ngqhp5uwcvgs5clswpfxhm7nyfjmaeysn6us0yvjdexn9yjkv3k7zjhp2sxq9z0rgqcqpnrzjqg587a2yyuqeua9c3j8nw7wwpx709slwl5lzfs0t0vq3kdwemzp67rtwevqq95gqqyqqq5sgqqq9yzqq2q9qxpqysgqgpp0zsetj9fedvr0szpwjfw2weckygmjthhnfpp2cerjtrn8n0pxyvrtc00l0jwzkqhwedcvgqljtwx3a7qplqp43jlxe4mpmw5svlgqfwa9yy"},
        {503, "lnbc5030n1p5n9kk6sp5ee6rsflv9rnnyt80ucc0fzlwa975nmufs2dn0x3u0hlerxxtc4nqpp5ew5mxfmu966c8wywvnvtgsljduq0jduvdpc3jzqzq9uqafx7ejgshp5uwcvgs5clswpfxhm7nyfjmaeysn6us0yvjdexn9yjkv3k7zjhp2sxq9z0rgqcqpnrzjqdqne4nrkxmz96ktnngat4nzx7sv0kf5uqmgfvqvvars7pac7fn9wr8fjvqq3csqqqqqqqqqqqqq0scqvs9qxpqysgq5lqwgfk6vv36tnlx2tv6reu2587x8ha2wsht0s75dpzvmknpgepsqaq9wnlx7n87j3x6w0vvkvc4qgda6mhacygn9f0xgagwt84uxtcqgtcpfs"},
        {999, "lnbc9990n1p5j3cf7sp575w4pw93kfrghl2gh68885v76gwjpzuv435t52q846cvx4w7yuvqpp5hdzvm3yf0r3vj99c7esmcv7zuj2fralf2twhl6s9xqcgr8g7nwyqhp5uwcvgs5clswpfxhm7nyfjmaeysn6us0yvjdexn9yjkv3k7zjhp2sxq9z0rgqcqpnrzjqt0mfswatysklf4z358sztscs5t0vdghmd5vfe9c9sa0gy6r5pdugrs7myqqvgqqqyqqqqqqqqqq86qq8s9qxpqysgq5wh9l4fy32ww4770mqm7yqvhwllaqyssvp335gjz6t59ca03gecyvdd9uv0ztrcm2uf2352wvwxcfh7yukucp4p6zu6ll867aj686wsqz0jlmt"},
        {1000, "lnbc10u1p5w6vggsp5gn5xhswgn5299w6elu2z0vzjxhf9hwd6pwjcgfwphaxunyu0dx6spp5a60trrhce2u6tzqjwjczem8rpdesgzkawcqg2xqaesz2kd50z4uqhp5uwcvgs5clswpfxhm7nyfjmaeysn6us0yvjdexn9yjkv3k7zjhp2sxq9z0rgqcqpnrzjqw22kc09xj0dm65ew5h5r003vtn72eyzchdgjag66l0yhwdudfmuzrwvesqq8qgqqgqqqqqqqqqqzhsq2q9qxpqysgq955gcfr95wwz0ehtnk3xraatkyhj88z44ku7yqutnwnt3gkh82jxehvdff7n2js2p54jgpvg6dmwmq8t9d8x05j63mqjrsr4cwd4lpcpnc39ru"},
        {1150, "lnbc11500n1p5jnmr7sp5u0s2wpuqn4mp0axyzgmsxzf5v8sy3zmzz9a7jyq38luyx9cntazqpp57j3carehwt4tqthxz9z7ea80t0htklh4v6v96dtn4vxuu4kwsershp53mwsvrcmkv743nyfzjp5a5fqrg2yngda3apf7jf9rzsuwt82wt3sxq9z0rgqcqpnrzjqg587a2yyuqeua9c3j8nw7wwpx709slwl5lzfs0t0vq3kdwemzp67rtwevqq95gqqyqqq5sgqqq9yzqq2q9qxpqysgqe97nwd9q74ua0sl9877sdprjcuc6jpyy8c52azpz8au6ur8q3838c0a0upnahs8w3sec8kxh26m3v9rkgqej36652t3sa5t25svacdcq5qwwjp"},
        {1151, "lnbc11510n1p5n9hzpsp5ey8npxa4nsaet73nc74lky0mv780h6890ua3kqhffvn8heqzk33spp5df0xt9s0e0kh0rx7dcy39u4q3g7cknk88wr4s90cldv6z2vwspgqhp5uwcvgs5clswpfxhm7nyfjmaeysn6us0yvjdexn9yjkv3k7zjhp2sxq9z0rgqcqpnrzjq0xp6zfjhwvmq6tltd09jcdc82ml6eh3alzvnaw8httxcx7tu78syrvfkqqqm0qqqyqqqqlgqqqvx5qqjq9qxpqysgqwnys3mklnsnrw5ysa8cjtynlqnllyxskcsamr7x96nl5kllyqcznlyeeuklr3zydeq43k6ckyrgqqfg965dsdjc675lvlssn0z4sxusq0lzrx6"},
        {2000, "lnbc20u1p5094fksp54vrdcymel5awhrpc0m6z4kvhhyvqlwkshkyt2wr6eyljkz8c798qpp59f2vc8td8tu62gtf4qfwzkrkxedsey7a5ajrd48a25z2kkwg407shp5nklhn663zgwcdnh7pe5jxt6td0cchhre6hxzdxrjdlfwtpq60f5sxq9z0rgqcqpnrzjqw0de9yc0j8n4hpgm269tm7qph4gwcyf5ys02uaapvpugrva87c7zr045uqq4jsqpsqqqqlgqqqqrcgq2q9qxpqysgq6g2pamgjumh6uw5k5rj2ket44wh8nfzs5gzyygl54hu5cefuxdhxp9h5mrg64rh07znktn9x9d5vg6fc0rw7m63x8rg4qk3kw6d8sycpywn48m"},
    };

    private static readonly Dictionary<int, string> bolt12Invoices = new()
    {
        {
            1200,
            "lno1zrxq8pjw7qjlm68mtp7e3yvxee4y5xrgjhhyf2fxhlphpckrvevh50u0qwumyhd9aa7p77jkp946nkphl2lutxa3e5zp8yx36pyycqas85txgqsre4qsrhyu2jqk5svgnwe5tng78r24dlwwglluetkdv4a5ppc3wanqqvlfkaqp5hhc6jl8eq0mau6wsdxevary7e0e3rpmma7plggygs7fr4e6dj8vflurnt7ajhgwxfu9hmqmf48wqd6tzuxmwdcgk9p6wspfqer0xj883lysflutn8qvudzakypdv8a7kqqsv0vcrt5w208yr5uzregj7whghy"
        },
    };
    private static ICounter counter = new InMemoryCounter();
    
    
    [Fact]
    public async Task FetchesInfoSuccessfully()
    {
        var wallet = Wallet.Create().WithMint(MintUrl);
        var info = await wallet.GetInfo();
        Assert.NotNull(info);
    }

    [Fact]
    public async Task MintsBolt11Successfully()
    {
        var wallet = Wallet.Create().WithMint(MintUrl);

        var mintQuote = await wallet
            .CreateMintQuote()
            .WithUnit("sat")
            .WithAmount(1337)
            .ProcessAsyncBolt11();
        
        Assert.NotNull(mintQuote);
        
        var paymentRequest = mintQuote.GetQuote().Request;
        Assert.Contains("lnbc1337", paymentRequest);
        
        await PayInvoice();

        var mintResponse = await mintQuote.Mint();
        Assert.NotNull(mintResponse);
        Assert.Equal(1337UL, Utils.SumProofs(mintResponse));
    }

    [Fact]
    public async Task MintsBolt12Successfully()
    {
        var wallet = Wallet.Create().WithMint(MintUrl);
        var privkey = new PrivKey(RandomNumberGenerator.GetHexString(64));
        
        var mintQuote = await wallet
            .CreateMintQuote()
            .WithPubkey(privkey.Key.CreatePubKey())
            .WithUnit("sat")
            .WithAmount(1337)
            .ProcessAsyncBolt12();
        
        Assert.NotNull(mintQuote);
        
        var paymentRequest = mintQuote.GetQuote().Request;
        Assert.NotNull(paymentRequest);
        mintQuote.SignWithPrivkey(privkey);
        
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
        
        var paymentRequest = mintQuote.GetQuote().Request;
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
        var phreshCounter = new InMemoryCounter();
        
        var wallet = Wallet
            .Create()
            .WithCounter(phreshCounter)
            .WithMint(MintUrl)
            .WithMnemonic(seed);
        
        var restoredProofs = await wallet
            .Restore()
            .ProcessAsync();
        
         var keys = (await wallet.GetKeys()).First().Keys;
         var expectedAmount = Utils.SplitToProofsAmounts(1336UL, keys).Count; // (one for fee)
         var keysets = await wallet.GetKeysets();

         foreach (var keyset in keysets)
         {
             // new counter will be bumped to newest state
            Assert.Equal(await counter.GetCounterForId(keyset.Id) + expectedAmount, await phreshCounter.GetCounterForId(keyset.Id));
         }
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
    public async Task MeltsBolt11Successfully()
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

         // create melt quote
         var meltQuote = await wallet
             .CreateMeltQuote()
             .WithInvoice(valuesInvoices[999])
             .WithUnit("sat")
             .ProcessAsyncBolt11();

         // select proofs to send 
         var q = meltQuote.GetQuote();
         var selectedProofs = await wallet.SelectProofsToSend(mintedProofs, q.Amount + (ulong)q.FeeReserve, true);
         
         //melt proofs 
         var change = await meltQuote.Melt(selectedProofs.Send);
         
         Assert.NotEmpty(change);
     }

     [Fact]
     public async Task MeltsBolt12Successfully()
     {
         var privkeyBob = new PrivKey(RandomNumberGenerator.GetBytes(32));
         
         // mint proofs
         var wallet = Wallet
             .Create()
             .WithMint(MintUrl);
     
         var mintQuote = await wallet
             .CreateMintQuote()
             .WithUnit("sat")
             .WithAmount(1337)
             .WithPubkey(privkeyBob.Key.CreatePubKey())
             .ProcessAsyncBolt12();
         
         await Task.Delay(3000);
         
         mintQuote.SignWithPrivkey(privkeyBob);
         var mintedProofs = await mintQuote.Mint();
         Assert.NotEmpty(mintedProofs);
     
         var Ids = mintedProofs.Select(proof => proof.Id).Count();
         
         Console.WriteLine($"amounts {Ids}");
         // create melt quote
         var meltQuote = await wallet
             .CreateMeltQuote()
             .WithInvoice(bolt12Invoices[1200])
             .WithUnit("sat")
             .WithAmount(1200)// it turns out that this invoice is amountless
             .ProcessAsyncBolt12();
     
         // select proofs to send 
         var q = meltQuote.GetQuote();
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
         
         var quote = meltHandler.GetQuote();
         
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
         
         await Assert.ThrowsAsync<CashuProtocolException>(
             async () => await wallet
                 .Swap()
                 .FromInputs(proofs)
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
             .WithPrivKeys([privKeyBob, privKeyAlice])
             .ProcessAsyncBolt11();

         var q = handler.GetQuote();
         
         var amountToPay = q.Amount + (ulong)q.FeeReserve;
         var selectorResponse = await wallet.SelectProofsToSend(proofs, amountToPay, true);
         var change = await handler.Melt(selectorResponse.Send);
         
         Assert.NotEmpty(change);
     }

     [Fact]
     public async Task MintSwapP2PkSigAll()
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
                     SigFlag = "SIG_ALL",
                     Pubkeys = [privKeyBob.Key.CreatePubKey()],
                     SignatureThreshold = 1
                 }
             ).ProcessAsyncBolt11();

         await PayInvoice();
         var proofs = await mintHandler.Mint();
         
         await Assert.ThrowsAsync<CashuProtocolException>(
             async () => await wallet
                 .Swap()
                 .FromInputs(proofs)
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
     public async Task MintSwapP2Bk()
     {
         var wallet = Wallet
             .Create()
             .WithMint(MintUrl);
         
         var privKeyBob = new PrivKey(RandomNumberGenerator.GetHexString(64, true));
         var privKeyAlice = new PrivKey(RandomNumberGenerator.GetHexString(64, true));

         var builder = new P2PkBuilder()
         {
             Pubkeys = [privKeyBob.Key.CreatePubKey(), privKeyAlice.Key.CreatePubKey()],
         };
         
         var quote = await wallet
             .CreateMintQuote()
             .WithAmount(1337)
             .WithP2PkLock(builder)
             .BlindPubkeys()
             .ProcessAsyncBolt11();
         
         await PayInvoice();
         var proofs = await quote.Mint();
         
         Assert.NotEmpty(proofs);
         Assert.NotEmpty(proofs.Select(p=>p.P2PkE));

         var newProofs = await wallet
             .Swap()
             .FromInputs(proofs)
             .WithPrivkeys([privKeyBob, privKeyAlice])
             .ProcessAsync();
         
         Assert.NotEmpty(newProofs);
     }
     
     [Fact]
     public async Task MintMeltP2Bk()
     {
         var wallet = Wallet
             .Create()
             .WithMint(MintUrl);
         
         var privKeyBob = new PrivKey(RandomNumberGenerator.GetHexString(64, true));

         var builder = new P2PkBuilder()
         {
             Pubkeys = [privKeyBob.Key.CreatePubKey()],
         };
         
         var quote = await wallet
             .CreateMintQuote()
             .WithAmount(1337)
             .WithP2PkLock(builder)
             .BlindPubkeys()
             .ProcessAsyncBolt11();
         
         await PayInvoice();
         var proofs = await quote.Mint();
         
         Assert.NotEmpty(proofs);
         Assert.NotEmpty(proofs.Select(p=>p.P2PkE));
         
         var meltHandler = await wallet
             .CreateMeltQuote()
             .WithInvoice(valuesInvoices[502])
             .WithPrivKeys([privKeyBob])
             .ProcessAsyncBolt11();
         
         var change = await meltHandler.Melt(proofs);
        
         Assert.NotEmpty(change);
     }

     [Fact]
     public async Task MintMeltP2BkSigAll()
     {
         var wallet = Wallet
             .Create()
             .WithMint(MintUrl);
         
         var privKeyBob = new PrivKey(RandomNumberGenerator.GetHexString(64, true));

         var builder = new P2PkBuilder()
         {
             Pubkeys = [privKeyBob.Key.CreatePubKey()],
             SigFlag = "SIG_ALL",
         };
         
         var quote = await wallet
             .CreateMintQuote()
             .WithAmount(1337)
             .WithP2PkLock(builder)
             .BlindPubkeys()
             .ProcessAsyncBolt11();
         
         await PayInvoice();
         var proofs = await quote.Mint();
         
         Assert.NotEmpty(proofs);
         Assert.NotEmpty(proofs.Select(p=>p.P2PkE));
         
         var meltHandler = await wallet
             .CreateMeltQuote()
             .WithInvoice(valuesInvoices[503])
             .WithPrivKeys([privKeyBob])
             .ProcessAsyncBolt11();
         
         var change = await meltHandler.Melt(proofs);
        
         Assert.NotEmpty(change);
     }

     [Fact]
     public async Task MintSwapP2BkSigAll()
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
                     SigFlag = "SIG_ALL",
                     Pubkeys = [privKeyBob.Key.CreatePubKey()],
                     SignatureThreshold = 1
                 }
             )
             .BlindPubkeys()
             .ProcessAsyncBolt11();

         await PayInvoice();
         var proofs = await mintHandler.Mint();
         
         await Assert.ThrowsAsync<CashuProtocolException>(
             async () => await wallet
                 .Swap()
                 .FromInputs(proofs)
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
     public async Task MintSwapHTLC()
     {
         var wallet = Wallet
             .Create()
             .WithMint(MintUrl);
         
         var privKeyBob = new PrivKey(RandomNumberGenerator.GetHexString(64, true));
         var preimage = "0000000000000000000000000000000000000000000000000000000000000001";
         var hashLock = Convert.ToHexString(SHA256.HashData(Convert.FromHexString(preimage)));
         
         var mintHandler = await wallet.CreateMintQuote()
             .WithAmount(1337)
             .WithHTLCLock(new HTLCBuilder()
             {
                 HashLock = hashLock,
                 Pubkeys = [privKeyBob.Key.CreatePubKey()],
                 SignatureThreshold = 1
             })
             .ProcessAsyncBolt11();
     
         await PayInvoice();
         var htlcProofs = await mintHandler.Mint();
         
         Assert.NotEmpty(htlcProofs);
         Assert.Equal(1337UL, Utils.SumProofs(htlcProofs));
         
         
         // try swap without preimage - should fail
         await Assert.ThrowsAsync<InvalidOperationException>(async () =>
         {
             await wallet.Swap()
                 .FromInputs(htlcProofs)
                 .WithPrivkeys([privKeyBob])
                 .ProcessAsync();
         });
         
         // swap with correct preimage and signature
         var swappedProofs = await wallet.Swap()
             .FromInputs(htlcProofs)
             .WithPrivkeys([privKeyBob])
             .WithHtlcPreimage(preimage)
             .ProcessAsync();
         
         Assert.NotEmpty(swappedProofs);
         // fee is 100 ppk - it can be calculated before but here we don't care
         Assert.Equal(1337UL - 1, Utils.SumProofs(swappedProofs));
     }

     [Fact]
     public async Task MintSwapHTLCSigAll()
     {
         var wallet = Wallet
             .Create()
             .WithMint(MintUrl);
         
         var privKeyBob = new PrivKey(RandomNumberGenerator.GetHexString(64, true));
         var preimage = "0000000000000000000000000000000000000000000000000000000000000001";
         var hashLock = Convert.ToHexString(SHA256.HashData(Convert.FromHexString(preimage)));
         
         var mintHandler = await wallet.CreateMintQuote()
             .WithAmount(1337)
             .WithHTLCLock(new HTLCBuilder()
             {
                 HashLock = hashLock,
                 Pubkeys = [privKeyBob.Key.CreatePubKey()],
                 SignatureThreshold = 1,
                 SigFlag = "SIG_ALL"
             })
             .ProcessAsyncBolt11();
     
         await PayInvoice();
         var htlcProofs = await mintHandler.Mint();
         
         Assert.NotEmpty(htlcProofs);
         Assert.Equal(1337UL, Utils.SumProofs(htlcProofs));
         
         await Assert.ThrowsAsync<InvalidOperationException>(async () =>
         {
             await wallet.Swap()
                 .FromInputs(htlcProofs)
                 .WithPrivkeys([privKeyBob])
                 .ProcessAsync();
         });
         
         var swappedProofs = await wallet.Swap()
             .FromInputs(htlcProofs)
             .WithPrivkeys([privKeyBob])
             .WithHtlcPreimage(preimage)
             .ProcessAsync();
         
         Assert.NotEmpty(swappedProofs);
         Assert.Equal(1337UL - 1, Utils.SumProofs(swappedProofs));
     }

     [Fact]
     public async Task MintSwapHtlcP2BkSigAll()
     {
         var wallet = Wallet
             .Create()
             .WithMint(MintUrl);
         
         var privKeyBob = new PrivKey(RandomNumberGenerator.GetHexString(64, true));
         var preimage = "0000000000000000000000000000000000000000000000000000000000000001";
         var hashLock = Convert.ToHexString(SHA256.HashData(Convert.FromHexString(preimage)));
         
         var mintHandler = await wallet.CreateMintQuote()
             .WithAmount(1337)
             .WithHTLCLock(new HTLCBuilder()
             {
                 HashLock = hashLock,
                 Pubkeys = [privKeyBob.Key.CreatePubKey()],
                 SignatureThreshold = 1,
                 SigFlag = "SIG_ALL"
             })
             .BlindPubkeys()
             .ProcessAsyncBolt11();
     
         await PayInvoice();
         var htlcProofs = await mintHandler.Mint();
         
         Assert.NotEmpty(htlcProofs);
         Assert.Equal(1337UL, Utils.SumProofs(htlcProofs));
         
         
         await Assert.ThrowsAsync<InvalidOperationException>(async () =>
         {
             await wallet.Swap()
                 .FromInputs(htlcProofs)
                 .WithPrivkeys([privKeyBob])
                 .ProcessAsync();
         });
         
         var swappedProofs = await wallet.Swap()
             .FromInputs(htlcProofs)
             .WithPrivkeys([privKeyBob])
             .WithHtlcPreimage(preimage)
             .ProcessAsync();
         
         Assert.NotEmpty(swappedProofs);
         Assert.Equal(1337UL - 1, Utils.SumProofs(swappedProofs));
     }
     
     [Fact]
     public async Task MintMeltHTLCP2Bk()
     {
         var wallet = Wallet
             .Create()
             .WithMint(MintUrl);
         
         var privKeyBob = new PrivKey(RandomNumberGenerator.GetHexString(64, true));
         var preimage = new string('0', 63) + "1";
         
         var builder = new HTLCBuilder()
         {
             Pubkeys = [privKeyBob.Key.CreatePubKey()],
             HashLock = Convert.ToHexString(SHA256.HashData(Convert.FromHexString(preimage))),
         };
         
         var quote = await wallet
             .CreateMintQuote()
             .WithAmount(1337)
             .WithHTLCLock(builder)
             .BlindPubkeys()
             .ProcessAsyncBolt11();
         
         await PayInvoice();
         var proofs = await quote.Mint();
         
         Assert.NotEmpty(proofs);
         Assert.NotEmpty(proofs.Select(p=>p.P2PkE));
         
         var meltHandler = await wallet
             .CreateMeltQuote()
             .WithInvoice(valuesInvoices[1150])
             .WithPrivKeys([privKeyBob])
             .WithHTLCPreimage(preimage)
             .ProcessAsyncBolt11();
         
         var change = await meltHandler.Melt(proofs);
        
         Assert.NotEmpty(change);
     }

     [Fact]
     public async Task MintMeltHTLCP2BkSigAll()
     {
         var wallet = Wallet
             .Create()
             .WithMint(MintUrl);
         
         var privKeyBob = new PrivKey(RandomNumberGenerator.GetHexString(64, true));
         var preimage = new string('0', 63) + "1";
         
         var builder = new HTLCBuilder()
         {
             SigFlag = "SIG_ALL",
             Pubkeys = [privKeyBob.Key.CreatePubKey()],
             HashLock = Convert.ToHexString(SHA256.HashData(Convert.FromHexString(preimage))),
         };
         
         var quote = await wallet
             .CreateMintQuote()
             .WithAmount(1337)
             .WithHTLCLock(builder)
             .BlindPubkeys()
             .ProcessAsyncBolt11();
         
         await PayInvoice();
         var proofs = await quote.Mint();
         
         Assert.NotEmpty(proofs);
         Assert.NotEmpty(proofs.Select(p=>p.P2PkE));
         
         var meltHandler = await wallet
             .CreateMeltQuote()
             .WithInvoice(valuesInvoices[1151])
             .WithPrivKeys([privKeyBob])
             .WithHTLCPreimage(preimage)
             .ProcessAsyncBolt11();
         
         var change = await meltHandler.Melt(proofs);
        
         Assert.NotEmpty(change);
     }
     

     
     [Fact]
     public async Task SwapWithCustomAmounts()
     {
         var wallet = Wallet
             .Create()
             .WithMint(MintUrl);
         
         // mint some proofs
         var mintQuote = await wallet
             .CreateMintQuote()
             .WithAmount(100)
             .WithUnit("sat")
             .ProcessAsyncBolt11();

         await PayInvoice();
         var mintedProofs = await mintQuote.Mint();
         Assert.NotEmpty(mintedProofs);
         
         // swap with specific amounts
         var desiredAmounts = new List<ulong> { 32, 32, 32, 2, 1 }; // 96 sat (should consume 1 for fees)
         var newProofs = await wallet
             .Swap()
             .FromInputs(mintedProofs)
             .WithAmounts(desiredAmounts)
             .ProcessAsync();
         
         Assert.NotEmpty(newProofs);
         // amount should be at least the requested amounts
         Assert.True(Utils.SumProofs(newProofs) >= 96);
     }
     
     [Fact]
     public async Task SwapToSpecificKeyset()
     {
         var wallet = Wallet
             .Create()
             .WithMint(MintUrl);
         
         // get active keyset
         var activeKeysetId = await wallet.GetActiveKeysetId("sat");
         Assert.NotNull(activeKeysetId);
         
         // mint some proofs
         var mintQuote = await wallet
             .CreateMintQuote()
             .WithAmount(64)
             .WithUnit("sat")
             .ProcessAsyncBolt11();

         await PayInvoice();
         var mintedProofs = await mintQuote.Mint();
         Assert.NotEmpty(mintedProofs);
         
         // swap to specific keyset
         var newProofs = await wallet
             .Swap()
             .FromInputs(mintedProofs)
             .ForKeyset(activeKeysetId)
             .ProcessAsync();
         
         Assert.NotEmpty(newProofs);
         Assert.All(newProofs, p => Assert.Equal(activeKeysetId, p.Id));
     }
     
     [Fact]
     public async Task MeltWithInsufficientFunds()
     {
         var wallet = Wallet
             .Create()
             .WithMint(MintUrl);
         
         // mint small amount
         var mintQuote = await wallet
             .CreateMintQuote()
             .WithAmount(10)
             .WithUnit("sat")
             .ProcessAsyncBolt11();

         await PayInvoice();
         var mintedProofs = await mintQuote.Mint();
         Assert.NotEmpty(mintedProofs);
         
         // try to melt for larger invoice - should fail during proof selection
         var meltHandler = await wallet
             .CreateMeltQuote()
             .WithInvoice(valuesInvoices[1000]) // 1000 sat invoice
             .WithUnit("sat")
             .ProcessAsyncBolt11();
         
         var quote = meltHandler.GetQuote();
         var amountNeeded = quote.Amount + (ulong)quote.FeeReserve;
         
         // selectProofsToSend should return empty Send list when insufficient
         var selection = await wallet.SelectProofsToSend(mintedProofs, amountNeeded, true);
         Assert.Empty(selection.Send);
         Assert.NotEmpty(selection.Keep);
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

        var quote = mintHandler.GetQuote();

        var sub = await service.SubscribeToMintQuoteAsync(MintUrl, new[] { quote.Quote });

        int connectedCount = 0;
        int notificationCount = 0;

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(240));

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


