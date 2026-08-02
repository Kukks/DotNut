namespace DotNut;

/// <summary>
/// A payment method the payee accepts, with an optional fee that compensates them
/// for melting out via that method. The fee only applies to payments from a mint
/// outside <see cref="PaymentRequest.Mints"/>, or from any mint if no mint list is set.
/// </summary>
public class SupportedMethod
{
    public string Method { get; set; }
    public ulong? Fee { get; set; }
}
