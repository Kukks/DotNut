using PeterO.Cbor;

namespace DotNut;

public class PaymentRequestEncoder : ICBORToFromConverter<PaymentRequest>
{
    public static readonly PaymentRequestEncoder Instance = new();

    public CBORObject ToCBORObject(PaymentRequest paymentRequest)
    {
        var cbor = CBORObject.NewMap();
        if (paymentRequest.PaymentId is not null)
            cbor.Add("i", paymentRequest.PaymentId);
        if (paymentRequest.Amount is not null)
            cbor.Add("a", paymentRequest.Amount);
        if (paymentRequest.Unit is not null)
            cbor.Add("u", paymentRequest.Unit);
        if (paymentRequest.OneTimeUse is not null)
            cbor.Add("s", paymentRequest.OneTimeUse);
        if (paymentRequest.Mints is not null)
            cbor.Add("m", paymentRequest.Mints);
        if (paymentRequest.Memo is not null)
            cbor.Add("d", paymentRequest.Memo);
        var transports = CBORObject.NewArray();
        foreach (var transport in paymentRequest.Transports)
        {
            var transportItem = CBORObject.NewMap()
                .Add("t", transport.Type)
                .Add("a", transport.Target);
            if (transport.Tags is not null)
            {
                var tags = CBORObject.NewArray();
                foreach (var tag in transport.Tags)
                {
                    var tagItem = CBORObject.NewArray();
                    tagItem.Add(tag.Key);
                    tagItem.Add(tag.Value);
                    tags.Add(tagItem);
                }

                transportItem.Add("g", tags);
            }

            transports.Add(transportItem);
        }

        cbor.Add("t", transports);
        return cbor;
    }

    public PaymentRequest FromCBORObject(CBORObject obj)
    {
        var paymentRequest = new PaymentRequest();
        foreach (var key in obj.Keys)
        {
            var value = obj[key];
            switch (key.AsString())
            {
                case "i":
                    paymentRequest.PaymentId = value.AsString();
                    break;
                case "a":
                    paymentRequest.Amount = value.AsInt32();
                    break;
                case "u":
                    paymentRequest.Unit = value.AsString();
                    break;
                case "s":
                    paymentRequest.OneTimeUse = value.AsBoolean();
                    break;
                case "m":
                    paymentRequest.Mints = value.Values.Select(v => v.AsString()).ToArray();
                    break;
                case "d":
                    paymentRequest.Memo = value.AsString();
                    break;
                case "t":
                    paymentRequest.Transports = value.Values.Select(v =>
                    {
                        var transport = new PaymentRequestTransport();
                        foreach (var transportKey in v.Keys)
                        {
                            var transportValue = v[transportKey];
                            switch (transportKey.AsString())
                            {
                                case "t":
                                    transport.Type = transportValue.AsString();
                                    break;
                                case "a":
                                    transport.Target = transportValue.AsString();
                                    break;
                                case "g":
                                    transport.Tags = transportValue.Values.Select(tag =>
                                    {
                                        var tagItem = new PaymentRequestTransportTag
                                        {
                                            Key = tag[0].AsString(),
                                            Value = tag[1].AsString()
                                        };
                                        return tagItem;
                                    }).ToArray();
                                    break;
                            }
                        }

                        return transport;
                    }).ToArray();
                    break;
            }
        }

        return paymentRequest;
    }
}