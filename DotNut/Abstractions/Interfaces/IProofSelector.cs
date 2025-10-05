namespace DotNut.Abstractions;

public interface IProofSelector
{
    public Task<SendResponse> SelectProofsToSend(List<Proof> proofs, ulong amountToSend, bool includeFees = false, CancellationToken cts = default);
}