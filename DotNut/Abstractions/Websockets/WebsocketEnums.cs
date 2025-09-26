using System.Text.Json.Serialization;

namespace DotNut.Abstractions.Websockets;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SubscriptionKind
{
    bolt11_melt_quote,
    bolt11_mint_quote,
    proof_state
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WsRequestMethod
{
    subscribe,
    unsubscribe
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProofState
{
    UNSPENT,
    PENDING,
    SPENT
}
