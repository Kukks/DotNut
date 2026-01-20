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

    public string ToBech32String()
    {
        return PaymentRequestBech32Encoder.Encode(this);
    }

    public static PaymentRequest Parse(string creq)
    {
        if (creq.StartsWith("creqA", StringComparison.InvariantCultureIgnoreCase))
        {
            var data = Base64UrlSafe.Decode(creq.Substring(5));
            return PaymentRequestEncoder.Instance.FromCBORObject(CBORObject.DecodeFromBytes(data));
        }

        if (creq.StartsWith("creqB", StringComparison.InvariantCultureIgnoreCase))
        {
            return PaymentRequestBech32Encoder.Decode(creq);
        }

        throw new FormatException("Invalid payment request");
    }
}