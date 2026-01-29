namespace DotNut.Abstractions;

public interface IProofSelector
{
    Task<SendResponse> SelectProofsToSend(
        IEnumerable<Proof> proofsToSelectFrom,
        ulong amountToSend,
        bool includeFees = false,
        CancellationToken ct = default
    );
}
