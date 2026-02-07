namespace DotNut.Abstractions;

public interface IMintHandler;

public interface IMintHandler<TQuote, TResponse> : IMintHandler
{
    public IMintHandler<TQuote, TResponse> WithSignature(string signature);
    public IMintHandler<TQuote, TResponse> SignWithPrivkey(PrivKey privkey);
    public IMintHandler<TQuote, TResponse> SignWithPrivkey(string privKeyHex);

    TQuote GetQuote();
    List<OutputData> GetOutputs();
    
    Task<TResponse> Mint(CancellationToken ct = default);
}
