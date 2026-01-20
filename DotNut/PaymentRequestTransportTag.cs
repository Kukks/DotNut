namespace DotNut;

public class PaymentRequestTransportTag
{
    public string Key { get; set; }
    public string Value { get; set; }

    public string[] toArray()
    {
        return new[] { Key, Value };
    }
}