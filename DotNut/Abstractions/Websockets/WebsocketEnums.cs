namespace DotNut.Abstractions.Websockets;

public enum SubscriptionKind
{
    Bolt11MeltQuote,
    Bolt11MintQuote,
    Bolt12MeltQuote,
    Bolt12MintQuote,
    ProofState,
}

public enum WsRequestMethod
{
    Subscribe,
    Unsubscribe,
}
