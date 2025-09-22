namespace DotNut;

public interface PaymentRequestInterfaceHandler
{
    bool CanHandle(PaymentRequest request);
    Task SendPayment(PaymentRequest request, PaymentRequestPayload payload,  CancellationToken cancellationToken = default);
}