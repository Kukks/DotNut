using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using DotNut.Abstractions.Websockets;

namespace DotNut.Abstractions;

public class WebsocketService : IWebsocketService
{
    private readonly ConcurrentDictionary<string, WebsocketConnection> _connections = new();
    private readonly ConcurrentDictionary<string, Subscription> _subscriptions = new();
    private readonly object _lockObject = new();
    private int _nextRequestId = 0;

    public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
    public event EventHandler<WsError>? OnWsError;
    
    public async Task<WebsocketConnection> ConnectAsync(string mintUrl, CancellationToken ct = default)
    {
        var normalized = _normalizeMintUrl(mintUrl);
        
        if (_connections.TryGetValue(normalized, out var existing))
        {
            return existing;
        }
        
        var connectionId = Guid.NewGuid().ToString();
        var wsUrl = GetWebSocketUrl(mintUrl);
             
        var clientWebSocket = new ClientWebSocket();
        await clientWebSocket.ConnectAsync(new Uri(wsUrl), ct); 
        
        var connection = new WebsocketConnection
        {
            Id = connectionId, 
            MintUrl = normalized, 
            WebSocket = clientWebSocket, 
            State = WebSocketState.Open
        };
             
        _connections[normalized] = connection;
             
        _ = Task.Run(async () => await ListenForMessages(connection, ct), ct);
             
        OnConnectionStateChanged(connectionId, WebSocketState.Open);
             
        return connection;
    }
    
    public async Task DisconnectAsync(string mintUrl, CancellationToken ct = default)
    {
        var normalized = _normalizeMintUrl(mintUrl);
        
        if (!_connections.TryGetValue(normalized, out var connection))
        {
            return;
        }
     
        try
        {
            if (connection.State == WebSocketState.Open)
            {
                await connection.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnecting", ct);
            }
        }
        catch (Exception)
        {
            // Ignore close exceptions
        }
        finally
        {
            connection.WebSocket.Dispose();
            _connections.TryRemove(normalized, out _);
                 
            var subscriptionsToRemove = _subscriptions
                .Where(s => s.Value.ConnectionId == connection.Id)
                .Select(s => s.Key)
                .ToList();
                 
            foreach (var subId in subscriptionsToRemove)
            {
                _subscriptions.TryRemove(subId, out _);
            }
                 
            OnConnectionStateChanged(connection.Id, WebSocketState.Closed);
        }
    }

    public async Task<Subscription> SubscribeAsync(string mintUrl, SubscriptionKind kind, string[] filters, CancellationToken ct = default)
    {
        var normalized = _normalizeMintUrl(mintUrl);
        
        if (!_connections.TryGetValue(normalized, out var connection))
        {
            throw new InvalidOperationException($"Connection for mint {mintUrl} not found");
        }
             
        if (connection.State != WebSocketState.Open)
        {
            throw new InvalidOperationException($"Connection for mint {mintUrl} is not open");
        }
     
        var subId = Guid.NewGuid().ToString();
        var requestId = GetNextRequestId();
        
        var channel = Channel.CreateUnbounded<WsNotificationParams>(new UnboundedChannelOptions { SingleReader = false });
             
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
            ConnectionId = connection.Id,
            Kind = kind,
            Filters = filters,
            CreatedAt = DateTime.UtcNow,
            NotificationChannel = channel,
        };
             
        _subscriptions[subId] = subscription;
             
        await SendMessageAsync<WsRequest>(connection, request, ct);
             
        return subscription;
    }
    
    public async Task UnsubscribeAsync(string subId, CancellationToken ct = default)
    {
        if (!_subscriptions.TryGetValue(subId, out var subscription))
            throw new InvalidOperationException($"Subscription {subId} not found");

        if (_connections.Values.FirstOrDefault(c => c.Id == subscription.ConnectionId) is not { } connection)
            throw new InvalidOperationException($"Connection for subscription {subId} not found");

             
        if (connection.State != WebSocketState.Open)
        {
            throw new InvalidOperationException($"Connection is not open");
        }
     
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
             
        await SendMessageAsync(connection, request, ct);
             
        _subscriptions.TryRemove(subId, out _);
    }
    
    public async ValueTask DisposeAsync()
    {
        foreach (var sub in _subscriptions.Values)
        {
            sub.Close();
        }
        var mintUrls = _connections.Keys.ToList();
        foreach (var mintUrl in mintUrls)
        {
            await DisconnectAsync(mintUrl);
        }
        _subscriptions.Clear();
        _connections.Clear();
    }
         
    public WebSocketState GetConnectionState(string mintUrl)
    {
        var normalized = _normalizeMintUrl(mintUrl);
        return _connections.TryGetValue(normalized, out var connection) 
            ? connection.State 
            : WebSocketState.None;
    }
    
    public IEnumerable<Subscription> GetSubscriptions(string mintUrl)
    {
        var normalized = _normalizeMintUrl(mintUrl);
        if (!_connections.TryGetValue(normalized, out var connection))
        {
            throw new Exception($"Connection for mint {mintUrl} not found");
        }
        return _subscriptions.Values.Where(s => s.ConnectionId == connection.Id);
    }
     
    public IEnumerable<WebsocketConnection> GetConnections()
    {
        return _connections.Values;
    }
     
    private async Task ListenForMessages(WebsocketConnection connection, CancellationToken ct)
    {
        var buffer = new byte[4096];
             
        try
        {
            while (connection.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                var result = await connection.WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                     
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
        }
    }
     
    private async Task ProcessMessage(WebsocketConnection connection, string message)
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
                    _onNotificationReceived(notification.Params);
                }
            }
            else if (jsonElement.TryGetProperty("result", out _))
            {
                var response = JsonSerializer.Deserialize<WsResponse>(message);
                // TODO: Handle response
            }
            else if (jsonElement.TryGetProperty("error", out _))
            {
                var error = JsonSerializer.Deserialize<WsError>(message);
                // TODO: Handle error
            }
        }
        catch (Exception ex)
        {
            // TODO: Log exception
        }
    }
     
    private async Task SendMessageAsync<T>(WebsocketConnection connection, T message, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);
             
        await connection.WebSocket.SendAsync(
            new ArraySegment<byte>(bytes), 
            WebSocketMessageType.Text, 
            true, 
            ct);
    }
     
    private string GetWebSocketUrl(string mintUrl)
    {
        var uri = new Uri(_normalizeMintUrl(mintUrl));
        var scheme = uri.Scheme == "https" ? "wss" : "ws";
        var hostPort = (uri.IsDefaultPort) ? uri.Host : $"{uri.Host}:{uri.Port}";
        var path = uri.AbsolutePath.TrimEnd('/');
        return $"{scheme}://{hostPort}{path}/v1/ws";
    }

    private int GetNextRequestId()
    {
        lock (_lockObject)
        {
            return ++_nextRequestId;
        }
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
        => _connections.ContainsKey(_normalizeMintUrl(mintUrl));
    
    private string _normalizeMintUrl(string mintUrl)
    {
        if (!Uri.TryCreate(mintUrl.TrimEnd('/'), UriKind.Absolute, out var uri))
        {
            return mintUrl.TrimEnd('/').ToLowerInvariant();
        }
        var host = uri.Host.ToLowerInvariant();
        var builder = new UriBuilder(uri) { Host = host };
        return builder.Uri.ToString().TrimEnd('/');
    }

    private void _onNotificationReceived(WsNotificationParams notificationParams)
    {
        if (!_subscriptions.TryGetValue(notificationParams.SubId, out var sub))
        {
            //it should never happen
            return;
        }
        sub.NotificationChannel.Writer.WriteAsync(notificationParams);
    }

    private WebsocketConnection? _getConnectionById(string connectionId)
    {
        return this._connections.Values.SingleOrDefault(c=>c.Id == connectionId, null);
    }
}