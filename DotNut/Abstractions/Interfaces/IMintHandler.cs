using DotNut.Abstractions.Websockets;

namespace DotNut.Abstractions;

public interface IMintHandler {}
public interface IMintHandler<TResponse>: IMintHandler
{
    Task<TResponse> Mint(CancellationToken cts = default);
    Task<Subscription> Subscribe();
}