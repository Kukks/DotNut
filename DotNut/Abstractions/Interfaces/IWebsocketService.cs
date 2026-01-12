using System.Net.WebSockets;
using DotNut.Abstractions.Websockets;

namespace DotNut.Abstractions;

public interface IWebsocketService : IAsyncDisposable
{
    /// <summary>
    /// Raised when a connection's state changes. Handlers should be thread-safe.
    /// </summary>
    event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

    Task<WebsocketConnection> LazyConnectAsync(string mintUrl, CancellationToken ct = default);

    Task DisconnectAsync(string mintUrl, CancellationToken ct = default);

    Task<Subscription> SubscribeAsync(
        string mintUrl,
        SubscriptionKind kind,
        string[] filters,
        CancellationToken ct = default
    );

    Task UnsubscribeAsync(string subId, CancellationToken ct = default);

    WebSocketState GetConnectionState(string mintUrl);

    IEnumerable<Subscription> GetSubscriptions(string mintUrl);

    IEnumerable<WebsocketConnection> GetConnections();
}
