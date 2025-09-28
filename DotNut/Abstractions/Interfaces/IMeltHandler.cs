using DotNut.Abstractions.Websockets;
using DotNut.ApiModels;
using DotNut.ApiModels.Melt.bolt12;

namespace DotNut.Abstractions.Interfaces;

public interface IMeltHandler{}

public interface IMeltHandler<TRequest, TResponse>: IMeltHandler
{
    Task<TResponse> Melt(TRequest request);
    
    Task<Subscription> Subscribe(TRequest request);
}