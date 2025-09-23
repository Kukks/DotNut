namespace DotNut;

public class OutputData
{
    public BlindedMessage[] BlindedMessages { get; set; }
    public ISecret[] Secrets { get; set; }
    public PrivKey[] BlindingFactors { get; set; }
}