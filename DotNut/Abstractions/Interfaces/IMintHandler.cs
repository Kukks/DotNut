using DotNut.Abstractions.Websockets;

namespace DotNut.Abstractions;

public interface IMintHandler;
public interface IMintHandler<TQuote, TResponse>: IMintHandler
{
    Task<TQuote> GetQuote(CancellationToken cts = default);
    Task<TResponse> Mint(CancellationToken cts = default);
    Task<Subscription> Subscribe(CancellationToken cts = default);
}