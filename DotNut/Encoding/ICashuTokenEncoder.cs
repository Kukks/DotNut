namespace DotNut;

public interface ICashuTokenEncoder
{
    string Encode(CashuToken token);
    CashuToken Decode(string token);
    
}