using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using NBitcoin.Secp256k1;

namespace DotNuts;

public class CashuHttpClient
{
    private readonly HttpClient _httpClient;

    public CashuHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<GetKeysResponse> GetKeys(CancellationToken cancellationToken = default)

    {
        var response = await _httpClient.GetAsync("v1/keys", cancellationToken);
        return await HandleResponse<GetKeysResponse>(response, cancellationToken);
    }

    public async Task<GetKeysetsResponse> GetKeysets(CancellationToken cancellationToken = default)

    {
        var response = await _httpClient.GetAsync("v1/keysets", cancellationToken);
        return await HandleResponse<GetKeysetsResponse>(response, cancellationToken);
    }

    public async Task<GetKeysetsResponse> GetKeys(string keysetId, CancellationToken cancellationToken = default)

    {
        var response = await _httpClient.GetAsync($"v1/keys/{keysetId}", cancellationToken);
        return await HandleResponse<GetKeysetsResponse>(response, cancellationToken);
    }
    public async Task<PostSwapResponse> Swap(PostSwapRequest request, CancellationToken cancellationToken = default)

    {
        var response = await _httpClient.PostAsync($"v1/swap", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"), cancellationToken);
        return await HandleResponse<PostSwapResponse>(response, cancellationToken);
    }
    
    public async Task<TResponse> CreateMintQuote<TResponse, TRequest>(string method, TRequest request, CancellationToken
        cancellationToken = default)
    {
        var response = await _httpClient.PostAsync($"v1/mint/quote/{method}", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"), cancellationToken);
        return await HandleResponse<TResponse>(response, cancellationToken);
    }
    
    public async Task<TResponse> CheckMintQuote<TResponse>(string method, string quoteId, CancellationToken
        cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"v1/mint/quote/{method}/{quoteId}", cancellationToken);
        return await HandleResponse<TResponse>(response, cancellationToken);
    }    
    
    public async Task<TResponse> Mint<TRequest,TResponse>(string method, TRequest request,  CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync($"v1/mint/{method}", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"), cancellationToken);
        return await HandleResponse<TResponse>(response, cancellationToken);
    }

    protected async Task<T> HandleResponse<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var error =
                await response.Content.ReadFromJsonAsync<CashuProtocolError>(cancellationToken: cancellationToken);
            throw new CashuProtocolException(error);
        }

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
        if (result is null)
        {
            var t = typeof(T);
            if (Nullable.GetUnderlyingType(t) != null)
            {
                return result!;
            }
        }

        return result!;
    }
}

public class PostSwapRequest
{
    [JsonPropertyName("inputs")] public Proof[] Inputs { get; set; }
    [JsonPropertyName("outputs")] public BlindedMessage[] Outputs { get; set; }
}
public class PostSwapResponse
{
    [JsonPropertyName("signatures")] public BlindSignature[] Signatures { get; set; }
}

public class GetKeysResponse
{
    [JsonPropertyName("keysets")] public Keyset[] Keysets { get; set; }

    public class Keyset
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("unit")] public string Unit { get; set; }
        [JsonPropertyName("keys")] public Dictionary<int, string> Keys { get; set; }
    }
}

public class GetKeysetsResponse
{
    [JsonPropertyName("keysets")] public Keyset[] Keysets { get; set; }

    public class Keyset
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("unit")] public string Unit { get; set; }
        [JsonPropertyName("active")] public bool Active { get; set; }
    }
}