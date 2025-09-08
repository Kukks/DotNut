# DotNut 🥜 

A complete C# implementation of the [Cashu protocol](https://cashu.space) - privacy-preserving electronic cash built on Bitcoin.

[![NuGet](https://img.shields.io/nuget/v/DotNut.svg)](https://www.nuget.org/packages/DotNut/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## What is Cashu?

Cashu is a free and open-source Chaumian e-cash system built for Bitcoin. It offers near-perfect privacy for users and can serve as an excellent Layer 2 scaling solution. DotNut provides a full-featured C# client library for interacting with Cashu mints.

## Installation

```bash
dotnet add package DotNut
```

## Quick Start

### 1. Connect to a Mint

```csharp
using DotNut;
using DotNut.Api;

// Connect to a Cashu mint
var httpClient = new HttpClient();
httpClient.BaseAddress = new Uri("https://testnut.cashu.space/");
var client = new CashuHttpClient(httpClient);

// Get mint information
var info = await client.GetInfo();
Console.WriteLine($"Connected to: {info.Name}");
```

### 2. Create and Send Tokens

```csharp
using DotNut.Encoding;

// Create a token from proofs (obtained from minting)
var token = new CashuToken
{
    Unit = "sat",
    Memo = "Payment for coffee ☕",
    Tokens = new List<CashuToken.Token>
    {
        new CashuToken.Token
        {
            Mint = "https://testnut.cashu.space",
            Proofs = myProofs // Your token proofs
        }
    }
};

// Encode for sharing (creates a cashu token string)
string encodedToken = token.Encode("B"); // V4 format (compact)
Console.WriteLine($"Token to share: {encodedToken}");

// Receive and decode a token
var receivedToken = CashuTokenHelper.Decode(encodedToken, out string version);
Console.WriteLine($"Received {receivedToken.TotalAmount()} sats");
```

### 3. Basic Mint Operations

```csharp
using DotNut.ApiModels.Mint;

// Create a mint quote for 1000 sats via Lightning
var mintQuote = await client.CreateMintQuote<PostMintQuoteBolt11Response, PostMintQuoteBolt11Request>(
    "bolt11", 
    new PostMintQuoteBolt11Request { Amount = 1000, Unit = "sat" }
);

Console.WriteLine($"Pay this invoice: {mintQuote.Request}");
// After paying the Lightning invoice, mint your tokens...

// Create a melt quote to convert tokens back to Lightning
var meltQuote = await client.CreateMeltQuote<PostMeltQuoteBolt11Response, PostMeltQuoteBolt11Request>(
    "bolt11",
    new PostMeltQuoteBolt11Request 
    { 
        Request = "lnbc1000n1...", // Lightning invoice to pay
        Unit = "sat" 
    }
);
```

## Core Concepts

### Tokens and Proofs
- **CashuToken**: Container for one or more tokens from different mints
- **Proof**: Cryptographic proof representing a specific amount
- **Secret**: The secret behind each proof (can be simple strings or complex conditions)

### Privacy Features
- **Blind Signatures**: Mint doesn't know which tokens belong to whom
- **DLEQ Proofs**: Verify mint behavior without compromising privacy
- **Token Swapping**: Change denominations while maintaining privacy

### Advanced Features
- **P2PK (Pay-to-Public-Key)**: Multi-signature spending conditions
- **HTLCs**: Hash Time-Locked Contracts for atomic swaps
- **Deterministic Secrets**: Generate secrets from mnemonic phrases

## Working with Secrets

```csharp
using DotNut;

// Simple string secret
var secret = new StringSecret("my-random-secret");

// Deterministic secret from mnemonic (NUT-13)
var mnemonic = new Mnemonic("abandon abandon abandon...");
var deterministicSecret = mnemonic.DeriveSecret(keysetId, counter: 0);

// Pay-to-Public-Key secret (NUT-11)
var p2pkBuilder = new P2PkBuilder
{
    Pubkeys = new[] { pubkey1, pubkey2 },
    SignatureThreshold = 1, // 1-of-2 multisig
    SigFlag = "SIG_INPUTS"
};
var p2pkSecret = new Nut10Secret(P2PKProofSecret.Key, p2pkBuilder.Build());
```

## Token Operations

```csharp
// Check if proofs are still valid
var stateRequest = new PostCheckStateRequest { Ys = proofs.Select(p => p.Y).ToArray() };
var stateResponse = await client.CheckState(stateRequest);

// Swap tokens to different denominations
var swapRequest = new PostSwapRequest
{
    Inputs = inputProofs,
    Outputs = newBlindedMessages
};
var swapResponse = await client.Swap(swapRequest);

// Restore tokens from secrets (if you've lost proofs)
var restoreRequest = new PostRestoreRequest { Outputs = blindedMessages };
var restoreResponse = await client.Restore(restoreRequest);
```

## Token Encoding Formats

DotNut supports multiple token encoding formats:

```csharp
// V3 format (JSON-based)
string v3Token = token.Encode("A");

// V4 format (CBOR-based, more compact)
string v4Token = token.Encode("B");

// As URI for easy sharing
string tokenUri = token.Encode("B", makeUri: true);
// Result: "cashu:cashuB..."
```

## Error Handling

```csharp
try
{
    var response = await client.Swap(swapRequest);
}
catch (CashuProtocolException ex)
{
    Console.WriteLine($"Mint error: {ex.Error.Detail}");
    Console.WriteLine($"Error code: {ex.Error.Code}");
}
```

## Nostr Integration

DotNut includes a separate package for Nostr integration:

```bash
dotnet add package DotNut.Nostr
```

This enables payment requests over Nostr (NUT-18) and other Nostr-based features.

## Implemented Specifications

Complete implementation of the [Cashu protocol specifications](https://github.com/cashubtc/nuts/):

| NUT | Description | Status |
|-----|-------------|--------|
| [00](https://github.com/cashubtc/nuts/blob/main/00.md) | Cryptographic primitives | ✅ |
| [01](https://github.com/cashubtc/nuts/blob/main/01.md) | Mint public key distribution | ✅ |
| [02](https://github.com/cashubtc/nuts/blob/main/02.md) | Keysets and keyset IDs | ✅ |
| [03](https://github.com/cashubtc/nuts/blob/main/03.md) | Swapping tokens | ✅ |
| [04](https://github.com/cashubtc/nuts/blob/main/04.md) | Minting tokens | ✅ |
| [05](https://github.com/cashubtc/nuts/blob/main/05.md) | Melting tokens | ✅ |
| [06](https://github.com/cashubtc/nuts/blob/main/06.md) | Mint info | ✅ |
| [07](https://github.com/cashubtc/nuts/blob/main/07.md) | Token state check | ✅ |
| [08](https://github.com/cashubtc/nuts/blob/main/08.md) | Lightning fee return | ✅ |
| [09](https://github.com/cashubtc/nuts/blob/main/09.md) | Token restoration | ✅ |
| [10](https://github.com/cashubtc/nuts/blob/main/10.md) | Spending conditions | ✅ |
| [11](https://github.com/cashubtc/nuts/blob/main/11.md) | Pay-to-Public-Key (P2PK) | ✅ |
| [12](https://github.com/cashubtc/nuts/blob/main/12.md) | DLEQ proofs | ✅ |
| [13](https://github.com/cashubtc/nuts/blob/main/13.md) | Deterministic secrets | ✅ |
| [14](https://github.com/cashubtc/nuts/blob/main/14.md) | Hash Time-Locked Contracts | ✅ |
| [15](https://github.com/cashubtc/nuts/blob/main/15.md) | Multipath payments | ✅ |
| [18](https://github.com/cashubtc/nuts/blob/main/18.md) | Payment requests | ✅ |

## Requirements

- .NET 8.0 or later
- HTTP client for mint communication

## Contributing

Contributions are welcome! Please feel free to submit issues, feature requests, or pull requests.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Resources

- [Cashu Protocol](https://cashu.space)
- [Cashu Specifications (NUTs)](https://github.com/cashubtc/nuts/)
- [NuGet Package](https://www.nuget.org/packages/DotNut/)
- [GitHub Repository](https://github.com/Kukks/DotNut)