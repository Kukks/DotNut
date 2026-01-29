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
            var transportItem = CBORObject
                .NewMap()
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

        if (paymentRequest.Nut10 is not null)
        {
            var nut10Obj = CBORObject.NewMap();
            nut10Obj.Add("k", paymentRequest.Nut10.Kind);
            nut10Obj.Add("d", paymentRequest.Nut10.Data);
            if (paymentRequest.Nut10.Tags is not null)
            {
                var tagsArray = CBORObject.NewArray();
                foreach (var tag in paymentRequest.Nut10.Tags)
                {
                    var tagItem = CBORObject.NewArray();
                    if (tag.Length != 2)
                    {
                        throw new ArgumentOutOfRangeException(
                            nameof(tag),
                            "Invalid nut10 tag length"
                        );
                    }
                    tagItem.Add(tag[0]);
                    tagItem.Add(tag[1]);
                    tagsArray.Add(tagItem);
                }
                nut10Obj.Add("t", tagsArray);
            }
            cbor.Add("nut10", nut10Obj);
        }

        if (paymentRequest.Nut26 is { } nut26)
        {
            cbor.Add("nut26", nut26);
        }
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
                    paymentRequest.Amount = value.ToObject<ulong>();
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
                    paymentRequest.Transports = value
                        .Values.Select(v =>
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
                                        transport.Tags = transportValue
                                            .Values.Select(tag =>
                                            {
                                                var tagItem = new PaymentRequestTransportTag
                                                {
                                                    Key = tag[0].AsString(),
                                                    Value = tag[1].AsString(),
                                                };
                                                return tagItem;
                                            })
                                            .ToArray();
                                        break;
                                }
                            }

                            return transport;
                        })
                        .ToArray();
                    break;
                case "nut10":
                    var lockingCondition = new Nut10LockingCondition();
                    foreach (var nut10Key in value.Keys)
                    {
                        var nut10Value = value[nut10Key];
                        switch (nut10Key.AsString())
                        {
                            case "k":
                                lockingCondition.Kind = nut10Value.AsString();
                                break;
                            case "d":
                                lockingCondition.Data = nut10Value.AsString();
                                break;
                            case "t":
                                lockingCondition.Tags = nut10Value
                                    .Values.Select(tagVal =>
                                    {
                                        string[] tag = [tagVal[0].AsString(), tagVal[1].AsString()];
                                        return tag;
                                    })
                                    .ToArray();
                                break;
                        }
                    }
                    paymentRequest.Nut10 = lockingCondition;
                    break;
                case "nut26":
                    paymentRequest.Nut26 = value.AsBoolean();
                    break;
            }
        }
        return paymentRequest;
    }
}
