using DotNut.Abstractions.Websockets;
using DotNut.ApiModels;
using DotNut.ApiModels.Melt.bolt12;

namespace DotNut.Abstractions.Interfaces;

public interface IMeltHandler;

public interface IMeltHandler<TQuote, TResponse>: IMeltHandler
{
    Task<TQuote> GetQuote(CancellationToken cts = default);
    Task<TResponse> Melt(List<Proof> inputs, CancellationToken cts = default);
    
    Task<Subscription> Subscribe(CancellationToken cts = default);
}