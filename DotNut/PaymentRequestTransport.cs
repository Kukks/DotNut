namespace DotNut;

public class PaymentRequestTransport
{
    public string Type { get; set; }
    public string Target { get; set; }
    public Tag[] Tags { get; set; }
}