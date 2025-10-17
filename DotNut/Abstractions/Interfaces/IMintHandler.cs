using DotNut.Abstractions.Websockets;

namespace DotNut.Abstractions;

public interface IMintHandler;
public interface IMintHandler<TQuote, TResponse>: IMintHandler
{
    Task<TQuote> GetQuote(CancellationToken ct = default);
    Task<TResponse> Mint(CancellationToken ct = default);
}