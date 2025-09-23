namespace DotNut.Abstractions;

public class SendResponse
{
    public List<Proof> Keep { get; set; } = new();
    public List<Proof> Send { get; set; } = new();
}