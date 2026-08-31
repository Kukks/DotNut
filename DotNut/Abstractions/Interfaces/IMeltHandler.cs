namespace DotNut.Abstractions;

public interface IMeltHandler;

public interface IMeltHandler<TQuote, TResponse> : IMeltHandler
{
    TQuote GetQuote();

    /// <summary>
    /// Pays the quote.
    /// </summary>
    /// <param name="preferAsync">
    /// Ask the mint to return once the request validates rather than once the payment settles,
    /// leaving the quote PENDING. Poll the quote or subscribe over websockets to follow it.
    /// Mints that do not support this for the method ignore it and pay synchronously.
    /// </param>
    Task<TResponse> Melt(
        IEnumerable<Proof> inputs,
        bool preferAsync = false,
        CancellationToken ct = default
    );
}
