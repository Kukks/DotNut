using System.Net.Http.Json;

namespace DotNut;

public class HttpPaymentRequestInterfaceHandler : PaymentRequestInterfaceHandler
{
    private readonly HttpClient _httpClient;

    public HttpPaymentRequestInterfaceHandler(HttpClient? httpClient)
    {
        _httpClient = httpClient ?? new HttpClient();
    }
    public bool CanHandle(PaymentRequest request)
    {
        return request.Transports.Any(t => t.Type == "post");
    }

    public async Task SendPayment(PaymentRequest request, PaymentRequestPayload payload,
        CancellationToken cancellationToken = default)
    { 
        var endpoint = new Uri(request.Transports.First(t => t.Type == "post").Target);
        var response = await _httpClient.PostAsJsonAsync(endpoint, payload,cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}