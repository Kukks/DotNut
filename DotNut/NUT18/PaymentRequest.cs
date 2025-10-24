using PeterO.Cbor;

namespace DotNut;

public class PaymentRequest
{
    public string? PaymentId { get; set; }
    public ulong? Amount { get; set; }
    public string? Unit { get; set; }
    public bool? OneTimeUse { get; set; }
    public string[]? Mints { get; set; }
    public string? Memo { get; set; }
    public PaymentRequestTransport[] Transports { get; set; }
    public Nut10LockingCondition? Nut10 { get; set; }

    public override string ToString()
    {
        var obj = PaymentRequestEncoder.Instance.ToCBORObject(this);
        return $"creqA{Base64UrlSafe.Encode(obj.EncodeToBytes())}";
    }

    public static PaymentRequest Parse(string creqA)
    {
        if (!creqA.StartsWith("creqA", StringComparison.InvariantCultureIgnoreCase))
        {
            throw new FormatException("Invalid payment request");
        }

        var data = Base64UrlSafe.Decode(creqA.Substring(5));
        return PaymentRequestEncoder.Instance.FromCBORObject(CBORObject.DecodeFromBytes(data));
    }
}