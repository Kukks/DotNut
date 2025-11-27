namespace DotNut.Abstractions;

public interface IMeltHandler;

public interface IMeltHandler<TQuote, TResponse>: IMeltHandler
{
    Task<TQuote> GetQuote(CancellationToken ct = default);
    Task<TResponse> Melt(List<Proof> inputs, CancellationToken ct = default);
}