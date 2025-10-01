using DotNut.Abstractions.Interfaces;
using DotNut.Api;
using DotNut.ApiModels;
using DotNut.NBitcoin.BIP39;

namespace DotNut.Abstractions;

/// <summary>
/// Fluent builder interface for Cashu Wallet operations
/// </summary>
public interface ICashuWalletBuilder
{
    ICashuWalletBuilder WithInfo(MintInfo info);
    ICashuWalletBuilder WithInfo(GetInfoResponse info);
    ICashuWalletBuilder WithKeysets(IEnumerable<GetKeysetsResponse.KeysetItemResponse> keysets);
    ICashuWalletBuilder WithKeysets(GetKeysetsResponse keysets);
    ICashuWalletBuilder WithKeys(IEnumerable<GetKeysResponse.KeysetItemResponse> keys);
    ICashuWalletBuilder WithSelector(IProofSelector selector);
    ICashuWalletBuilder WithMint(ICashuApi mintApi);
    ICashuWalletBuilder WithMint(string mintUrl);
    ICashuWalletBuilder WithMnemonic(Mnemonic mnemonic); 
    ICashuWalletBuilder WithMnemonic(string mnemonic);
    ICashuWalletBuilder WithCounter(ICounter counter);
    Task<MintInfo> GetInfo(bool forceReferesh = false, CancellationToken cts = default);
    Task<OutputData> CreateOutputs(List<ulong> amounts, KeysetId id, CancellationToken cts = default);

    Task<IDictionary<string, KeysetId>?> GetActiveKeysetIdsWithUnits();

    ICashuApi? GetMintApi();

    
    // Swap operations
    ICashuWalletSwapBuilder Swap();
    
    // Melt operations (pay invoices)
    ICashuWalletMeltQuoteBuilder CreateMeltQuote();
    
    // Mint operations (receive from invoice)
    ICashuWalletMintBuilder CreateMintQuote();
    
    // Restore operations
    ICashuWalletRestoreBuilder Restore();
}

/// <summary>
/// Swap operation builder
/// </summary>
public interface ICashuWalletSwapBuilder
{
    ICashuWalletSwapBuilder WithUnit(string unit);
    ICashuWalletSwapBuilder ForKeyset(KeysetId targetKeysetId);
    ICashuWalletSwapBuilder WithOutputs(IEnumerable<BlindedMessage> outputs);
    Task<List<Proof>> ProcessAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Melt operation builder (pay invoices)
/// </summary>
public interface ICashuWalletMeltQuoteBuilder
{
    ICashuWalletMeltQuoteBuilder WithUnit(string unit);
    ICashuWalletMeltQuoteBuilder WithInvoice(string bolt11Invoice);
    ICashuWalletMeltQuoteBuilder WithMethod(string method = "bolt11");
    Task<MeltResult> ProcessAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Mint operation builder (receive from invoice)
/// </summary>
public interface ICashuWalletMintBuilder
{
    ICashuWalletMintBuilder WithUnit(string unit);
    ICashuWalletMintBuilder WithAmount(ulong amount);
    ICashuWalletMintBuilder WithOutputs(IEnumerable<BlindedMessage> outputs);
    ICashuWalletMintBuilder WithMethod(string method = "bolt11");
    // Task<MintResult> ProcessAsync(CancellationToken cancellationToken = default);
    Task<IMintHandler> ProcessAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Restore operation builder
/// </summary>
public interface ICashuWalletRestoreBuilder
{
    ICashuWalletRestoreBuilder ForKeysetIds(IEnumerable<KeysetId> keysetIds);
    Task<RestoreResult> ProcessAsync(CancellationToken cancellationToken = default);
}
