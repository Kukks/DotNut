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
    
    Task DisconnectAsync(string connectionId, CancellationToken ct = default);

    Task<Subscription> SubscribeAsync(string connectionId, SubscriptionKind kind, string[] filters, CancellationToken ct = default);

    Task UnsubscribeAsync(string subId, CancellationToken ct = default);

    WebSocketState GetConnectionState(string connectionId);

    IEnumerable<Subscription> GetSubscriptions(string connectionId);

    IEnumerable<WebsocketConnection> GetConnections();
}
