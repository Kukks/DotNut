namespace DotNut.Abstractions;

public interface IProofSelector
{
    Task<SendResponse> SelectProofsToSend(
        List<Proof> proofs,
        ulong amountToSend,
        bool includeFees = false,
        CancellationToken ct = default
    );
}
