using System.Collections.Concurrent;

namespace DotNut;

public class PaymentRequestTransportInitiator
{
    private readonly IEnumerable<PaymentRequestInterfaceHandler> _handlers;
    public static ConcurrentBag<PaymentRequestInterfaceHandler> Handlers { get; } = [ new HttpPaymentRequestInterfaceHandler(null) ];
    public PaymentRequestTransportInitiator(IEnumerable<PaymentRequestInterfaceHandler> handlers)
    {
        _handlers = handlers;
    }

    public PaymentRequestTransportInitiator()
    {
        _handlers = Handlers.ToArray();
    }
}