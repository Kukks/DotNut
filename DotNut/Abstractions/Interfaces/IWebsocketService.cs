using System.Net.WebSockets;

namespace DotNut.Abstractions.Websockets;

public interface IWebsocketService : IDisposable
{
    event EventHandler<NotificationEventArgs>? NotificationReceived;
    
    event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

    Task<string> ConnectAsync(string mintUrl, CancellationToken cancellationToken = default);

    Task<string> SubscribeAsync(string connectionId, SubscriptionKind kind, string[] filters, CancellationToken cancellationToken = default);

    Task UnsubscribeAsync(string connectionId, string subId, CancellationToken cancellationToken = default);

    Task DisconnectAsync(string connectionId, CancellationToken cancellationToken = default);

    WebSocketState GetConnectionState(string connectionId);

    IEnumerable<Subscription> GetSubscriptions(string connectionId);

    IEnumerable<WebSocketConnection> GetConnections();
}
