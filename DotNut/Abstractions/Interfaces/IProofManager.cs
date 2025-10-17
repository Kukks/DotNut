namespace DotNut.Abstractions.Interfaces;

public interface IProofManager
{ 
    Task AddProofAsync(Proof proof, CancellationToken ct = default);
    Task<List<Proof>> GetProofsForKeysetId(KeysetId ids, CancellationToken ct = default);
    Task<List<Proof>> GetProofsForMint(List<KeysetId> ids, CancellationToken ct = default); // should still query proofs based on keysetid
    Task MarkProofAsSpent(Proof proof, CancellationToken ct = default);
}