namespace DotNut.Abstractions;

public interface IMeltHandler;

public interface IMeltHandler<TQuote, TResponse> : IMeltHandler
{
    TQuote GetQuote();
    Task<TResponse> Melt(List<Proof> inputs, CancellationToken ct = default);
}
