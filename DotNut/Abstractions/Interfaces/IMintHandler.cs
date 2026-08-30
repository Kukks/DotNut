namespace DotNut.Abstractions;

public interface IMintHandler;

public interface IMintHandler<TQuote, TResponse> : IMintHandler
{
    public IMintHandler<TQuote, TResponse> WithSignature(string signature);
    public IMintHandler<TQuote, TResponse> SignWithPrivkey(PrivKey privkey);
    public IMintHandler<TQuote, TResponse> SignWithPrivkey(string privKeyHex);

    TQuote GetQuote();
    List<OutputData> GetOutputs();

    /// <summary>
    /// Re-reads the quote from the mint and returns it. A response older than the one already
    /// held is discarded, as NUT-04 requires: mints bump <c>updated_at</c> on every change to
    /// the amounts, and a wallet must not let a stale reply walk those amounts backwards.
    /// </summary>
    Task<TQuote> RefreshQuote(CancellationToken ct = default);

    Task<TResponse> Mint(CancellationToken ct = default);
}
