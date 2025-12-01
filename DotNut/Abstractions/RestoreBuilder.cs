using DotNut.ApiModels;
using DotNut.NBitcoin.BIP39;
using DotNut.NUT13;

namespace DotNut.Abstractions;

public class RestoreBuilder : IRestoreBuilder
{
    private readonly Wallet _wallet;
    private List<KeysetId>? _specifiedKeysets;

    private bool _shouldSwap = true;

    public RestoreBuilder(Wallet wallet)
    {
        this._wallet = wallet;
    }
    
    public IRestoreBuilder ForKeysetIds(IEnumerable<KeysetId> keysetIds)
    {
        this._specifiedKeysets = keysetIds.ToList();
        return this;
    }

    public IRestoreBuilder WithSwap(bool shouldSwap = true)
    {
        this._shouldSwap = shouldSwap;
        return this;
    }

    public async Task<IEnumerable<Proof>> ProcessAsync(CancellationToken ct = default)
    {
        var api = await _wallet.GetMintApi(ct);
        await _wallet._maybeSyncKeys(ct);
        
        var mnemonic = _wallet.GetMnemonic()??
                       throw new ArgumentNullException(nameof(Mnemonic), "Can't restore wallet without Mnemonic");
        
        _specifiedKeysets ??= 
            (await _wallet.GetKeysets(ct: ct)).Select(k => k.Id).ToList();

        if (_specifiedKeysets == null || _specifiedKeysets.Count == 0)
        {
            throw new InvalidOperationException("No keysets available for restoration. Ensure the mint has at least one keyset or specify keysets explicitly.");
        }
        
        var counter = _wallet.GetCounter();
        if (counter == null)
        {
            _wallet.WithCounter(new InMemoryCounter());
            counter = _wallet.GetCounter();
        }

        List<Proof> recoveredProofs = new List<Proof>();
        foreach (var keysetId in _specifiedKeysets)
        {
            int batchNumber = 0;
            int emptyBatchesRemaining = 3;

            GetKeysResponse.KeysetItemResponse? keyset;

            try
            {
                keyset = await _wallet.GetKeys(keysetId, true, false, ct);
            }
            catch (Exception e)
            {
                continue;
            }

            if (keyset == null)
            {
                continue;
            }
            
            while (emptyBatchesRemaining > 0)
            {
                var outputs = await _createBatch(mnemonic, keysetId, batchNumber, ct);
                var req = new PostRestoreRequest
                {
                    Outputs = outputs.Select(o=>o.BlindedMessage).ToArray()
                };
                var res = await api.Restore(req, ct);
                await counter!.IncrementCounter(keysetId, 100, ct);

                if (!res.Signatures.Any())
                {
                    emptyBatchesRemaining--;
                }

                var proofs = Utils.ConstructProofsFromPromises(res.Signatures.ToList(), outputs, keyset.Keys);
                recoveredProofs.AddRange(proofs);
                batchNumber++;
            }
            
        }
        
        if (!this._shouldSwap || !recoveredProofs.Any())
        {
            return recoveredProofs;
        }
        
        var freshProofs = new List<Proof>();
        
        // create hash table for every KeysetId : unit. 
        var allKeysetsUnits = await _wallet.GetKeysetIdsWithUnits(ct);
        var unitsForKeysets = new Dictionary<KeysetId, string>();
        if (allKeysetsUnits == null)
        {
            throw new InvalidOperationException("No keysets available for restoration.");
        }
        foreach (var unit in allKeysetsUnits)
        {
            foreach (var keysetId in unit.Value)
            {
                unitsForKeysets.Add(keysetId, unit.Key);
            }
        }
        
        var activeUnits = await this._wallet.GetActiveKeysetIdsWithUnits(ct);
        if (activeUnits == null || !activeUnits.Any())
        {
            throw new InvalidOperationException("Could not restore wallet without active keysets");
        }

        foreach (var unitKeyset in activeUnits)
        {
            var correspondingKeys = await _wallet.GetKeys(unitKeyset.Value, true, false, ct);
            
            var unit = unitKeyset.Key;
            var proofsForUnit = recoveredProofs
                .Where(p => unitsForKeysets.TryGetValue(p.Id, out var proofUnit) && proofUnit == unit)
                .ToList();
            if (!proofsForUnit.Any()) continue;
            var totalAmount = proofsForUnit.Select(p => p.Amount).Sum();
            
            var amounts = Utils.SplitToProofsAmounts(totalAmount, correspondingKeys.Keys);
            var ctr = await counter!.GetCounterForId(unitKeyset.Value, ct);
            var newOutputs = Utils.CreateOutputs(amounts, unitKeyset.Value, correspondingKeys.Keys, mnemonic, ctr);
            await counter.IncrementCounter(unitKeyset.Value, newOutputs.Select(o=>o.BlindedMessage).Count(), ct);
            
            var swapRequest = new PostSwapRequest
            {
                Inputs = proofsForUnit.ToArray(),
                Outputs = newOutputs.Select(o=>o.BlindedMessage).ToArray(),
            };
        
            var swapResult = await api.Swap(swapRequest, ct);
            var constructedProofs = Utils.ConstructProofsFromPromises(swapResult.Signatures.ToList(), newOutputs, correspondingKeys.Keys);
            
            freshProofs.AddRange(constructedProofs);
        }
        return freshProofs;
    }

    private async Task<List<OutputData>> _createBatch(Mnemonic mnemonic, KeysetId keysetId, int batchNumber, CancellationToken ct)
    {
        var amounts = Enumerable.Repeat((ulong)1, 100).ToList();
        return mnemonic.DeriveOutputs(amounts, keysetId, batchNumber*100);
    }
}