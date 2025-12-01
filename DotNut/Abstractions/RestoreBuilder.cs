using DotNut.ApiModels;
using DotNut.NBitcoin.BIP39;
using DotNut.NUT13;

namespace DotNut.Abstractions;

public class RestoreBuilder : IRestoreBuilder
{
    private readonly Wallet _wallet;
    private List<KeysetId>? _specifiedKeysets;
    private static int BATCH_SIZE = 100;

    public RestoreBuilder(Wallet wallet)
    {
        this._wallet = wallet;
    }
    
    public IRestoreBuilder ForKeysetIds(IEnumerable<KeysetId> keysetIds)
    {
        this._specifiedKeysets = keysetIds.ToList();
        return this;
    }


    public async Task<IEnumerable<Proof>> ProcessAsync(CancellationToken ct = default)
    {
        var api = await _wallet.GetMintApi(ct);
        await _wallet._maybeSyncKeys(ct);
        var mnemonic = _wallet.GetMnemonic()??
                       throw new ArgumentNullException(nameof(Mnemonic), "Can't restore wallet without Mnemonic");
        
        // keyset ids we want to grind our counter on
        _specifiedKeysets ??= 
            (await _wallet.GetKeysets(ct: ct)).Select(k => k.Id).ToList();
        if (_specifiedKeysets == null || _specifiedKeysets.Count == 0)
        {
            throw new InvalidOperationException("No keysets available for restoration. Ensure the mint has at least one active keyset or specify keysets explicitly.");
        }
        
        // init brand new counter
        _wallet.WithCounter(new InMemoryCounter());
        var counter = _wallet.GetCounter();

        // fetch all batches
        List<Proof> recoveredProofs = new List<Proof>();
        foreach (var keysetId in _specifiedKeysets)
        {
            int batchNumber = 0;
            int emptyBatchesRemaining = 3;
            
            // don't care about invalid / non existent source keyset ids. let's fetch what we can
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
            
            // proofs for keysetid are considered restored after 3 empty batches. 
            while (emptyBatchesRemaining > 0)
            {
                var outputs = await _createBatch(mnemonic, keysetId, batchNumber, ct);
                var req = new PostRestoreRequest
                {
                    Outputs = outputs.Select(o=>o.BlindedMessage).ToArray()
                };
                var res = await api.Restore(req, ct);
                await counter!.IncrementCounter(keysetId, BATCH_SIZE, ct);
                batchNumber++;
                if (res.Signatures.Length == 0)
                {
                    emptyBatchesRemaining--;
                    continue;
                }

                var returnedOutputs = new List<OutputData>();

                foreach (var output in res.Outputs)
                {
                    returnedOutputs.Add(outputs.Single(o=>Equals(o.BlindedMessage.B_, output.B_)));
                }
                
                var proofs = Utils.ConstructProofsFromPromises(res.Signatures.ToList(), returnedOutputs , keyset.Keys);
                recoveredProofs.AddRange(proofs);
            }
            
        }
        
        // if nothing found - return empty collection
        if (recoveredProofs.Count == 0)
        {
            return [];
        }

        var freshProofs = new List<Proof>();
        
        // create hash table for every KeysetId : unit. 
        var allKeysetsUnits = await _wallet.GetKeysetIdsWithUnits(ct);
        if (allKeysetsUnits == null)
        {
            throw new InvalidOperationException("No keysets available for restoration.");
        }
        var unitsForKeysets = allKeysetsUnits
        .SelectMany(unit => 
            unit.Value.Select(keysetId => 
                new { KeysetId = keysetId, Unit = unit.Key }))
        .ToDictionary(x => x.KeysetId, x => x.Unit);
        
        var activeUnits = await this._wallet.GetActiveKeysetIdsWithUnits(ct);
        if (activeUnits == null || !activeUnits.Any())
        {
            throw new InvalidOperationException("Could not restore wallet without active keysets");
        }

        foreach (var unitKeyset in activeUnits)
        {
            var unit = unitKeyset.Key;
            var proofsForUnit = recoveredProofs
                .Where(p => unitsForKeysets.TryGetValue(p.Id, out var proofUnit) && proofUnit == unit)
                .ToList();
            if (proofsForUnit.Count == 0)
            {
                continue;
            }
            
            // check proofs state:
            var unspentProofsForUnit = new List<Proof>();
            var state = await _wallet.CheckState(proofsForUnit, ct);
            for (int i = 0; i < proofsForUnit.Count; i++)
            {
                if (state.States[i].State != StateResponseItem.TokenState.UNSPENT)
                {
                    continue;
                }
                unspentProofsForUnit.Add(proofsForUnit[i]);
            }
            
            // swap unspent tokens to single keyset
            var proofs = await _wallet
                .Swap()
                .ForKeyset(unitKeyset.Value)
                .WithDLEQVerification()
                .FromInputs(unspentProofsForUnit)
                .ProcessAsync(ct);
            
            freshProofs.AddRange(proofs);
        }
        return freshProofs;
    }

    private static async Task<List<OutputData>> _createBatch(Mnemonic mnemonic, KeysetId keysetId, int batchNumber, CancellationToken ct)
    {
        var amounts = Enumerable.Repeat((ulong)0, BATCH_SIZE).ToList();
        return mnemonic.DeriveOutputs(amounts, keysetId, batchNumber*BATCH_SIZE);
    }
}