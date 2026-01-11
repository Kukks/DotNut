using DotNut.Api;
using DotNut.ApiModels;
using DotNut.NBitcoin.BIP39;
using DotNut.NUT13;

namespace DotNut.Abstractions;

public class RestoreBuilder : IRestoreBuilder
{
    private readonly Wallet _wallet;
    private List<KeysetId>? _specifiedKeysets;
    private static uint BATCH_SIZE = 100;
    private static uint EMPTY_BATCHES_ALLOWED = 3;

    public RestoreBuilder(Wallet wallet)
    {
        this._wallet = wallet;
    }

    public IRestoreBuilder FromKeysetIds(IEnumerable<KeysetId> keysetIds)
    {
        this._specifiedKeysets = keysetIds.ToList();
        return this;
    }

    public async Task<IEnumerable<Proof>> ProcessAsync(CancellationToken ct = default)
    {
        var api = await _wallet.GetMintApi(ct);
        await _wallet._maybeSyncKeys(ct);
        var mnemonic =
            _wallet.GetMnemonic()
            ?? throw new ArgumentNullException(
                nameof(Mnemonic),
                "Can't restore wallet without Mnemonic"
            );

        // keyset ids we want to grind our counter on
        _specifiedKeysets ??= (await _wallet.GetKeysets(ct: ct)).Select(k => k.Id).ToList();
        if (_specifiedKeysets == null || _specifiedKeysets.Count == 0)
        {
            throw new InvalidOperationException(
                "No keysets available for restoration. Ensure the mint has at least one active keyset or specify keysets explicitly."
            );
        }

        var counter = _wallet.GetCounter();
        if (counter == null)
        {
            throw new ArgumentNullException(nameof(counter), "Counter cannot be null.");
        }

        // fetch all batches
        List<Proof> recoveredProofs = new List<Proof>();
        foreach (var keysetId in _specifiedKeysets)
        {
            var keysetProofs = await GrindKeyset(keysetId, mnemonic, counter, api, ct);
            recoveredProofs.AddRange(keysetProofs);
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
                unit.Value.Select(keysetId => new { KeysetId = keysetId, Unit = unit.Key })
            )
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
                .Where(p =>
                    unitsForKeysets.TryGetValue(p.Id, out var proofUnit) && proofUnit == unit
                )
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

    private static List<OutputData> CreateBatch(
        Mnemonic mnemonic,
        KeysetId keysetId,
        int batchNumber,
        CancellationToken ct
    )
    {
        if (batchNumber < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchNumber));
        }
        var amounts = Enumerable.Repeat((ulong)0, (int)BATCH_SIZE).ToList();
        return mnemonic.DeriveOutputs(amounts, keysetId, (uint)(batchNumber * BATCH_SIZE));
    }

    private async Task<List<Proof>> GrindKeyset(
        KeysetId keysetId,
        Mnemonic mnemonic,
        ICounter counter,
        ICashuApi api,
        CancellationToken ct
    )
    {
        uint batchNumber = 0;
        uint emptyBatchesRemaining = EMPTY_BATCHES_ALLOWED;
        uint lastUsedCounter = 0;
        List<Proof> recoveredProofs = new List<Proof>();

        // don't care about invalid / non existent source keyset ids. let's fetch what we can
        GetKeysResponse.KeysetItemResponse? keyset;
        try
        {
            keyset = await _wallet.GetKeys(keysetId, true, false, ct);
        }
        catch
        {
            return [];
        }
        if (keyset == null)
        {
            return [];
        }

        // proofs for keysetid are considered restored after 3 empty batches.
        while (emptyBatchesRemaining > 0)
        {
            // create batch of 100, and request restore for whole batch
            var outputs = CreateBatch(mnemonic, keysetId, (int)batchNumber, ct);
            var req = new PostRestoreRequest
            {
                Outputs = outputs.Select(o => o.BlindedMessage).ToArray(),
            };
            var res = await api.Restore(req, ct);

            if (res.Signatures.Length == 0)
            {
                emptyBatchesRemaining--;
                batchNumber++;
                continue;
            }

            // find last restored index of batch
            uint lastUsedIndexInBatch = (uint)
                outputs
                    .Select((o, i) => new { o, i })
                    .Where(x => res.Outputs.Any(r => Equals(r.B_, x.o.BlindedMessage.B_)))
                    .MaxBy(x => x.i)!
                    .i;

            // set last used counter value for this batch
            lastUsedCounter = BATCH_SIZE * batchNumber + lastUsedIndexInBatch;

            // bump batch number after calculating last used counter
            batchNumber++;

            // if anything found, reset batches counter
            emptyBatchesRemaining = EMPTY_BATCHES_ALLOWED;

            var returnedOutputs = new List<OutputData>();
            foreach (var output in res.Outputs)
            {
                // there can't be any dupes here
                var matchingOutputs = outputs.SingleOrDefault(o => Equals(o.BlindedMessage.B_, output.B_));
                if (matchingOutputs == null)
                {
                    throw new InvalidOperationException("Invalid outputs returned by mint!");
                }
                returnedOutputs.Add(matchingOutputs);
            }

            var proofs = Utils.ConstructProofsFromPromises(
                res.Signatures.ToList(),
                returnedOutputs,
                keyset.Keys
            );
            recoveredProofs.AddRange(proofs);
        }

        // 1 is added so we'll be consistent with counter usage. it will be ready for next use
        await counter.SetCounter(keysetId, lastUsedCounter + 1, ct);
        return recoveredProofs;
    }
    // in future it may be also usefult to add restore by binary search
}
