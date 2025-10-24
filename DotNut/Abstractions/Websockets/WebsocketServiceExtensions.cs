namespace DotNut.Abstractions.Websockets;

public static class WebsocketServiceExtensions
{
    public static async Task<Subscription> SubscribeToMintQuoteAsync(
        this IWebsocketService service,
        string mintUrl,
        string[] quoteIds,
        CancellationToken ct = default)
    {
        await service.LazyConnectAsync(mintUrl, ct);
        return await service.SubscribeAsync(mintUrl, SubscriptionKind.bolt11_mint_quote, quoteIds, ct);
    }

    public static async Task<Subscription> SubscribeToMeltQuoteAsync(
        this IWebsocketService service,
        string mintUrl,
        string[] quoteIds,
        CancellationToken ct = default)
    {
        await service.LazyConnectAsync(mintUrl, ct);
        return await service.SubscribeAsync(mintUrl, SubscriptionKind.bolt11_melt_quote, quoteIds, ct);
    }

    public static async Task<Subscription> SubscribeToProofStateAsync(
        this IWebsocketService service,
        string mintUrl,
        string[] proofYs,
        CancellationToken ct = default)
    {
        await service.LazyConnectAsync(mintUrl, ct);
        return await service.SubscribeAsync(mintUrl, SubscriptionKind.proof_state, proofYs, ct);
    }

    public static async Task<Subscription> SubscribeToSingleProofStateAsync(
        this IWebsocketService service,
        string mintUrl,
        string proofY,
        CancellationToken ct = default)
    {
        await service.LazyConnectAsync(mintUrl, ct);
        return await service.SubscribeToProofStateAsync(mintUrl, new[] { proofY }, ct);
    }

    public static async Task<Subscription> SubscribeToSingleMintQuoteAsync(
        this IWebsocketService service,
        string mintUrl,
        string quoteId,
        CancellationToken ct = default)
    {
        await service.LazyConnectAsync(mintUrl, ct);
        return await service.SubscribeToMintQuoteAsync(mintUrl, new[] { quoteId }, ct);
    }

    public static async Task<Subscription> SubscribeToSingleMeltQuoteAsync(
        this IWebsocketService service,
        string mintUrl,
        string quoteId,
        CancellationToken ct = default)
    {
        await service.LazyConnectAsync(mintUrl, ct);
        return await service.SubscribeToMeltQuoteAsync(mintUrl, new[] { quoteId }, ct);
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
        CancellationToken ct = default)
    {
        var subscriptions = service.GetSubscriptions(connectionId).ToList();
        foreach (var subscription in subscriptions)
        {
            await service.UnsubscribeAsync(subscription.Id, ct);
        }
    }

    public static async Task UnsubscribeByKindAsync(
        this IWebsocketService service,
        string connectionId,
        SubscriptionKind kind,
        CancellationToken ct = default)
    {
        var subscriptions = service.GetSubscriptionsByKind(connectionId, kind).ToList();
        foreach (var subscription in subscriptions)
        {
            await service.UnsubscribeAsync(subscription.Id, ct);
        }
    }
}
