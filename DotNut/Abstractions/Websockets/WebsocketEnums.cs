using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace DotNut.Abstractions.Websockets;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SubscriptionKind
{
    
    [EnumMember(Value = "bolt11_melt_quote")] Bolt11MeltQuote,
    
    [EnumMember(Value = "bolt11_mint_quote")] Bolt11MintQuote,
    
    [EnumMember(Value = "bolt12_melt_quote")] Bolt12MeltQuote,
    
    [EnumMember(Value = "bolt12_mint_quote")] Bolt12MintQuote,
    
    [EnumMember(Value = "proof_state")] ProofState,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WsRequestMethod
{
    [EnumMember(Value = "subscribe")]
    Subscribe,
    [EnumMember(Value = "unsubscribe")]
    Unsubscribe,
}
