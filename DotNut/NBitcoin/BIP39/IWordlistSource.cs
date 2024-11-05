
// ReSharper disable once CheckNamespace
namespace NBitcoin
{
    public interface IWordlistSource
    {
        Task<Wordlist>? Load(string name);
    }
}