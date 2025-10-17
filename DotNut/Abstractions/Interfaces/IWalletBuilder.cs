using DotNut.Abstractions.Interfaces;
using DotNut.Abstractions.Websockets;
using DotNut.Api;
using DotNut.ApiModels;
using DotNut.ApiModels.Mint.bolt12;
using DotNut.NBitcoin.BIP39;

namespace DotNut.Abstractions;

/// <summary>
/// Fluent builder interface for Cashu Wallet operations
/// </summary>
public interface IWalletBuilder
{
    IWalletBuilder WithMint(ICashuApi mintApi);
    IWalletBuilder WithMint(string mintUrl);
    IWalletBuilder WithInfo(MintInfo info);
    IWalletBuilder WithInfo(GetInfoResponse info);
    IWalletBuilder WithKeysets(IEnumerable<GetKeysetsResponse.KeysetItemResponse> keysets);
    IWalletBuilder WithKeysets(GetKeysetsResponse keysets);
    IWalletBuilder WithKeys(IEnumerable<GetKeysResponse.KeysetItemResponse> keys);
    IWalletBuilder WithKeys(GetKeysResponse keys);
    IWalletBuilder WithKeysetSync(bool syncKeyset = true);
    IWalletBuilder WithSelector(IProofSelector selector);
    IWalletBuilder WithMnemonic(Mnemonic mnemonic); 
    IWalletBuilder WithMnemonic(string mnemonic);
    IWalletBuilder WithCounter(ICounter counter);
    IWalletBuilder WithCounter(IDictionary<KeysetId, int> counter);
    IWalletBuilder ShouldBumpCounter(bool shouldBumpCounter = true);
    IWalletBuilder WithWebsocketService(IWebsocketService websocketService);

    Task<MintInfo> GetInfo(bool forceReferesh = false, CancellationToken ct = default);
    Task<OutputData> CreateOutputs(List<ulong> amounts, KeysetId id, CancellationToken ct = default);

    Task<IDictionary<string, KeysetId>?> GetActiveKeysetIdsWithUnits(CancellationToken ct = default);

    Task<ICashuApi> GetMintApi(CancellationToken ct = default);

    Task<KeysetId?> GetActiveKeysetId(string unit, CancellationToken ct = default);
    Task<List<GetKeysResponse.KeysetItemResponse>> GetKeys(bool forceRefresh = false, CancellationToken ct = default);

    Task<GetKeysResponse.KeysetItemResponse> GetKeys(KeysetId id, bool forceRefresh = false,
        CancellationToken ct = default);

    Task<List<GetKeysetsResponse.KeysetItemResponse>> GetKeysets(bool forceRefresh = false,
        CancellationToken ct = default);

    Task<OutputData> CreateOutputs(List<ulong> amounts, string unit, CancellationToken ct = default);

    Task<SendResponse> SelectProofsToSend(List<Proof> proofs, ulong amount, bool includeFees,
        CancellationToken ct = default);
    
    Task<IWebsocketService> GetWebsocketService(CancellationToken ct = default);
    
    // Swap operations
    ISwapBuilder Swap();
    
    // Melt operations (pay invoices)
    IMeltQuoteBuilder CreateMeltQuote();
    
    // Mint operations (receive from invoice)
    IMintQuoteBuilder CreateMintQuote();
    
    IRestoreBuilder Restore();
    
}

