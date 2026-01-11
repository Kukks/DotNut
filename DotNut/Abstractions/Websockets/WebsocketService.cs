using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using DotNut.Abstractions.Websockets;

namespace DotNut.Abstractions;

public class WebsocketService : IWebsocketService
{
    private readonly ConcurrentDictionary<string, WebsocketConnection> _connections = new();
    private readonly ConcurrentDictionary<string, Subscription> _subscriptions = new();
    private readonly ConcurrentDictionary<int, PendingRequest> _pendingRequests = new();
    private int _nextRequestId = 0;

    public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

    public async Task<WebsocketConnection> ConnectAsync(
        string mintUrl,
        CancellationToken ct = default
    )
    {
        var normalized = NormalizeMintUrl(mintUrl);

        var connectionId = Guid.NewGuid().ToString();
        var wsUrl = GetWebSocketUrl(mintUrl);

        var clientWebSocket = new ClientWebSocket();
        try
        {
            await clientWebSocket.ConnectAsync(new Uri(wsUrl), ct);
        }
        catch (Exception ex)
        {
            clientWebSocket.Dispose();
            throw;
        }

        var connection = new WebsocketConnection
        {
            Id = connectionId,
            MintUrl = normalized,
            WebSocket = clientWebSocket,
            State = WebSocketState.Open,
        };

        _connections[normalized] = connection;
        OnConnectionStateChanged(connectionId, WebSocketState.Open);

        _ = Task.Run(async () => await ListenForMessages(connection, CancellationToken.None));

        return connection;
    }

    public async Task<WebsocketConnection> LazyConnectAsync(
        string mintUrl,
        CancellationToken ct = default
    )
    {
        var normalized = NormalizeMintUrl(mintUrl);

        if (_connections.TryGetValue(normalized, out var existing))
        {
            if (existing is { State: WebSocketState.Open, WebSocket.State: WebSocketState.Open })
            {
                return existing;
            }
        }
        _connections.TryRemove(normalized, out _);
        return await ConnectAsync(mintUrl, ct);
    }

    public async Task DisconnectAsync(string mintUrl, CancellationToken ct = default)
    {
        var normalized = NormalizeMintUrl(mintUrl);

        if (!_connections.TryGetValue(normalized, out var connection))
        {
            return;
        }

        try
        {
            if (connection.State == WebSocketState.Open)
            {
                await connection.WebSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Client disconnecting",
                    ct
                );
            }
        }
        catch (Exception _)
        {
            // ignored
        }
        finally
        {
            connection.State = WebSocketState.Closed;
            connection.WebSocket.Dispose();
            _connections.TryRemove(normalized, out _);

            var subscriptionsToRemove = _subscriptions
                .Where(s => s.Value.ConnectionId == connection.Id)
                .Select(s => s.Key)
                .ToList();

            foreach (var subId in subscriptionsToRemove)
            {
                if (_subscriptions.TryRemove(subId, out var removedSub))
                {
                    await removedSub.CloseAsync();
                }
            }

            OnConnectionStateChanged(connection.Id, WebSocketState.Closed);
        }
    }

    public async Task<Subscription> SubscribeAsync(
        string mintUrl,
        SubscriptionKind kind,
        string[] filters,
        CancellationToken ct = default
    )
    {
        var normalized = NormalizeMintUrl(mintUrl);

        if (!_connections.TryGetValue(normalized, out var connection))
        {
            throw new InvalidOperationException($"Connection for mint {mintUrl} not found");
        }

        if (connection.State != WebSocketState.Open)
        {
            throw new InvalidOperationException($"Connection for mint {mintUrl} is not open");
        }

        var subId = Guid.NewGuid().ToString();
        var requestId = _getNextRequestId();

        var channel = Channel.CreateUnbounded<WsMessage>(
            new UnboundedChannelOptions { SingleReader = false }
        );

        var request = new WsRequest
        {
            JsonRpc = "2.0",
            Method = WsRequestMethod.subscribe,
            Params = new WsRequestParams
            {
                Kind = kind,
                SubId = subId,
                Filters = filters,
            },
            Id = requestId,
        };

        var subscription = new Subscription(this)
        {
            Id = subId,
            ConnectionId = connection.Id,
            Kind = kind,
            Filters = filters,
            CreatedAt = DateTime.UtcNow,
            NotificationChannel = channel,
        };

        _subscriptions[subId] = subscription;

        var tcs = new TaskCompletionSource<RequestResult>();

        _pendingRequests[requestId] = new PendingRequest { Tcs = tcs, SubscriptionId = subId };

        try
        {
            await SendMessageAsync(connection, request, ct);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(30));

            var completedTask = await Task.WhenAny(
                    tcs.Task,
                    Task.Delay(Timeout.Infinite, cts.Token)
                )
                .ConfigureAwait(false);

            if (completedTask != tcs.Task)
            {
                _subscriptions.TryRemove(subId, out _);
                await subscription.CloseAsync();
                throw new TimeoutException("Subscription request timed out");
            }

            var result = await tcs.Task;

            if (result is RequestResult.Failure failure)
            {
                _subscriptions.TryRemove(subId, out _);
                await subscription.CloseAsync();
                throw new InvalidOperationException($"Subscription failed: {failure.Message}");
            }

            return subscription;
        }
        finally
        {
            _pendingRequests.TryRemove(requestId, out _);
        }
    }

    public async Task UnsubscribeAsync(string subId, CancellationToken ct = default)
    {
        if (!_subscriptions.TryGetValue(subId, out var subscription))
            throw new InvalidOperationException($"Subscription {subId} not found");

        var connection = _connections.Values.FirstOrDefault(c => c.Id == subscription.ConnectionId);
        if (connection is null)
            throw new InvalidOperationException($"Connection for subscription {subId} not found");

        if (connection.State != WebSocketState.Open)
        {
            throw new InvalidOperationException($"Connection is not open");
        }

        var requestId = _getNextRequestId();
        var tcs = new TaskCompletionSource<RequestResult>();
        _pendingRequests[requestId] = new PendingRequest { Tcs = tcs, SubscriptionId = subId };

        try
        {
            var request = new WsRequest
            {
                JsonRpc = "2.0",
                Method = WsRequestMethod.unsubscribe,
                Params = new WsRequestParams { SubId = subId },
                Id = requestId,
            };

            await SendMessageAsync(connection, request, ct);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(30));

            var completed = await Task.WhenAny(tcs.Task, Task.Delay(Timeout.Infinite, cts.Token))
                .ConfigureAwait(false);

            if (completed != tcs.Task)
            {
                throw new TimeoutException("Unsubscribe request timed out");
            }

            await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            _pendingRequests.TryRemove(requestId, out _);
            _subscriptions.TryRemove(subId, out _);
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var sub in _subscriptions.Values)
        {
            try
            {
                await sub.CloseAsync();
            }
            catch { }
        }
        var mintUrls = _connections.Keys.ToList();
        foreach (var mintUrl in mintUrls)
        {
            await DisconnectAsync(mintUrl);
        }
        _subscriptions.Clear();
        _connections.Clear();
        _pendingRequests.Clear();
    }

    public WebSocketState GetConnectionState(string mintUrl)
    {
        var normalized = NormalizeMintUrl(mintUrl);
        return _connections.TryGetValue(normalized, out var connection)
            ? connection.State
            : WebSocketState.None;
    }

    public IEnumerable<Subscription> GetSubscriptions(string mintUrl)
    {
        var normalized = NormalizeMintUrl(mintUrl);
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
        var messageBuffer = new MemoryStream();

        try
        {
            while (connection.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                var result = await connection.WebSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    ct
                );

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    connection.State = WebSocketState.Closed;
                    OnConnectionStateChanged(connection.Id, WebSocketState.Closed);
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    messageBuffer.Write(buffer, 0, result.Count);
                    if (result.EndOfMessage)
                    {
                        var message = Encoding.UTF8.GetString(messageBuffer.ToArray());
                        messageBuffer.SetLength(0);
                        _processMessage(connection, message);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
        catch (Exception ex)
        {
            connection.State = WebSocketState.Aborted;
            OnConnectionStateChanged(connection.Id, WebSocketState.Aborted);
        }
        finally
        {
            // Close all subscriptions for this connection
            var subscriptionsToClose = _subscriptions
                .Values.Where(s => s.ConnectionId == connection.Id)
                .ToList();

            foreach (var sub in subscriptionsToClose)
            {
                try
                {
                    await sub.CloseAsync();
                }
                catch { }
                _subscriptions.TryRemove(sub.Id, out _);
            }
        }
    }

    private void _processMessage(WebsocketConnection connection, string message)
    {
        try
        {
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(message);

            if (
                jsonElement.TryGetProperty("method", out var methodProp)
                && methodProp.GetString() == "subscribe"
            )
            {
                var notification = JsonSerializer.Deserialize<WsNotification>(message);
                if (notification != null)
                {
                    _onNotificationReceived(notification);
                }
            }
            else if (jsonElement.TryGetProperty("result", out _))
            {
                var response = JsonSerializer.Deserialize<WsResponse>(message);
                if (response != null)
                {
                    HandleResponse(response);
                }
            }
            else if (jsonElement.TryGetProperty("error", out _))
            {
                var error = JsonSerializer.Deserialize<WsError>(message);
                if (error != null)
                {
                    HandleError(error);
                }
            }
        }
        catch (Exception ex)
        {
            // Could be logged if logging is added later
        }
    }

    private async Task SendMessageAsync<T>(
        WebsocketConnection connection,
        T message,
        CancellationToken ct
    )
    {
        var json = JsonSerializer.Serialize(
            message,
            new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            }
        );
        var bytes = Encoding.UTF8.GetBytes(json);

        await connection.WebSocket.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            true,
            ct
        );
    }

    public bool IsConnected(string mintUrl)
    {
        var normalized = NormalizeMintUrl(mintUrl);
        return _connections.TryGetValue(normalized, out var conn)
            && conn.State == WebSocketState.Open;
    }

    private string GetWebSocketUrl(string mintUrl)
    {
        var uri = new Uri(NormalizeMintUrl(mintUrl));
        var scheme = uri.Scheme == "https" ? "wss" : "ws";
        var hostPort = (uri.IsDefaultPort) ? uri.Host : $"{uri.Host}:{uri.Port}";
        var path = uri.AbsolutePath.TrimEnd('/');
        return $"{scheme}://{hostPort}{path}/v1/ws";
    }

    private int _getNextRequestId()
    {
        return Interlocked.Increment(ref _nextRequestId);
    }

    private void OnConnectionStateChanged(string connectionId, WebSocketState state)
    {
        ConnectionStateChanged?.Invoke(
            this,
            new ConnectionStateChangedEventArgs { ConnectionId = connectionId, State = state }
        );
    }

    private static string NormalizeMintUrl(string mintUrl)
    {
        if (!Uri.TryCreate(mintUrl.TrimEnd('/'), UriKind.Absolute, out var uri))
        {
            return mintUrl.TrimEnd('/').ToLowerInvariant();
        }
        var host = uri.Host.ToLowerInvariant();
        var builder = new UriBuilder(uri) { Host = host };
        return builder.Uri.ToString().TrimEnd('/');
    }

    private void HandleResponse(WsResponse response)
    {
        if (!_pendingRequests.TryGetValue(response.Id, out var pr))
        {
            return;
        }
        var result = new RequestResult.Success(
            SubId: response.Result.SubId,
            Status: response.Result.Status
        );
        pr.Tcs.TrySetResult(result);

        if (!_subscriptions.TryGetValue(pr.SubscriptionId, out var sub))
        {
            return;
        }
        sub.NotificationChannel.Writer.TryWrite(new WsMessage.Response(response));
    }

    private void HandleError(WsError error)
    {
        if (!_pendingRequests.TryGetValue(error.Id, out var pr))
        {
            return;
        }
        var result = new RequestResult.Failure(
            Code: error.Error.Code,
            Message: error.Error.Message,
            RequestId: error.Id
        );
        pr.Tcs.TrySetResult(result);

        if (!_subscriptions.TryGetValue(pr.SubscriptionId, out var sub))
        {
            return;
        }

        sub.NotificationChannel.Writer.TryWrite(new WsMessage.Error(error));
    }

    private void _onNotificationReceived(WsNotification notification)
    {
        if (!_subscriptions.TryGetValue(notification.Params.SubId, out var sub))
        {
            return;
        }
        sub.NotificationChannel.Writer.TryWrite(new WsMessage.Notification(notification));
    }
}
