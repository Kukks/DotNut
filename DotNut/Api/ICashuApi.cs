using DotNut.ApiModels;

namespace DotNut.Api;

public interface ICashuApi
{
    string GetBaseUrl();
    Task<GetKeysResponse> GetKeys(CancellationToken cancellationToken = default);
    Task<GetKeysResponse> GetKeys(KeysetId keysetId, CancellationToken cancellationToken = default);
    Task<GetKeysetsResponse> GetKeysets(CancellationToken cancellationToken = default);
    Task<PostSwapResponse> Swap(PostSwapRequest request, CancellationToken cancellationToken = default);

    Task<TResponse> CreateMintQuote<TResponse, TRequest>(string method, TRequest request, CancellationToken
        cancellationToken = default);

    Task<TResponse> CreateMeltQuote<TResponse, TRequest>(string method, TRequest request, CancellationToken
        cancellationToken = default);

    Task<TResponse> Melt<TResponse, TRequest>(string method, TRequest request, CancellationToken
        cancellationToken = default);

    Task<TResponse> CheckMintQuote<TResponse>(string method, string quoteId, CancellationToken
        cancellationToken = default);

    Task<TResponse> Mint<TRequest,TResponse>(string method, TRequest request,  CancellationToken cancellationToken = default);
    Task<PostCheckStateResponse> CheckState(PostCheckStateRequest request,  CancellationToken cancellationToken = default);
    Task<PostRestoreResponse> Restore(PostRestoreRequest request,  CancellationToken cancellationToken = default);
    Task<GetInfoResponse> GetInfo(CancellationToken cancellationToken = default);
}