namespace DotNut.Abstractions;

public class OutputData
{
    public BlindedMessage BlindedMessage { get; set; }
    public ISecret Secret { get; set; }
    public PrivKey BlindingFactor { get; set; }

    public PubKey? P2BkE { get; set; }
}
