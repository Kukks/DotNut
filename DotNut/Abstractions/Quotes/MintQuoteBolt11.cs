using DotNut.ApiModels;

namespace DotNut.Abstractions;

public class MintQuoteBolt11
{
    private readonly string method = "bolt11";
    private ulong Amount;
    
    public MintQuoteBolt11(PostMintQuoteBolt11Response response)
    {
        this.Amount = response.Amount;
        
    }
}