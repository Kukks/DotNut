using System.Text.Json;
using DotNut.ApiModels;

namespace DotNut.Abstractions.Websockets;

public static class NotificationParser
{
    public static object? ParsePayload(WsNotification notification, SubscriptionKind subscriptionKind)
    {
        if (notification.Params.Payload == null)
            return null;

        var jsonElement = (JsonElement)notification.Params.Payload;

        return subscriptionKind switch
        {
            SubscriptionKind.bolt11_mint_quote => jsonElement.Deserialize<PostMintQuoteBolt11Response>(),
            SubscriptionKind.bolt11_melt_quote => jsonElement.Deserialize<PostMeltQuoteBolt11Response>(),
            SubscriptionKind.proof_state => jsonElement.Deserialize<PostCheckStateResponse>(),
            _ => notification.Params.Payload
        };
    }

    public static T? ParsePayload<T>(WsNotification notification) where T : class
    {
        if (notification.Params.Payload == null)
            return null;

        var jsonElement = (JsonElement)notification.Params.Payload;
        return jsonElement.Deserialize<T>();
    }

    public static bool IsPayloadOfType<T>(WsNotification notification) where T : class
    {
        try
        {
            return ParsePayload<T>(notification) != null;
        }
        catch
        {
            return false;
        }
    }
}
