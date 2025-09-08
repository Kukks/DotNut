# DotNut Demo Application

A comprehensive interactive demo showcasing all features of the DotNut Cashu library.

## 🚀 Quick Start

1. **Clone and build the project:**
   ```bash
   git clone https://github.com/Kukks/DotNut.git
   cd DotNut
   dotnet build
   ```

2. **Run the demo:**
   ```bash
   cd DotNut.Demo
   dotnet run
   ```

3. **Follow the interactive menu to explore different features!**

## 📋 Available Demos

### 1. Connect to Mint & Get Info
- Connects to a live Cashu mint (testnut.cashu.space)
- Retrieves mint information, keysets, and public keys
- Demonstrates basic API communication

### 2. Create Cashu Token
- Creates example proofs and tokens
- Shows token structure and properties
- Demonstrates wallet management

### 3. Token Encoding/Decoding
- Shows V3 (JSON) vs V4 (CBOR) encoding formats
- Demonstrates URI format for sharing
- Compares encoding efficiency
- Shows decode and verification process

### 4. Lightning Mint Quote (Demo)
- Creates real mint quotes from the test mint
- Shows Lightning invoice generation
- Explains the minting process flow
- **Note:** Shows API usage with real responses

### 5. Lightning Melt Quote (Demo)
- Demonstrates melt quote creation process
- Shows expected API structure
- Explains melting workflow
- **Note:** Uses example invoice for demonstration

### 6. Token Swapping (Demo)
- Explains the token swapping concept
- Shows input/output structure
- Demonstrates denomination management
- **Note:** Educational walkthrough of the process

### 7. Working with Secrets
- Simple string secrets
- Random secret generation
- Demonstrates hash-to-curve operations
- Shows secret uniqueness properties

### 8. Mnemonic Secrets (NUT-13)
- Generates BIP39 mnemonic phrases
- Derives deterministic secrets and blinding factors
- Shows counter-based secret derivation
- Explains security considerations

### 9. P2PK Secrets (NUT-11)
- Creates Pay-to-Public-Key secrets
- Demonstrates multisignature setup
- Shows time-locked spending conditions
- Explains advanced spending scenarios

### 10. Show Current Wallet
- Displays current proof inventory
- Shows denomination breakdown
- Demonstrates wallet state management

### 11. Check Proof States
- Shows proof state checking API
- Explains UNSPENT/SPENT/RESERVED states
- Demonstrates proof validation

## 🧪 What's Educational vs Real

### Real Interactions
- **Mint Connection**: Connects to actual testnet mint
- **Mint Info/Keys**: Real data from the mint
- **Lightning Mint Quotes**: Real Lightning invoices generated
- **Token Encoding/Decoding**: Fully functional
- **Cryptographic Operations**: All secret/key operations are real

### Educational Demos
- **Token Creation**: Uses example proofs (not minted from actual operations)
- **Melt Quotes**: Uses fake invoice for demonstration
- **Swapping**: Explains process without actual mint interaction
- **Proof States**: Uses example proofs (will show expected errors)

## 🔧 Technical Details

### Dependencies
- **.NET 8.0**: Target framework
- **DotNut**: Main Cashu library (project reference)
- **NBitcoin.Secp256k1**: For cryptographic operations

### Architecture
The demo is structured as an interactive console application with:
- **Menu-driven interface**: Easy navigation between features
- **Comprehensive error handling**: Shows both expected and unexpected errors
- **Educational output**: Explains concepts and processes
- **Real API integration**: Uses live testnet mint when possible

### Code Structure
- `Program.cs`: Main application with all demo implementations
- Interactive menu system for easy exploration
- Modular demo methods for each feature
- Helper methods for creating example data
- Extension methods for utility functions

## 🎯 Learning Objectives

After running through the demos, you'll understand:

1. **Basic Cashu Concepts**: Tokens, proofs, secrets, keysets
2. **API Usage**: How to interact with Cashu mints
3. **Token Management**: Creation, encoding, decoding, sharing
4. **Advanced Features**: P2PK, mnemonics, time-locks
5. **Cryptographic Foundations**: Hash-to-curve, blind signatures
6. **Real-world Workflows**: Mint → Send → Receive → Melt cycles

## 🛠️ Extending the Demo

Want to add more features? Consider:

- **Real Minting**: Implement actual Lightning payment and minting
- **Token Persistence**: Save/load wallet state
- **Multi-mint Support**: Connect to multiple mints
- **HTLC Demos**: Hash Time-Locked Contract examples
- **Nostr Integration**: Payment requests over Nostr

## 🤝 Contributing

Found issues or want to improve the demo? Contributions are welcome:

1. Fork the repository
2. Create your feature branch
3. Add your demo or improvement
4. Submit a pull request

## 📚 Next Steps

After exploring the demo:

1. **Read the main README**: Understand the full library capabilities
2. **Check the documentation**: Deep dive into specific features
3. **Try with real Lightning**: Test minting with actual payments
4. **Build your own app**: Use DotNut in your projects
5. **Join the community**: Connect with other Cashu developers

## ⚠️ Important Notes

- **Testnet Only**: This demo uses testnet Bitcoin/Lightning
- **Educational Purpose**: Not for production use without modifications
- **API Keys**: No API keys required for the demo
- **Internet Required**: Connects to external mint for live demos

Happy exploring! 🥜✨