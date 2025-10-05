namespace DotNut.Abstractions.Interfaces;

public interface IProofManager
{ 
    Task AddProofAsync(Proof proof, CancellationToken cts = default);
    Task<List<Proof>> GetProofsForKeysetId(KeysetId ids, CancellationToken cts = default);
    Task<List<Proof>> GetProofsForMint(List<KeysetId> ids, CancellationToken cts = default); // should still query proofs based on keysetid
    Task MarkProofAsSpent(Proof proof, CancellationToken cts = default);
}