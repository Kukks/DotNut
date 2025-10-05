namespace DotNut.Abstractions.Interfaces;

public class InMemoryProofManager: IProofManager
{
    private Dictionary<KeysetId, List<Proof>> _proofsDictionary = new();
    
    public async Task AddProofAsync(Proof proof, CancellationToken cts = default)
    {
        if (_proofsDictionary.TryGetValue(proof.Id, out var proofs))
        {
            proofs.Add(proof);
            return;
        }
        _proofsDictionary.Add(proof.Id, new List<Proof> { proof });
    }

    public async Task<List<Proof>> GetProofsForKeysetId(KeysetId ids, CancellationToken cts = default)
    {
        return _proofsDictionary.TryGetValue(ids, out var proofs) ? proofs : new List<Proof>();
    }

    public Task<List<Proof>> GetProofsForMint(List<KeysetId> ids, CancellationToken cts = default)
    {
        throw new NotImplementedException();
    }
    public Task MarkProofAsSpent(Proof proof, CancellationToken cts = default)
    {
        throw new NotImplementedException();
    }
}