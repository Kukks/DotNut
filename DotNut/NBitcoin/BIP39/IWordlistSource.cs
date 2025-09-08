namespace DotNut.NBitcoin.BIP39
{
    public interface IWordlistSource
    {
        Task<Wordlist>? Load(string name);
    }
}