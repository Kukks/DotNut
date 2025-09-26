using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using DotNut.Abstractions.Websockets;

namespace DotNut.Abstractions;

public class WebsocketService : IWebsocketService
{
    private readonly ConcurrentDictionary<string, WebSocketConnection> _connections = new();
    private readonly ConcurrentDictionary<string, Subscription> _subscriptions = new();
    private readonly object _lockObject = new();
    private int _nextRequestId = 0;
     
    public event EventHandler<NotificationEventArgs>? NotificationReceived;
    public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
    public async Task<string> ConnectAsync(string mintUrl, CancellationToken cancellationToken = default)
         {
             var connectionId = Guid.NewGuid().ToString();
             var wsUrl = GetWebSocketUrl(mintUrl);
             
             var clientWebSocket = new ClientWebSocket();
             await clientWebSocket.ConnectAsync(new Uri(wsUrl), cancellationToken);
             
             var connection = new WebSocketConnection
             {
                 Id = connectionId,
                 MintUrl = mintUrl,
                 WebSocket = clientWebSocket,
                 State = WebSocketState.Open
             };
             
             _connections[connectionId] = connection;
             
             _ = Task.Run(async () => await ListenForMessages(connection, cancellationToken), cancellationToken);
             
             OnConnectionStateChanged(connectionId, WebSocketState.Open);
             
             return connectionId;
         }
    public async Task<string> SubscribeAsync(string connectionId, SubscriptionKind kind, string[] filters, CancellationToken cancellationToken = default)
         {
             if (!_connections.TryGetValue(connectionId, out var connection))
                 throw new InvalidOperationException($"Connection {connectionId} not found");
             
             if (connection.State != WebSocketState.Open)
                 throw new InvalidOperationException($"Connection {connectionId} is not open");
     
             var subId = Guid.NewGuid().ToString();
             var requestId = GetNextRequestId();
             
             var request = new WsRequest
             {
                 JsonRpc = "2.0",
                 Method = WsRequestMethod.subscribe,
                 Params = new WsRequestParams
                 {
                     Kind = kind,
                     SubId = subId,
                     Filters = filters
                 },
                 Id = requestId
             };
     
             var subscription = new Subscription
             {
                 Id = subId,
                 ConnectionId = connectionId,
                 Kind = kind,
                 Filters = filters,
                 CreatedAt = DateTime.UtcNow
             };
             
             _subscriptions[subId] = subscription;
             
             await SendMessageAsync(connection, request, cancellationToken);
             
             return subId;
         }
    public async Task UnsubscribeAsync(string connectionId, string subId, CancellationToken cancellationToken = default)
         {
             if (!_connections.TryGetValue(connectionId, out var connection))
                 throw new InvalidOperationException($"Connection {connectionId} not found");
             
             if (connection.State != WebSocketState.Open)
                 throw new InvalidOperationException($"Connection {connectionId} is not open");
     
             var requestId = GetNextRequestId();
             
             var request = new WsRequest
             {
                 JsonRpc = "2.0",
                 Method = WsRequestMethod.unsubscribe,
                 Params = new WsRequestParams
                 {
                     SubId = subId
                 },
                 Id = requestId
             };
             
             await SendMessageAsync(connection, request, cancellationToken);
             
             _subscriptions.TryRemove(subId, out _);
         }
     
    public async Task DisconnectAsync(string connectionId, CancellationToken cancellationToken = default)
         {
             if (!_connections.TryGetValue(connectionId, out var connection))
                 return;
     
             try
             {
                 if (connection.State == WebSocketState.Open)
                 {
                     await connection.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnecting", cancellationToken);
                 }
             }
             catch (Exception)
             {
                 // Ignore close exceptions
             }
             finally
             {
                 connection.WebSocket.Dispose();
                 _connections.TryRemove(connectionId, out _);
                 
                 var subscriptionsToRemove = _subscriptions
                     .Where(s => s.Value.ConnectionId == connectionId)
                     .Select(s => s.Key)
                     .ToList();
                 
                 foreach (var subId in subscriptionsToRemove)
                 {
                     _subscriptions.TryRemove(subId, out _);
                 }
                 
                 OnConnectionStateChanged(connectionId, WebSocketState.Closed);
             }
         }
    
    public async ValueTask DisposeAsync()
         {
             var connectionIds = _connections.Keys.ToList();
             foreach (var connectionId in connectionIds)
             {
                 await DisconnectAsync(connectionId);
             }
         }
         
    // Use only if necessary. pls use DisposeAsync
    public void Dispose()
         {
             var connectionIds = _connections.Keys.ToList();
             foreach (var connectionId in connectionIds)
             {
                 DisconnectAsync(connectionId).Wait(TimeSpan.FromSeconds(5));
             }
         }
         
    public WebSocketState GetConnectionState(string connectionId)
         {
             return _connections.TryGetValue(connectionId, out var connection) 
                 ? connection.State 
                 : WebSocketState.None;
         }
     
    public IEnumerable<Subscription> GetSubscriptions(string connectionId)
         {
             return _subscriptions.Values.Where(s => s.ConnectionId == connectionId);
         }
     
    public IEnumerable<WebSocketConnection> GetConnections()
         {
             return _connections.Values;
         }
     
    private async Task ListenForMessages(WebSocketConnection connection, CancellationToken cancellationToken)
         {
             var buffer = new byte[4096];
             
             try
             {
                 while (connection.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                 {
                     var result = await connection.WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                     
                     if (result.MessageType == WebSocketMessageType.Close)
                     {
                         connection.State = WebSocketState.Closed;
                         OnConnectionStateChanged(connection.Id, WebSocketState.Closed);
                         break;
                     }
                     
                     if (result.MessageType == WebSocketMessageType.Text)
                     {
                         var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                         await ProcessMessage(connection, message);
                     }
                 }
             }
             catch (Exception ex)
             {
                 connection.State = WebSocketState.Aborted;
                 OnConnectionStateChanged(connection.Id, WebSocketState.Aborted);
                 // Log exception
             }
         }
     
    private async Task ProcessMessage(WebSocketConnection connection, string message)
         {
             try
             {
                 var jsonElement = JsonSerializer.Deserialize<JsonElement>(message);
                 
                 if (jsonElement.TryGetProperty("method", out var methodProp) && 
                     methodProp.GetString() == "subscribe")
                 {
                     var notification = JsonSerializer.Deserialize<WsNotification>(message);
                     if (notification != null)
                     {
                         OnNotificationReceived(connection.Id, notification);
                     }
                 }
                 else if (jsonElement.TryGetProperty("result", out _))
                 {
                     var response = JsonSerializer.Deserialize<WsResponse>(message);
                 }
                 else if (jsonElement.TryGetProperty("error", out _))
                 {
                     var error = JsonSerializer.Deserialize<WsError>(message);
                 }
             }
             catch (Exception ex)
             {
             }
         }
     
    private async Task SendMessageAsync<T>(WebSocketConnection connection, T message, CancellationToken cancellationToken)
         {
             var json = JsonSerializer.Serialize(message);
             var bytes = Encoding.UTF8.GetBytes(json);
             
             await connection.WebSocket.SendAsync(
                 new ArraySegment<byte>(bytes), 
                 WebSocketMessageType.Text, 
                 true, 
                 cancellationToken);
         }
     
    private string GetWebSocketUrl(string mintUrl)
         {
             var uri = new Uri(mintUrl.TrimEnd('/'));
             var scheme = uri.Scheme == "https" ? "wss" : "ws";
             return $"{scheme}://{uri.Host}:{uri.Port}/v1/ws";
         }
     
    private int GetNextRequestId()
         {
             lock (_lockObject)
             {
                 return ++_nextRequestId;
             }
         }
     
    private void OnNotificationReceived(string connectionId, WsNotification notification)
         {
             NotificationReceived?.Invoke(this, new NotificationEventArgs
             {
                 ConnectionId = connectionId,
                 Notification = notification
             });
         }
    private void OnConnectionStateChanged(string connectionId, WebSocketState state)
         {
             ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs
             {
                 ConnectionId = connectionId,
                 State = state
             });
         }
    public bool IsConnected(string mintUrl) 
             => _connections.Any(x => _normalizeMintUrl(x.Value.MintUrl) == _normalizeMintUrl(mintUrl));
    private string _normalizeMintUrl(string mintUrl)
         {
             if (Uri.TryCreate(mintUrl.TrimEnd('/'), UriKind.Absolute, out var uri))
             {
                 var host = uri.Host.ToLowerInvariant();
                 var builder = new UriBuilder(uri) { Host = host };
                 return builder.Uri.ToString().TrimEnd('/');
             }
             return mintUrl.TrimEnd('/').ToLowerInvariant();
         }
     }
