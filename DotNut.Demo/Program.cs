using DotNut.Api;
using DotNut.ApiModels;
using NBitcoin.Secp256k1;
using System.Security.Cryptography;
using DotNut.NBitcoin.BIP39;
using DotNut.NUT13;

namespace DotNut.Demo;

class Program
{
    private static readonly string DefaultMintUrl = "https://testnut.cashu.space";
    private static CashuHttpClient? _client;
    private static List<Proof> _wallet = new();
    
    static async Task Main(string[] args)
    {
        Console.WriteLine("🥜 DotNut - Cashu Library Demo");
        Console.WriteLine("==============================");
        Console.WriteLine();
        
        await InitializeMint();
        
        while (true)
        {
            ShowMenu();
            var choice = Console.ReadLine();
            
            try
            {
                switch (choice?.ToLower())
                {
                    case "1":
                        await ConnectToMintDemo();
                        break;
                    case "2":
                        await TokenCreationDemo();
                        break;
                    case "3":
                        await TokenEncodingDemo();
                        break;
                    case "4":
                        await LightningMintDemo();
                        break;
                    case "5":
                        await LightningMeltDemo();
                        break;
                    case "6":
                        await TokenSwapDemo();
                        break;
                    case "7":
                        await SecretsDemo();
                        break;
                    case "8":
                        await MnemonicDemo();
                        break;
                    case "9":
                        await P2PKDemo();
                        break;
                    case "10":
                        ShowWallet();
                        break;
                    case "11":
                        await CheckProofStatesDemo();
                        break;
                    case "q":
                    case "quit":
                        Console.WriteLine("Goodbye! 👋");
                        return;
                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                if (ex is CashuProtocolException cashuEx)
                {
                    Console.WriteLine($"   Code: {cashuEx.Error.Code}");
                    Console.WriteLine($"   Detail: {cashuEx.Error.Detail}");
                }
            }
            
            Console.WriteLine("\nPress any key to continue...");
            Console.Read();
            Console.Clear();
        }
    }
    
    private static void ShowMenu()
    {
        Console.WriteLine("📋 Available Demos:");
        Console.WriteLine(" 1. Connect to Mint & Get Info");
        Console.WriteLine(" 2. Create Cashu Token");
        Console.WriteLine(" 3. Token Encoding/Decoding");
        Console.WriteLine(" 4. Lightning Mint Quote (Demo)");
        Console.WriteLine(" 5. Lightning Melt Quote (Demo)");
        Console.WriteLine(" 6. Token Swapping (Demo)");
        Console.WriteLine(" 7. Working with Secrets");
        Console.WriteLine(" 8. Mnemonic Secrets (NUT-13)");
        Console.WriteLine(" 9. P2PK Secrets (NUT-11)");
        Console.WriteLine("10. Show Current Wallet");
        Console.WriteLine("11. Check Proof States");
        Console.WriteLine(" Q. Quit");
        Console.WriteLine();
        Console.Write("Choose an option: ");
    }
    
    private static async Task InitializeMint()
    {
        try
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(DefaultMintUrl);
            _client = new CashuHttpClient(httpClient);
            
            Console.WriteLine($"🔗 Initialized connection to: {DefaultMintUrl}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to initialize mint connection: {ex.Message}");
        }
    }
    
    private static async Task ConnectToMintDemo()
    {
        Console.WriteLine("🔗 Connect to Mint & Get Info Demo");
        Console.WriteLine("==================================");
        
        if (_client == null)
        {
            Console.WriteLine("❌ Client not initialized");
            return;
        }
        
        try
        {
            // Get mint information
            var info = await _client.GetInfo();
            Console.WriteLine($"✅ Connected to mint: {info.Name}");
            Console.WriteLine($"   Description: {info.Description}");
            Console.WriteLine($"   Version: {info.Version}");
            Console.WriteLine($"   Contact: {string.Join(", ", info.Contact?.Select(c => $"{c.Method}: {c.Info}") ?? new[] { "N/A" })}");
            
            // Get available keysets
            var keysets = await _client.GetKeysets();
            Console.WriteLine($"   Available keysets: {keysets.Keysets.Length}");
            
            foreach (var keyset in keysets.Keysets.Take(3))
            {
                Console.WriteLine($"     - {keyset.Id} ({keyset.Unit}) [{keyset.Active}]");
            }
            
            // Get keys for the first active keyset
            var activeKeyset = keysets.Keysets.FirstOrDefault(k => k.Active);
            if (activeKeyset != null)
            {
                var keys = await _client.GetKeys(activeKeyset.Id);
                Console.WriteLine($"   Keys in active keyset ({activeKeyset.Id}):");
                
                foreach (var key in keys.Keysets.First().Keys.Take(5))
                {
                    Console.WriteLine($"     - Amount {key.Key}: {key.Value}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to connect to mint: {ex.Message}");
        }
    }
    
    private static async Task TokenCreationDemo()
    {
        Console.WriteLine("🪙 Token Creation Demo");
        Console.WriteLine("======================");
        
        // Create some example proofs for demonstration
        var proofs = CreateExampleProofs();
        _wallet.AddRange(proofs);
        
        // Create a token
        var token = new CashuToken
        {
            Unit = "sat",
            Memo = "Demo payment - Coffee ☕",
            Tokens = new List<CashuToken.Token>
            {
                new CashuToken.Token
                {
                    Mint = DefaultMintUrl,
                    Proofs = proofs
                }
            }
        };
        
        Console.WriteLine($"✅ Created token with {proofs.Count} proofs");
        Console.WriteLine($"   Total amount: {token.TotalAmount()} sats");
        Console.WriteLine($"   Memo: {token.Memo}");
        Console.WriteLine($"   Mint: {token.Tokens.First().Mint}");
        
        // Show proof details
        Console.WriteLine("   Proofs:");
        foreach (var proof in proofs)
        {
            Console.WriteLine($"     - {proof.Amount} sats (ID: {proof.Id})");
        }
    }
    
    private static async Task TokenEncodingDemo()
    {
        Console.WriteLine("🔄 Token Encoding/Decoding Demo");
        Console.WriteLine("===============================");
        
        var proofs = CreateExampleProofs();
        var token = new CashuToken
        {
            Unit = "sat",
            Memo = "Encoding demo token",
            Tokens = new List<CashuToken.Token>
            {
                new CashuToken.Token
                {
                    Mint = DefaultMintUrl,
                    Proofs = proofs
                }
            }
        };
        
        // V3 Encoding (JSON-based)
        Console.WriteLine("📝 V3 Encoding (JSON-based):");
        var v3Token = token.Encode("A");
        Console.WriteLine($"   Length: {v3Token.Length} characters");
        Console.WriteLine($"   Token: {v3Token.Substring(0, Math.Min(80, v3Token.Length))}...");
        
        // V4 Encoding (CBOR-based, more compact)
        Console.WriteLine("\n📦 V4 Encoding (CBOR-based, compact):");
        var v4Token = token.Encode("B");
        Console.WriteLine($"   Length: {v4Token.Length} characters");
        Console.WriteLine($"   Token: {v4Token.Substring(0, Math.Min(80, v4Token.Length))}...");
        
        // URI format
        Console.WriteLine("\n🔗 URI Format:");
        var uriToken = token.Encode("B", makeUri: true);
        Console.WriteLine($"   URI: {uriToken.Substring(0, Math.Min(80, uriToken.Length))}...");
        
        // Decode and verify
        Console.WriteLine("\n🔍 Decoding V4 token:");
        var decoded = CashuTokenHelper.Decode(v4Token, out string version);
        Console.WriteLine($"   Version: {version}");
        Console.WriteLine($"   Amount: {decoded.TotalAmount()} sats");
        Console.WriteLine($"   Memo: {decoded.Memo}");
        Console.WriteLine($"   Proofs: {decoded.Tokens.First().Proofs.Count}");
        
        Console.WriteLine($"\n💾 Space savings: V4 is {((double)(v3Token.Length - v4Token.Length) / v3Token.Length * 100):F1}% smaller than V3");
    }
    
    private static async Task LightningMintDemo()
    {
        Console.WriteLine("⚡ Lightning Mint Quote Demo");
        Console.WriteLine("============================");
        Console.WriteLine("ℹ️  This demo shows how to create mint quotes - actual minting requires paying a real Lightning invoice");
        
        if (_client == null)
        {
            Console.WriteLine("❌ Client not initialized");
            return;
        }
        
        try
        {
            // Create mint quote
            var mintRequest = new PostMintQuoteBolt11Request
            {
                Amount = 1000, // 1000 sats
                Unit = "sat"
            };
            
            var mintQuote = await _client.CreateMintQuote<PostMintQuoteBolt11Response, PostMintQuoteBolt11Request>(
                "bolt11", mintRequest);
            
            Console.WriteLine("✅ Mint quote created successfully!");
            Console.WriteLine($"   Quote ID: {mintQuote.Quote}");
            Console.WriteLine($"   Amount: {mintQuote.Amount} {mintRequest.Unit}");
            Console.WriteLine($"   Unit: {mintQuote.Unit ?? mintRequest.Unit}");
            Console.WriteLine($"   Expiry: {DateTimeOffset.FromUnixTimeSeconds(mintQuote.Expiry ?? 0).UtcDateTime}");
            Console.WriteLine($"   State: {mintQuote.State}");
            Console.WriteLine("\n📄 Lightning Invoice:");
            Console.WriteLine($"   {mintQuote.Request}");
            
            Console.WriteLine("\n💡 To complete minting:");
            Console.WriteLine("   1. Pay the Lightning invoice above");
            Console.WriteLine("   2. Create blinded messages for desired denominations");
            Console.WriteLine("   3. Call the mint endpoint with the quote ID and blinded messages");
            Console.WriteLine("   4. Unblind the returned signatures to get your proofs");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to create mint quote: {ex.Message}");
        }
    }
    
    private static async Task LightningMeltDemo()
    {
        Console.WriteLine("⚡ Lightning Melt Quote Demo");
        Console.WriteLine("============================");
        Console.WriteLine("ℹ️  This demo shows how to create melt quotes - actual melting requires valid proofs");
        
        if (_client == null)
        {
            Console.WriteLine("❌ Client not initialized");
            return;
        }
        
        // Example Lightning invoice (fake for demo purposes)
        var exampleInvoice = "lnbc10n1pj9x8x8pp5k6k6k6k6k6k6k6k6k6k6k6k6k6k6k6k6k6k6k6k6k6k6k6k6k6qdqqcqzpgxqyz5vqsp5example";
        
        try
        {
            var meltRequest = new PostMeltQuoteBolt11Request
            {
                Request = exampleInvoice,
                Unit = "sat"
            };
            
            // Note: This will likely fail with the example invoice, but shows the API usage
            Console.WriteLine("📤 Attempting to create melt quote...");
            Console.WriteLine($"   Invoice: {exampleInvoice.Substring(0, 50)}...");
            
            var meltQuote = await _client.CreateMeltQuote<PostMeltQuoteBolt11Response, PostMeltQuoteBolt11Request>(
                "bolt11", meltRequest);
            
            Console.WriteLine("✅ Melt quote created successfully!");
            Console.WriteLine($"   Quote ID: {meltQuote.Quote}");
            Console.WriteLine($"   Amount: {meltQuote.Amount} {meltRequest.Unit}");
            Console.WriteLine($"   Fee reserve: {meltQuote.FeeReserve} {meltRequest.Unit}");
            Console.WriteLine($"   Expiry: {DateTimeOffset.FromUnixTimeSeconds(meltQuote.Expiry ?? 0).UtcDateTime}");
            Console.WriteLine($"   State: {meltQuote.State}");
            
            Console.WriteLine("\n💡 To complete melting:");
            Console.WriteLine("   1. Provide proofs with sufficient value (amount + fee)");
            Console.WriteLine("   2. Call the melt endpoint with quote ID and proofs");
            Console.WriteLine("   3. The Lightning invoice will be paid automatically");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Expected error with demo invoice: {ex.Message}");
            Console.WriteLine("\n💡 To test melting:");
            Console.WriteLine("   1. Generate a real Lightning invoice from your wallet");
            Console.WriteLine("   2. Use that invoice instead of the demo one");
            Console.WriteLine("   3. Ensure you have sufficient proofs in your wallet");
        }
    }
    
    private static async Task TokenSwapDemo()
    {
        Console.WriteLine("🔄 Token Swapping Demo");
        Console.WriteLine("======================");
        Console.WriteLine("ℹ️  Swapping allows you to change token denominations or refresh secrets");
        
        if (_wallet.Count == 0)
        {
            Console.WriteLine("⚠️  No proofs in wallet. Creating example proofs for demo...");
            _wallet.AddRange(CreateExampleProofs());
        }
        
        var inputProofs = _wallet.Take(2).ToList();
        Console.WriteLine($"📥 Input proofs: {inputProofs.Count} proofs totaling {inputProofs.Sum(p => (long)p.Amount)} sats");
        
        foreach (var proof in inputProofs)
        {
            Console.WriteLine($"   - {proof.Amount} sats (Secret: {proof.Secret})");
        }
        
        // In a real implementation, you would:
        // 1. Create blinded messages for new denominations
        // 2. Send swap request to mint
        // 3. Unblind the returned signatures
        
        Console.WriteLine("\n💡 Swap process would involve:");
        Console.WriteLine("   1. Creating blinded messages for desired output amounts");
        Console.WriteLine("   2. Sending PostSwapRequest with input proofs and output blinded messages");
        Console.WriteLine("   3. Receiving BlindSignatures from the mint");
        Console.WriteLine("   4. Unblinding signatures to get new proofs with fresh secrets");
        Console.WriteLine("   5. The old proofs become invalid, new proofs are added to wallet");
    }
    
    private static async Task SecretsDemo()
    {
        Console.WriteLine("🔐 Working with Secrets Demo");
        Console.WriteLine("============================");
        
        // Simple string secret
        Console.WriteLine("1️⃣ Simple String Secret:");
        var stringSecret = new StringSecret("my-random-secret-12345");
        Console.WriteLine($"   Secret: {stringSecret}");
        Console.WriteLine($"   Curve point: {stringSecret.ToCurve().ToHex()}");
        
        // Random secret generation
        Console.WriteLine("\n2️⃣ Random Secret Generation:");
        var randomBytes = new byte[32];
        RandomNumberGenerator.Fill(randomBytes);
        var randomSecret = new StringSecret(Convert.ToHexString(randomBytes).ToLower());
        Console.WriteLine($"   Random secret: {randomSecret}");
        
        // Demonstrate secret uniqueness
        Console.WriteLine("\n3️⃣ Secret Uniqueness:");
        var secret1 = new StringSecret("test-secret-1");
        var secret2 = new StringSecret("test-secret-2");
        Console.WriteLine($"   Secret 1 → Curve: {secret1.ToCurve().ToHex()}");
        Console.WriteLine($"   Secret 2 → Curve: {secret2.ToCurve().ToHex()}");
        Console.WriteLine($"   Different secrets produce different curve points ✅");
        
        Console.WriteLine("\n💡 Key points about secrets:");
        Console.WriteLine("   - Secrets are hashed to elliptic curve points");
        Console.WriteLine("   - Each secret maps to a unique point on the curve");
        Console.WriteLine("   - Changing even one character creates a completely different point");
        Console.WriteLine("   - Secrets should be random and unpredictable");
    }
    
    private static async Task MnemonicDemo()
    {
        Console.WriteLine("🎲 Mnemonic Secrets Demo (NUT-13)");
        Console.WriteLine("==================================");
        
        // Create a mnemonic
        var mnemonic = new Mnemonic(Wordlist.English, WordCount.Twelve);
        Console.WriteLine($"📝 Generated mnemonic:");
        Console.WriteLine($"   {mnemonic}");
        
        // Example keyset ID (normally you'd get this from the mint)
        var keysetId = new KeysetId("009a1f293253e41e");
        
        Console.WriteLine($"\n🔑 Deriving secrets from mnemonic:");
        Console.WriteLine($"   Keyset ID: {keysetId}");
        
        // Derive multiple secrets
        for (uint i = 0; i < 5; i++)
        {
            var secret = mnemonic.DeriveSecret(keysetId, counter: i);
            var blindingFactor = mnemonic.DeriveBlindingFactor(keysetId, counter: i);
            
            Console.WriteLine($"   Counter {i}:");
            Console.WriteLine($"     Secret: {secret}");
            Console.WriteLine($"     Blinding: {Convert.ToHexString(blindingFactor).ToLower()}");
        }
        
        Console.WriteLine("\n💡 Benefits of deterministic secrets:");
        Console.WriteLine("   - Reproducible from mnemonic phrase");
        Console.WriteLine("   - No need to store individual secrets");
        Console.WriteLine("   - Can recover proofs if you lose wallet data");
        Console.WriteLine("   - Counter ensures each secret is unique");
        
        Console.WriteLine("\n⚠️  Security considerations:");
        Console.WriteLine("   - Keep your mnemonic phrase secure");
        Console.WriteLine("   - Anyone with the mnemonic can recreate your secrets");
        Console.WriteLine("   - Use proper entropy when generating mnemonics");
    }
    
    private static async Task P2PKDemo()
    {
        Console.WriteLine("🔒 Pay-to-Public-Key Demo (NUT-11)");
        Console.WriteLine("===================================");
        
        // Create some public keys for the demo
        var privKey1 = ECPrivKey.Create(RandomNumberGenerator.GetBytes(32));
        var privKey2 = ECPrivKey.Create(RandomNumberGenerator.GetBytes(32));
        var pubKey1 = privKey1.CreatePubKey();
        var pubKey2 = privKey2.CreatePubKey();
        
        Console.WriteLine("🔑 Generated demo keys:");
        Console.WriteLine($"   PubKey 1: {pubKey1}");
        Console.WriteLine($"   PubKey 2: {pubKey2}");
        
        // Create a 1-of-2 multisig P2PK secret
        Console.WriteLine("\n🏗️  Creating 1-of-2 multisig P2PK:");
        var p2pkBuilder = new P2PKBuilder
        {
            Pubkeys = new[] { pubKey1, pubKey2 },
            SignatureThreshold = 1, // 1-of-2 multisig
            SigFlag = "SIG_INPUTS"
        };
        
        var p2pkSecret = p2pkBuilder.Build();
        var nut10Secret = new Nut10Secret(P2PKProofSecret.Key, p2pkSecret);
        
        Console.WriteLine($"   Signature threshold: {p2pkBuilder.SignatureThreshold}-of-{p2pkBuilder.Pubkeys.Length}");
        Console.WriteLine($"   Signature flag: {p2pkBuilder.SigFlag}");
        Console.WriteLine($"   P2PK secret created ✅");
        
        // Create a time-locked P2PK
        Console.WriteLine("\n⏰ Creating time-locked P2PK:");
        var timeLockedBuilder = new P2PKBuilder
        {
            Pubkeys = new[] { pubKey1 },
            SignatureThreshold = 1,
            SigFlag = "SIG_INPUTS",
            Lock = DateTimeOffset.UtcNow.AddHours(1), // Lock for 1 hour
            RefundPubkeys = new[] { pubKey2 } // Refund key after timeout
        };
        
        var timeLockedSecret = timeLockedBuilder.Build();
        var timeLockedNut10 = new Nut10Secret(P2PKProofSecret.Key, timeLockedSecret);
        
        Console.WriteLine($"   Lock time: {timeLockedBuilder.Lock}");
        Console.WriteLine($"   Refund key: {pubKey2}");
        Console.WriteLine($"   Time-locked P2PK secret created ✅");
        
        Console.WriteLine("\n💡 P2PK use cases:");
        Console.WriteLine("   - Multisignature wallets");
        Console.WriteLine("   - Escrow services");
        Console.WriteLine("   - Time-locked payments");
        Console.WriteLine("   - Conditional spending");
        
        Console.WriteLine("\n🔓 To spend P2PK proofs:");
        Console.WriteLine("   - Create signatures with required private keys");
        Console.WriteLine("   - Include witness data in the proof");
        Console.WriteLine("   - Mint validates signatures against public keys");
    }
    
    private static async Task CheckProofStatesDemo()
    {
        Console.WriteLine("🔍 Check Proof States Demo");
        Console.WriteLine("==========================");
        
        if (_client == null)
        {
            Console.WriteLine("❌ Client not initialized");
            return;
        }
        
        if (_wallet.Count == 0)
        {
            Console.WriteLine("⚠️  No proofs in wallet. Creating example proofs...");
            _wallet.AddRange(CreateExampleProofs());
        }
        
        try
        {
            var proofsToCheck = _wallet.Take(3).ToList();
            Console.WriteLine($"📋 Checking state of {proofsToCheck.Count} proofs...");
            
            // Create check state request
            var stateRequest = new PostCheckStateRequest
            {
                Ys = proofsToCheck.Select(p => p.C.ToString()).ToArray()
            };
            
            // Note: This will likely fail with fake proofs, but shows the API usage
            var stateResponse = await _client.CheckState(stateRequest);
            
            Console.WriteLine("✅ State check successful:");
            for (int i = 0; i < stateResponse.States.Length; i++)
            {
                var state = stateResponse.States[i];
                var proof = proofsToCheck[i];
                Console.WriteLine($"   Proof {i + 1} ({proof.Amount} sats): {state.State}");
                if (!string.IsNullOrEmpty(state.Witness))
                {
                    Console.WriteLine($"     Witness: {state.Witness}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Expected error with demo proofs: {ex.Message}");
            Console.WriteLine("\n💡 Proof states in real usage:");
            Console.WriteLine("   - UNSPENT: Proof is valid and can be spent");
            Console.WriteLine("   - SPENT: Proof has already been used");
            Console.WriteLine("   - RESERVED: Proof is temporarily locked");
            Console.WriteLine("   - Check states before attempting to spend proofs");
        }
    }
    
    private static void ShowWallet()
    {
        Console.WriteLine("💰 Current Wallet");
        Console.WriteLine("=================");
        
        if (_wallet.Count == 0)
        {
            Console.WriteLine("   Empty wallet - no proofs stored");
            return;
        }
        
        var totalAmount = _wallet.Sum(p => (long)p.Amount);
        Console.WriteLine($"   Total balance: {totalAmount} sats");
        Console.WriteLine($"   Number of proofs: {_wallet.Count}");
        Console.WriteLine();
        
        Console.WriteLine("   Proof details:");
        foreach (var proof in _wallet.Take(10)) // Show first 10 proofs
        {
            Console.WriteLine($"     - {proof.Amount,4} sats | ID: {proof.Id} | Secret: {proof.Secret.ToString().Substring(0, Math.Min(20, proof.Secret.ToString().Length))}...");
        }
        
        if (_wallet.Count > 10)
        {
            Console.WriteLine($"     ... and {_wallet.Count - 10} more proofs");
        }
        
        // Show denomination breakdown
        var denominations = _wallet.GroupBy(p => p.Amount).OrderBy(g => g.Key);
        Console.WriteLine("\n   Denomination breakdown:");
        foreach (var denom in denominations)
        {
            Console.WriteLine($"     {denom.Key,4} sats: {denom.Count()} proofs = {denom.Key * (ulong)denom.Count()} sats");
        }
    }
    
    private static List<Proof> CreateExampleProofs()
    {
        // Create example proofs for demonstration
        // In a real application, these would come from minting operations
        var keysetId = new KeysetId("009a1f293253e41e");
        
        var proofs = new List<Proof>();
        var amounts = new ulong[] { 1, 2, 4, 8, 16, 32, 64, 128, 256, 512 };
        
        foreach (var amount in amounts.Take(5)) // Create 5 demo proofs
        {
            var secret = new StringSecret($"demo-secret-{amount}-{Guid.NewGuid()}");
            var privKey = ECPrivKey.Create(RandomNumberGenerator.GetBytes(32));
            var pubKey = privKey.CreatePubKey();
            
            var proof = new Proof
            {
                Amount = amount,
                Id = keysetId,
                Secret = secret,
                C = pubKey,
                // Note: In real usage, these would be proper cryptographic proofs from the mint
            };
            
            proofs.Add(proof);
        }
        
        return proofs;
    }
}

// Extension method to calculate total amount
public static class CashuTokenExtensions
{
    public static ulong TotalAmount(this CashuToken token)
    {
        return (ulong)token.Tokens.SelectMany(t => t.Proofs).Sum(p => (long)p.Amount);
    }
}