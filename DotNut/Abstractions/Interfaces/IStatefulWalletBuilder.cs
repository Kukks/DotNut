namespace DotNut.Abstractions.Interfaces;

/// <summary>
/// Abstraction on WalletBuilder, with Proof Manager. Stateful wallet library, abstracting all operations like melting/minting.
/// </summary>
/// 
public interface IStatefulWalletBuilder
{
    Task ReceiveLightning();
    Task SendLightning();
    Task ReceiveProofs();
    Task SendProofs();
}