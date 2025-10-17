using System.Net.WebSockets;

namespace DotNut.Abstractions.Websockets;

public interface IWebsocketService : IAsyncDisposable
{
    event EventHandler<WsError>? OnWsError;
    
    event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

    Task<WebsocketConnection> ConnectAsync(string mintUrl, CancellationToken ct = default);
    
    Task DisconnectAsync(string connectionId, CancellationToken ct = default);


    Task<Subscription> SubscribeAsync(string connectionId, SubscriptionKind kind, string[] filters, CancellationToken ct = default);

    Task UnsubscribeAsync(string subId, CancellationToken ct = default);


    WebSocketState GetConnectionState(string connectionId);

    IEnumerable<Subscription> GetSubscriptions(string connectionId);

    IEnumerable<WebsocketConnection> GetConnections();
}
