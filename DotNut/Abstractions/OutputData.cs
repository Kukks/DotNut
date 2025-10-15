namespace DotNut;

public class OutputData
{
    public List<BlindedMessage> BlindedMessages { get; set; } = [];
    public List<ISecret> Secrets { get; set; } = [];
    public List<PrivKey> BlindingFactors { get; set; } = [];
}