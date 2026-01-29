using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DotNut.ApiModels;

namespace DotNut.Api;

public class CashuHttpClient : ICashuApi
{
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;

    public CashuHttpClient(HttpClient httpClient, bool ownsHttpClient = false)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(httpClient.BaseAddress);
        _httpClient = httpClient;
        _ownsHttpClient = ownsHttpClient;
    }

    public string GetBaseUrl()
    {
        ArgumentNullException.ThrowIfNull(_httpClient.BaseAddress);
        return _httpClient.BaseAddress.AbsoluteUri;
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

    public async Task<GetKeysResponse> GetKeys(
        KeysetId keysetId,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _httpClient.GetAsync($"v1/keys/{keysetId}", cancellationToken);
        return await HandleResponse<GetKeysResponse>(response, cancellationToken);
    }

    public async Task<PostSwapResponse> Swap(
        PostSwapRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _httpClient.PostAsync(
            $"v1/swap",
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"),
            cancellationToken
        );
        return await HandleResponse<PostSwapResponse>(response, cancellationToken);
    }

    public async Task<TResponse> CreateMintQuote<TResponse, TRequest>(
        string method,
        TRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _httpClient.PostAsync(
            $"v1/mint/quote/{method}",
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"),
            cancellationToken
        );
        return await HandleResponse<TResponse>(response, cancellationToken);
    }

    public async Task<TResponse> CreateMeltQuote<TResponse, TRequest>(
        string method,
        TRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _httpClient.PostAsync(
            $"v1/melt/quote/{method}",
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"),
            cancellationToken
        );
        return await HandleResponse<TResponse>(response, cancellationToken);
    }

    public async Task<TResponse> Melt<TResponse, TRequest>(
        string method,
        TRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _httpClient.PostAsync(
            $"v1/melt/{method}",
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"),
            cancellationToken
        );
        return await HandleResponse<TResponse>(response, cancellationToken);
    }

    public async Task<TResponse> CheckMeltQuote<TResponse>(
        string method,
        string quoteId,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _httpClient.GetAsync(
            $"v1/melt/quote/{method}/{quoteId}",
            cancellationToken
        );
        return await HandleResponse<TResponse>(response, cancellationToken);
    }

    public async Task<TResponse> CheckMintQuote<TResponse>(
        string method,
        string quoteId,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _httpClient.GetAsync(
            $"v1/mint/quote/{method}/{quoteId}",
            cancellationToken
        );
        return await HandleResponse<TResponse>(response, cancellationToken);
    }

    public async Task<TResponse> Mint<TRequest, TResponse>(
        string method,
        TRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _httpClient.PostAsync(
            $"v1/mint/{method}",
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"),
            cancellationToken
        );
        return await HandleResponse<TResponse>(response, cancellationToken);
    }

    public async Task<PostCheckStateResponse> CheckState(
        PostCheckStateRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _httpClient.PostAsync(
            $"v1/checkstate",
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"),
            cancellationToken
        );
        return await HandleResponse<PostCheckStateResponse>(response, cancellationToken);
    }

    public async Task<PostRestoreResponse> Restore(
        PostRestoreRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _httpClient.PostAsync(
            $"v1/restore",
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"),
            cancellationToken
        );
        return await HandleResponse<PostRestoreResponse>(response, cancellationToken);
    }

    public async Task<GetInfoResponse> GetInfo(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("v1/info", cancellationToken);
        return await HandleResponse<GetInfoResponse>(response, cancellationToken);
    }

    protected async Task<T> HandleResponse<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken
    )
    {
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var error = await response.Content.ReadFromJsonAsync<CashuProtocolError>(
                cancellationToken: cancellationToken
            );
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

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }
}
