using DotNut.Abstractions.Interfaces;
using DotNut.Abstractions.Websockets;
using DotNut.Api;
using DotNut.ApiModels;
using DotNut.ApiModels.Info;
using DotNut.NBitcoin.BIP39;
using NBitcoin.Secp256k1;

namespace DotNut.Abstractions;

/// <summary>
/// Main Cashu Wallet class implementing fluent builder pattern
/// </summary>
///<inheritdoc/>

public class Wallet : IWalletBuilder
{
    private MintInfo? _info;
    private IProofSelector? _selector;
    private ICashuApi? _mintApi;
    private List<GetKeysetsResponse.KeysetItemResponse>? _keysets;
    private List<GetKeysResponse.KeysetItemResponse>? _keys;
    private Dictionary<KeysetId, ulong>? _keysetFees => _keysets?.ToDictionary(k=>k.Id, k=>k.InputFee??0);
    private Mnemonic? _mnemonic;
    private ICounter? _counter;
    
    private IWebsocketService? _wsService;
    
    //flags 
    private bool _shouldSyncKeyset = true;
    private DateTime? _lastSync = DateTime.MinValue;
    private TimeSpan? _syncThresold; // if null sync only once 
    
    private bool _shouldBumpCounter = true;
    private bool _allowInvalidKeysetIds = false;

    
    /*
     * Fluent Builder Methods 
     */
    public static IWalletBuilder Create() => new Wallet();
    
    public IWalletBuilder WithMint(ICashuApi mintApi)
    {
        _mintApi = mintApi;
        return this;
    }
    
    public IWalletBuilder WithMint(string mintUrl)
    {
        var httpClient = new HttpClient{ BaseAddress = new Uri(mintUrl)};
        _mintApi = new CashuHttpClient(httpClient);
        return this;
    }
    
    public IWalletBuilder WithInfo(MintInfo info)
    {
        this._info = info;
        return this;
    }
    
    public IWalletBuilder WithInfo(GetInfoResponse info) => this.WithInfo(new MintInfo(info));
   
    public IWalletBuilder WithKeysets(IEnumerable<GetKeysetsResponse.KeysetItemResponse> keysets)
    {
        this._keysets = keysets.ToList();
        return this;
    }
    
    public IWalletBuilder WithKeysets(GetKeysetsResponse keysets) => this.WithKeysets(keysets.Keysets.ToList());
    
    public IWalletBuilder WithKeys(IEnumerable<GetKeysResponse.KeysetItemResponse> keys)
    {
        this._keys = keys.ToList();
        return this;
    }
    
    public IWalletBuilder WithKeys(GetKeysResponse keys) => this.WithKeys(keys.Keysets.ToList());
    
    public IWalletBuilder WithKeysetSync(bool syncKeyset = true)
    {
        this._shouldSyncKeyset = syncKeyset;
        return this;
    }

    public IWalletBuilder WithKeysetSync(bool syncKeyset, TimeSpan syncThreesold)
    {
        this._shouldSyncKeyset = syncKeyset;
        this._syncThresold = syncThreesold;
        return this;
    }
    
    public IWalletBuilder WithSelector(IProofSelector selector)
    {
        _selector = selector;
        return this;
    }
    
    public IWalletBuilder WithMnemonic(Mnemonic mnemonic)
    {
        _mnemonic = mnemonic;
        return this;
    }
    
    public IWalletBuilder WithMnemonic(string mnemonic)
    {
        _mnemonic = new Mnemonic(mnemonic);
        return this;
    }
    
    /// <summary>
    /// Optional and mandatory if Mnemonic provided. Counter for each Keyset Id for derivation purposes.
    /// </summary>
    /// <param name="counter">Counter object</param>
    public IWalletBuilder WithCounter(ICounter counter)
    {
        this._counter = counter;
        return this;
    }

    /// <summary>
    /// Optional and mandatory if Mnemonic provided. Counter for each Keyset Id for derivation purposes.
    /// </summary>
    /// <param name="counter">Counter dictionary</param>
    /// <returns></returns>
    public IWalletBuilder WithCounter(IDictionary<KeysetId, int> counter)
    {
        this._counter = new InMemoryCounter(counter);
        return this;
    }

    /// <summary>
    /// Optional and if not set, always true. Controls automatic counter incrementation for secret generation.
    /// </summary>
    /// <param name="shouldBumpCounter">If true, counter increments automatically. If false, requires manual management.</param>
    /// <remarks>
    /// WARNING: Disabling auto-increment is potentially dangerous. Manual counter management is required 
    /// to prevent secret reuse, which will cause mint rejection and operation failures.
    /// </remarks>
    public IWalletBuilder ShouldBumpCounter(bool shouldBumpCounter = true)
    {
        this._shouldBumpCounter = shouldBumpCounter;
        return this;
    }
    
    /// <summary>
    /// Optional.
    /// Adds websocket service. You should use single websocket service (singleton at best) for multiple wallets, in order to handle everything in nice manner.
    /// If not set, but requested it'll be created automatically (which won't be so optimal).
    /// </summary>
    /// <param name="websocketService"></param>
    /// <returns></returns>
    public IWalletBuilder WithWebsocketService(IWebsocketService websocketService)
    {
        this._wsService = websocketService;
        return this;
    }


    /*
     * Main api methods
     */
    

    public IMintQuoteBuilder CreateMintQuote()
    {
        _ensureApiConnected();
        return new MintQuoteBuilder(this);
    }
    
    public ISwapBuilder Swap()
    {
        _ensureApiConnected();
        return new SwapBuilder(this);
    }

    public IMeltQuoteBuilder CreateMeltQuote()
    {
        _ensureApiConnected();
        return new MeltQuoteBuilder(this);
    }
    

    public async Task<PostCheckStateResponse> CheckState(IEnumerable<Proof> proofs)
    {
        return await CheckState(proofs.Select(p => p.Secret.ToCurve()));
    }

    public async Task<PostCheckStateResponse> CheckState(IEnumerable<ECPubKey> Ys)
    {
        _ensureApiConnected();
        var req = new PostCheckStateRequest()
        {
            Ys = Ys.Select(y=>y.ToString()).ToArray(),
        };
        return await _mintApi!.CheckState(req);
    }
    
    public IRestoreBuilder Restore()
    {
        _ensureApiConnected();
        return new RestoreBuilder(this);
    }
    
    
    
    /*
     * Public Mint utils
     */
    
    /// <summary>
    /// Set Last sync date to DateTime.MinValue - keysets will be synced before next operation
    /// </summary>
    public void InvalidateCache()
    {
        _lastSync = DateTime.MinValue;
    }

    /// <summary>
    /// Get active keyset id for chosen unit.
    /// </summary>
    /// <param name="unit">keyset unit, e.g. sat</param>
    /// <param name="ct"></param>
    /// <returns>Active keysetId</returns>
    public async Task<KeysetId?> GetActiveKeysetId(string unit, CancellationToken ct = default)
    {
        await _maybeSyncKeys(ct);
        return _keysets?
            .OrderBy(k => k.InputFee)
            .FirstOrDefault(k => k is { Active: true } && k.Unit == unit, null)
            ?.Id;
    }

    /// <summary>
    /// Get active keyset ids for each unit
    /// </summary>
    /// <returns>Dictionary of (unit, KeysetId) </returns>
    public async Task<IDictionary<string, KeysetId>?> GetActiveKeysetIdsWithUnits(CancellationToken ct = default)
    {
        await _maybeSyncKeys(ct);
        return _keysets?
            .GroupBy(k => k.Unit)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(k => k.InputFee).First().Id
            );
    }
    
    /// <summary>
    /// Get keys of current mint stored in wallet.
    /// </summary>
    /// <param name="forceRefresh">Refetch flag</param>
    /// <param name="ct"></param>
    /// <returns>Mints keys</returns>
    public async Task<List<GetKeysResponse.KeysetItemResponse>> GetKeys(bool forceRefresh = false, CancellationToken ct = default)
    {
        if (forceRefresh)
        {
           this._keys = await _fetchKeys(ct);
           return this._keys ?? [];
        }
        await _maybeSyncKeys(ct);
        return this._keys ?? [];
    }

   /// <summary>
   /// Get Keys for given KeysetID
   /// </summary>
   /// <param name="id">KeysetId</param>
   /// <param name="forceRefresh">Refetch flag</param>
   /// <param name="ct"></param>
   /// <returns>Keys for given keyset</returns>
   /// <exception cref="ArgumentNullException">If wallet doesn't contain keysets for given keysetId</exception>
    public async Task<GetKeysResponse.KeysetItemResponse> GetKeys(KeysetId id, bool forceRefresh = false, CancellationToken ct = default)
    {
        if (forceRefresh)
        {
            return await _fetchKeys(id, ct);
        }
        if (this._keys == null)
        {
            throw new ArgumentNullException(nameof(this._keys), "Wallet doesn't contain keys for this keyset!");
        }
        return this._keys.Single(k => k.Id == id);
    }
   
   /// <summary>
   /// Get Keysets stored in wallet
   /// </summary>
   /// <param name="forceRefresh">Refetch flag</param>
   /// <param name="ct"></param>
   /// <returns>List of Keysets</returns>
    public async Task<List<GetKeysetsResponse.KeysetItemResponse>> GetKeysets(bool forceRefresh = false, CancellationToken ct = default)
    {
        if (forceRefresh)
        {
           this._keysets = await _fetchKeysets(ct);
           return _keysets ?? [];
        }
        await _maybeSyncKeys(ct);
        return _keysets ?? [];
    }
   
   /// <summary>
   /// Get Mints info, supported methods etc. 
   /// </summary>
   /// <param name="forceReferesh">Refetch flag</param>
   /// <param name="ct"></param>
   /// <returns>MintInfo object</returns>
    public async Task<MintInfo> GetInfo(bool forceReferesh = false, CancellationToken ct = default)
    {
        if (forceReferesh)
        {
            return await _fetchMintInfo(ct);
        }
        return await _lazyFetchMintInfo(ct);
    }
   
   /// <summary>
   /// Create Outputs (BlindedMessags, Blinding Factors, Secrets), for given keysetId.
   /// Deterministic if Mnemonic and Counter set up.
   /// </summary>
   /// <param name="amounts">List of amounts in Outputs.</param>
   /// <param name="id">Keyset ID</param>
   /// <param name="ct"></param>
   /// <returns>Outputs</returns>
   /// <exception cref="ArgumentNullException">If keys not set. If Mnemonic set, but no Counter.</exception>
    public async Task<OutputData> CreateOutputs(List<ulong> amounts, KeysetId id, CancellationToken ct = default)
    {
        await _maybeSyncKeys(ct);
        if (this._keys == null)
        {
            throw new ArgumentNullException(nameof(this._keys), "No Keys found. Make sure to fetch them!");
        }
        var keyset = this._keys.Single(k => k.Id == id);
        if (this._mnemonic == null)
        {
            return Utils.CreateOutputs(amounts, id, keyset.Keys);
        }

        if (this._counter == null)
        {
            throw new ArgumentNullException(nameof(ICounter), "Can't derive outputs without keyset counter");
        }

        var counterValue = await this._counter.GetCounterForId(id, ct);
        if (_shouldBumpCounter)
        {
            await this._counter.IncrementCounter(id, amounts.Count, ct);
        }
        return Utils.CreateOutputs(amounts, id, keyset.Keys, this._mnemonic, counterValue);
    }
   
    /// <summary>
    /// Create Outputs for active KeysetId for given unit.
    /// </summary>
    /// <param name="amounts">List of amounts.</param>
    /// <param name="unit"></param>
    /// <param name="ct"></param>
    /// <returns>Outputs</returns>
    /// <exception cref="ArgumentNullException">If no keysetID stored in wallet.</exception>
    public async Task<OutputData> CreateOutputs(List<ulong> amounts, string unit, CancellationToken ct = default)
    {
        var keysetId = await this.GetActiveKeysetId(unit, ct);
        if (keysetId == null)
        {
            throw new ArgumentNullException(nameof(keysetId));
        }
        return await this.CreateOutputs(amounts, keysetId, ct);
    }
    
    public async Task<SendResponse> SelectProofsToSend(List<Proof> proofs, ulong amount, bool includeFees, CancellationToken ct = default)
    {
        if (this._selector == null)
        {
            await _maybeSyncKeys(ct);
            ArgumentNullException.ThrowIfNull(this._keysetFees);
            this._selector = new ProofSelector(this._keysetFees);
        }

        return await _selector.SelectProofsToSend(proofs, amount, includeFees, ct);
    }
    
    public async Task<ICashuApi> GetMintApi(CancellationToken ct = default)
    {
        _ensureApiConnected();
        return _mintApi;
    }
    public async Task<IProofSelector>? GetSelector(CancellationToken ct = default)
    {
        if (this._selector == null)
        {
            await _maybeSyncKeys(ct);
            ArgumentNullException.ThrowIfNull(this._keysetFees);
            this._selector = new ProofSelector(this._keysetFees);
        }
        return this._selector;
    }

    public async Task<IWebsocketService> GetWebsocketService(CancellationToken ct = default)
    {
        return this._wsService ??= new WebsocketService();
    }
    public Mnemonic? GetMnemonic() => _mnemonic;
    public ICounter? GetCounter() => _counter;
    
        
    /*
     * Private helpers
     */
    
    /// <summary>
    /// Throws exception if api not connected
    /// </summary>
    /// <param name="msg"></param>
    /// <exception cref="ArgumentNullException"></exception>
    internal void _ensureApiConnected(string? msg = null)
    {
        if (_mintApi != null)
        {
            return;
        }

        if (msg is not null)
        {
            throw new ArgumentNullException(nameof(this._mintApi), msg);
        }

        throw new ArgumentNullException(nameof(this._mintApi));
    }
    
    /// <summary>
    /// Wrapper for GetKeysets api endpoint. Formats Keysets to list.
    /// </summary>
    /// <returns>List of Keysets</returns>
    /// <exception cref="ArgumentNullException">May be thrown if mint is not set.</exception>
    private async Task<List<GetKeysetsResponse.KeysetItemResponse>> _fetchKeysets(CancellationToken ct = default)
    {
        _ensureApiConnected("Can't fetch keysets without mint api!");
        var keysetsRaw = await _mintApi!.GetKeysets(ct);
        return keysetsRaw.Keysets.ToList();
    }
    
    /// <summary>
    /// Wrapper for GetKeys api endpoint. Validates returned KeysetIds and formats Keys to list.
    /// </summary>
    /// <returns>List of Keys (lists :))</returns>
    /// <exception cref="ArgumentNullException">May be thrown if mint is not set.</exception>
    /// <exception cref="ArgumentException">May be thrown if mint returns invalid keysetId for at least one Keyset</exception>
    private async Task<List<GetKeysResponse.KeysetItemResponse>> _fetchKeys(CancellationToken ct = default)
    {
        _ensureApiConnected("Can't fetch keys without mint api!");
        var keysRaw = await _mintApi!.GetKeys(ct);
        foreach (var keysetItemResponse in keysRaw.Keysets)
        {
            var isKeysetIdValid = keysetItemResponse.Keys.VerifyKeysetId(keysetItemResponse.Id, keysetItemResponse.Unit, keysetItemResponse.FinalExpiry);
            if (!isKeysetIdValid)
            {
                throw new ArgumentException($"Mint provided invalid keysetId. Provided: {keysetItemResponse.Id}, derived: {keysetItemResponse.Keys.GetKeysetId()} ");
            }
        }
        return keysRaw.Keysets.ToList();
    }
    
    /// <summary>
    /// Wrapper for GetKeys api endpoint. Validates KeysetId and fetches keys for single KeysetId Formats Keys to list. 
    /// </summary>
    /// <param name="id">KeysetId we want fetch keys for.</param>
    /// <returns>Keys</returns>
    /// <exception cref="ArgumentException">May be thrown if mint returns invalid keysetId for at least one Keyset</exception>
    /// <exception cref="ArgumentNullException">May be thrown if mint is not set.</exception>
    private async Task<GetKeysResponse.KeysetItemResponse> _fetchKeys(KeysetId id, CancellationToken cts = default)
    {
        _ensureApiConnected("Can't fetch keys without mint api!");
        var keysRaw = (await _mintApi!.GetKeys(id, cts)).Keysets.Single();
        
        var isKeysetIdValid = keysRaw.Keys.VerifyKeysetId(keysRaw.Id, keysRaw.Unit, keysRaw.FinalExpiry);
        if (!isKeysetIdValid)
        {
            throw new ArgumentException($"Mint provided invalid keysetId. Provided: {keysRaw.Id}, derived: {keysRaw.Keys.GetKeysetId()} ");
        }

        return keysRaw;
    }
    
    /// <summary>
    /// Wrapper for GetInfo api endpoint. Translates Payload to MintInfo.
    /// </summary>
    /// <exception cref="ArgumentNullException">May be thrown if mint is not set.</exception>
    private async Task<MintInfo> _fetchMintInfo(CancellationToken cts = default)
    {
        _ensureApiConnected("Can't fetch mint info without mint api!");
        var infoRaw = await _mintApi!.GetInfo(cts);
        return new MintInfo(infoRaw);
    }

    /// <summary>
    /// Fetches mint info if not present in CashuWallet.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    private async Task<MintInfo> _lazyFetchMintInfo(CancellationToken cts = default)
    {
        if (this._info != null) return this._info;
        return await this._fetchMintInfo(cts);
    }
    
    /// <summary>
    /// Local Keys sync. 
    /// </summary>
    /// <param name="cts"></param>
    /// <exception cref="ArgumentNullException"></exception>
    internal async Task _maybeSyncKeys(CancellationToken cts = default)
    {
        if (!_shouldSyncKeyset)
        {
            return;
        }
        // should sync keysets SINGLE time in the lifespan of object. If already synced - return;
        if (_syncThresold == null && _lastSync != DateTime.MinValue)
        { 
            return;
        }
        // should sync keysets in some timepsan 
        if (_syncThresold != null && _lastSync + _syncThresold >= DateTime.Now)
        {
            return;
        }

        this._keysets = await _fetchKeysets(cts);
        if (_keys == null)
        {
            this._keys = await _fetchKeys(cts); // we're fetching all keys here, so no need for additional check.
            return;
        }
        
        var knownIds = _keys.Select(key => key.Id).ToHashSet();
        var unknownKeysets = _keysets.Where(k => !knownIds.Contains(k.Id)).ToList();

        if (unknownKeysets.Count > 2) // just make a single request. May override stored keys.
        {
            this._keys = await _fetchKeys(cts);
            return;
        }
        
        foreach (var unknownKeyset in unknownKeysets)
        {
            var keyset = await this._fetchKeys(unknownKeyset.Id, cts); 
            this._keys.Add(keyset);
        }
        
        _lastSync = DateTime.Now;
    }

}

