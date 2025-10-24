namespace DotNut.Abstractions;

public interface IMintHandler;
public interface IMintHandler<TQuote, TResponse>: IMintHandler
{
    public IMintHandler<TQuote, TResponse> WithSignature(string signature);
    public IMintHandler<TQuote, TResponse>  SignWithPrivkey(PrivKey privkey);
    public IMintHandler<TQuote, TResponse>  SignWithPrivkey(string privKeyHex);
    
    Task<TQuote> GetQuote(CancellationToken ct = default);
    Task<TResponse> Mint(CancellationToken ct = default);
}