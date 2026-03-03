# DotNut

C# library for the [Cashu protocol](https://cashu.space) - a Chaumian e-cash system built on Bitcoin/Lightning.

[![NuGet](https://img.shields.io/nuget/v/DotNut.svg)](https://www.nuget.org/packages/DotNut/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## Installation

```bash
dotnet add package DotNut
```

## Usage

The main entry point is the `Wallet` class, which exposes a fluent builder for connecting to a mint and performing operations.

### Setup

```csharp
var wallet = Wallet.Create()
    .WithMint("https://testnut.cashu.space")
    .WithMnemonic("your twelve word mnemonic phrase here...")
    .WithCounter(new InMemoryCounter());
```

### Mint (Lightning &rarr; tokens)

```csharp
var mintHandler = await wallet
    .CreateMintQuote()
    .WithAmount(1000)
    .WithUnit("sat")
    .ProcessAsyncBolt11();

// Pay the Lightning invoice
Console.WriteLine(mintHandler.GetQuote().Request);

// After payment, mint the tokens
List<Proof> proofs = await mintHandler.Mint();
```

### Swap (rebalance / receive token)

```csharp
// From a cashuB... token string
List<Proof> proofs = await wallet
    .Swap()
    .WithDLEQVerification()
    .ProcessAsync(); // pass token string to SwapBuilder or use FromInputs()

// From raw proofs
List<Proof> rebalanced = await wallet
    .Swap()
    .FromInputs(existingProofs)
    .ProcessAsync();
```

### Melt (tokens &rarr; Lightning)

```csharp
var meltHandler = await wallet
    .CreateMeltQuote()
    .WithInvoice("lnbc...")
    .WithUnit("sat")
    .ProcessAsyncBolt11();

List<Proof> changeProofs = await meltHandler.Melt(inputProofs);
```

### Restore (from mnemonic)

```csharp
IEnumerable<Proof> recovered = await wallet
    .Restore()
    .ProcessAsync();
```

### Token encoding

```csharp
var token = new CashuToken
{
    Unit = "sat",
    Tokens = [new CashuToken.Token { Mint = "https://testnut.cashu.space", Proofs = proofs }]
};

string v4 = token.Encode("B");          // cashuB... (CBOR, compact)
string v3 = token.Encode("A");          // cashuA... (JSON)
string uri = token.Encode("B", makeUri: true); // cashu:cashuB...

var decoded = CashuTokenHelper.Decode(v4, out string version);
```

### P2PK / HTLC spending conditions

```csharp
// Mint tokens locked to a pubkey
var mintHandler = await wallet
    .CreateMintQuote()
    .WithAmount(500)
    .WithP2PkLock(new P2PKBuilder { Pubkeys = [pubkey], SignatureThreshold = 1 })
    .ProcessAsyncBolt11();

// Spend P2PK-locked tokens by signing during swap
List<Proof> proofs = await wallet
    .Swap()
    .FromInputs(lockedProofs)
    .WithPrivkeys([privKey])
    .ProcessAsync();
```

### Direct API access

If you need raw protocol access without the wallet abstraction:

```csharp
var httpClient = new HttpClient { BaseAddress = new Uri("https://testnut.cashu.space/") };
var api = new CashuHttpClient(httpClient);

var info = await api.GetInfo();
var keysets = await api.GetKeysets();
var mintQuote = await api.CreateMintQuote<PostMintQuoteBolt11Response, PostMintQuoteBolt11Request>(
    "bolt11", new PostMintQuoteBolt11Request { Amount = 1000, Unit = "sat" }
);
```

### WebSockets (NUT-17)

```csharp
var wallet = Wallet.Create()
    .WithMint("https://testnut.cashu.space")
    .WithWebsocketService(new WebsocketService());

var ws = await wallet.GetWebsocketService();
// subscribe to quote state changes, proof state updates, etc.
```

## Implemented NUTs

| NUT                                                      | Description                             |
|----------------------------------------------------------|-----------------------------------------|
| [00](https://github.com/cashubtc/nuts/blob/main/00.md)   | Cryptographic primitives & token format |
| [01](https://github.com/cashubtc/nuts/blob/main/01.md)   | Mint public key distribution            |
| [02](https://github.com/cashubtc/nuts/blob/main/02.md)   | Keysets and keyset IDs                  |
| [03](https://github.com/cashubtc/nuts/blob/main/03.md)   | Swapping tokens                         |
| [04](https://github.com/cashubtc/nuts/blob/main/04.md)   | Minting tokens                          |
| [05](https://github.com/cashubtc/nuts/blob/main/05.md)   | Melting tokens                          |
| [06](https://github.com/cashubtc/nuts/blob/main/06.md)   | Mint info                               |
| [07](https://github.com/cashubtc/nuts/blob/main/07.md)   | Token state check                       |
| [08](https://github.com/cashubtc/nuts/blob/main/08.md)   | Lightning fee return                    |
| [09](https://github.com/cashubtc/nuts/blob/main/09.md)   | Token restoration                       |
| [10](https://github.com/cashubtc/nuts/blob/main/10.md)   | Spending conditions                     |
| [11](https://github.com/cashubtc/nuts/blob/main/11.md)   | Pay-to-Public-Key (P2PK)                |
| [12](https://github.com/cashubtc/nuts/blob/main/12.md)   | DLEQ proofs                             |
| [13](https://github.com/cashubtc/nuts/blob/main/13.md)   | Deterministic secrets (BIP39)           |
| [14](https://github.com/cashubtc/nuts/blob/main/14.md)   | Hash Time-Locked Contracts (HTLC)       |
| [17](https://github.com/cashubtc/nuts/blob/main/17.md)   | WebSocket subscriptions                 |
| [18](https://github.com/cashubtc/nuts/blob/main/18.md)   | Payment requests                        |
| [20](https://github.com/cashubtc/nuts/blob/main/20.md)   | Signature on Mint Quote                 |
| [23](https://github.com/cashubtc/nuts/blob/main/23.md)   | BOLT11                                  |
| [25](https://github.com/cashubtc/nuts/blob/main/25.md)   | BOLT12                                  |
| [26](https://github.com/cashubtc/nuts/blob/main/26.md)   | Payment Request Bech32m Encoding        |
| [27](https://github.com/cashubtc/nuts/blob/main/27.md)   | Nostr Mint Backup                       |
| [28](https://github.com/cashubtc/nuts/blob/main/28.md)   | Pay-to-Blinded-Key (P2BK)               |

TODO:

| NUT                                                    | Description                             |
|--------------------------------------------------------|-----------------------------------------|
| [15](https://github.com/cashubtc/nuts/blob/main/15.md) | Multipath payments                      |
| [21](https://github.com/cashubtc/nuts/blob/main/21.md) | Clear Authentication                      |
| [22](https://github.com/cashubtc/nuts/blob/main/22.md) | Blind Authentication                 |
## Requirements

- .NET 8.0+

## License

MIT - see [LICENSE](LICENSE).

## Resources

- [Cashu protocol](https://cashu.space)
- [NUT specifications](https://github.com/cashubtc/nuts/)
- [NuGet package](https://www.nuget.org/packages/DotNut/)
- [GitHub](https://github.com/Kukks/DotNut)
