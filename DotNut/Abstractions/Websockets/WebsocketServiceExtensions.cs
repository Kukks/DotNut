namespace DotNut.Abstractions.Websockets;

public static class WebsocketServiceExtensions
{
    public static async Task<string> SubscribeToMintQuoteAsync(
        this IWebsocketService service,
        string connectionId,
        string[] quoteIds,
        CancellationToken cancellationToken = default)
    {
        return await service.SubscribeAsync(connectionId, SubscriptionKind.bolt11_mint_quote, quoteIds, cancellationToken);
    }

    public static async Task<string> SubscribeToMeltQuoteAsync(
        this IWebsocketService service,
        string connectionId,
        string[] quoteIds,
        CancellationToken cancellationToken = default)
    {
        return await service.SubscribeAsync(connectionId, SubscriptionKind.bolt11_melt_quote, quoteIds, cancellationToken);
    }

    public static async Task<string> SubscribeToProofStateAsync(
        this IWebsocketService service,
        string connectionId,
        string[] proofYs,
        CancellationToken cancellationToken = default)
    {
        return await service.SubscribeAsync(connectionId, SubscriptionKind.proof_state, proofYs, cancellationToken);
    }

    public static async Task<string> SubscribeToSingleProofStateAsync(
        this IWebsocketService service,
        string connectionId,
        string proofY,
        CancellationToken cancellationToken = default)
    {
        return await service.SubscribeToProofStateAsync(connectionId, new[] { proofY }, cancellationToken);
    }

    public static async Task<string> SubscribeToSingleMintQuoteAsync(
        this IWebsocketService service,
        string connectionId,
        string quoteId,
        CancellationToken cancellationToken = default)
    {
        return await service.SubscribeToMintQuoteAsync(connectionId, new[] { quoteId }, cancellationToken);
    }

    public static async Task<string> SubscribeToSingleMeltQuoteAsync(
        this IWebsocketService service,
        string connectionId,
        string quoteId,
        CancellationToken cancellationToken = default)
    {
        return await service.SubscribeToMeltQuoteAsync(connectionId, new[] { quoteId }, cancellationToken);
    }

    public static bool IsConnectionActive(this IWebsocketService service, string connectionId)
    {
        var state = service.GetConnectionState(connectionId);
        return state == System.Net.WebSockets.WebSocketState.Open;
    }

    public static IEnumerable<Subscription> GetSubscriptionsByKind(
        this IWebsocketService service,
        string connectionId,
        SubscriptionKind kind)
    {
        return service.GetSubscriptions(connectionId).Where(s => s.Kind == kind);
    }

    public static async Task UnsubscribeAllAsync(
        this IWebsocketService service,
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        var subscriptions = service.GetSubscriptions(connectionId).ToList();
        foreach (var subscription in subscriptions)
        {
            await service.UnsubscribeAsync(connectionId, subscription.Id, cancellationToken);
        }
    }

    public static async Task UnsubscribeByKindAsync(
        this IWebsocketService service,
        string connectionId,
        SubscriptionKind kind,
        CancellationToken cancellationToken = default)
    {
        var subscriptions = service.GetSubscriptionsByKind(connectionId, kind).ToList();
        foreach (var subscription in subscriptions)
        {
            await service.UnsubscribeAsync(connectionId, subscription.Id, cancellationToken);
        }
    }
}
