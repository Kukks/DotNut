using DotNut.Abstractions.Interfaces;
using DotNut.Abstractions.Websockets;
using DotNut.ApiModels.Melt.bolt12;

namespace DotNut.Abstractions.Handlers;

public class MeltHandlerBolt12: IMeltHandler<PostMeltQuoteBolt12Response, List<Proof>>
{
    public Task<PostMeltQuoteBolt12Response> GetQuote(CancellationToken cts = default)
    {
        throw new NotImplementedException();
    }

    public Task<List<Proof>> Melt(List<Proof> inputs, CancellationToken cts = default)
    {
        throw new NotImplementedException();
    }

    public Task<Subscription> Subscribe(CancellationToken cts = default)
    {
        throw new NotImplementedException();
    }
}