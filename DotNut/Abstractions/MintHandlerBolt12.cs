using DotNut.Abstractions.Websockets;
using DotNut.ApiModels;
using DotNut.ApiModels.Mint.bolt12;

namespace DotNut.Abstractions;

public class MintHandlerBolt12: IMintHandler<PostMintResponse>
{
    
    private readonly ICashuWalletBuilder _wallet;
    private PostMintQuoteBolt12Response _quote;
    private GetKeysResponse.KeysetItemResponse _keyset;

    public MintHandlerBolt12(Wallet wallet, PostMintQuoteBolt12Response quote, GetKeysResponse.KeysetItemResponse keyset)
    {
        this._wallet = wallet;
        this._quote = quote;
        this._keyset = keyset;
    }

    public Task<PostMintResponse> Mint(CancellationToken cts = default)
    {
        throw new NotImplementedException();
    }

    public Task<Subscription> Subscribe()
    {
        throw new NotImplementedException();
    }
}