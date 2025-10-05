using DotNut.Abstractions.Interfaces;

namespace DotNut.Abstractions;

public class StatefulWallet: IStatefulWalletBuilder
{
    private IWalletBuilder _wallet;
    private IProofManager _proofManager;
    
    public StatefulWallet(IWalletBuilder wallet, IProofManager proofManager)
    {
        this._wallet = wallet;
        this._proofManager = proofManager;
    }

    public Task ReceiveLightning()
    {
        throw new NotImplementedException();
    }

    public Task SendLightning()
    {
        throw new NotImplementedException();
    }

    public Task ReceiveProofs()
    {
        throw new NotImplementedException();
    }

    public Task SendProofs()
    {
        throw new NotImplementedException();
    }
}