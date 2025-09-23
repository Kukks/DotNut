namespace DotNut.Abstractions;

public interface IProofSelector
{
    public SendResponse SelectProofsToSend(List<Proof> proofs, ulong amountToSend, bool includeFees = false);
}