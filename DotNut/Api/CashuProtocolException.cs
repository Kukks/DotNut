namespace DotNut.Api;

public class CashuProtocolException : Exception
{
    public CashuProtocolException(CashuProtocolError error) : base(error.Detail)
    {
        Error = error;
    }

    public CashuProtocolError Error { get; }
}