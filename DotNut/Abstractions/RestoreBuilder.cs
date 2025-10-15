using DotNut.Abstractions.Interfaces;
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
    
    public RestoreBuilder ForKeysetIds(IEnumerable<KeysetId> keysetIds)
    {
        this._specifiedKeysets = keysetIds.ToList();
        return this;
    }

    public IRestoreBuilder WithSwap(bool shouldSwap = true)
    {
        this._shouldSwap = shouldSwap;
        return this;
    }

    public async Task<IEnumerable<Proof>> ProcessAsync(CancellationToken cts = default)
    {
        var api = await _wallet.GetMintApi();
        await _wallet._maybeSyncKeys(cts);
        
        var mnemonic = _wallet.GetMnemonic()??
                       throw new ArgumentNullException("Can't restore wallet without Mnemonic");
        
        _specifiedKeysets ??= 
            (await _wallet.GetKeysets(cts: cts)).Select(k => k.Id).ToList();

        if (_specifiedKeysets == null || _specifiedKeysets.Count == 0)
        {
            throw new ArgumentNullException(nameof(_specifiedKeysets));
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

            var keyset = await _wallet.GetKeys(keysetId, false, cts);
            
            while (emptyBatchesRemaining > 0)
            {
                var outputs = await _createBatch(mnemonic, keysetId, batchNumber, cts);
                await counter!.IncrementCounter(keysetId, batchNumber * 100);
                var req = new PostRestoreRequest
                {
                    Outputs = outputs.BlindedMessages.ToArray()
                };
                var res = await api.Restore(req, cts);

                if (!res.Signatures.Any())
                {
                    emptyBatchesRemaining--;
                }

                var proofs = CashuUtils.ConstructProofsFromPromises(res.Signatures.ToList(), outputs, keyset.Keys);
                recoveredProofs.AddRange(proofs);
                batchNumber++;
            }
            
        }
        
        if (!this._shouldSwap || !recoveredProofs.Any())
        {
            return recoveredProofs;
        }
        
        var freshProofs = new List<Proof>();
        var activeUnits = await this._wallet.GetActiveKeysetIdsWithUnits();
        
        if (activeUnits == null || !activeUnits.Any())
        {
            throw new InvalidOperationException("Could not restore wallet without active keysets");
        }

        foreach (var unitKeyset in activeUnits)
        {
            var correspondingKeys = await _wallet.GetKeys(unitKeyset.Value, false, cts);
            var totalAmount = recoveredProofs.Select(p=>p.Amount).Aggregate((a,c) => a + c);
            var amounts = CashuUtils.SplitToProofsAmounts(totalAmount, correspondingKeys.Keys);
            var ctr = await counter!.GetCounterForId(unitKeyset.Value, cts);
            var newOutputs = CashuUtils.CreateOutputs(amounts, unitKeyset.Value, correspondingKeys.Keys, mnemonic, ctr);
            await counter.IncrementCounter(unitKeyset.Value, newOutputs.BlindedMessages.Count, cts);
            
            var swapRequest = new PostSwapRequest
            {
                Inputs = recoveredProofs.ToArray(),
                Outputs = newOutputs.BlindedMessages.ToArray(),
            };
        
            var swapResult = await api.Swap(swapRequest, cts);
            var constructedProofs = CashuUtils.ConstructProofsFromPromises(swapResult.Signatures.ToList(), newOutputs, correspondingKeys.Keys);
            
            freshProofs.AddRange(constructedProofs);
        }
        return freshProofs;
    }

    private async Task<OutputData> _createBatch(Mnemonic mnemonic, KeysetId keysetId, int batchNubmber, CancellationToken cts)
    {
        var amounts = Enumerable.Repeat((ulong)1, 100).ToList();
        Console.WriteLine(batchNubmber);
        Console.WriteLine($"Where does batch start: {batchNubmber*100}");
        return mnemonic.DeriveOutputs(amounts, keysetId, batchNubmber*100);
    }
}