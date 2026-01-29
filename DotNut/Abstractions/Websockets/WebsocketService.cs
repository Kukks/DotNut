using System.Buffers;
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
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _connectionLocks = new();
    private readonly ConcurrentDictionary<string, SubscriptionInfo> _subscriptionInfos = new();

    private readonly WebsocketServiceOptions _options;
    private readonly CancellationTokenSource _disposeCts = new();

    private int _nextRequestId;
    private volatile bool _disposed;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

    public WebsocketService() : this(new WebsocketServiceOptions()) { }

    public WebsocketService(WebsocketServiceOptions options)
    {
        _options = options ?? new WebsocketServiceOptions();
        _ = RunRequestCleanupLoopAsync(_disposeCts.Token);
    }

    public async Task<WebsocketConnection> ConnectAsync(
        string mintUrl,
        CancellationToken ct = default
    )
    {
        var normalized = NormalizeMintUrl(mintUrl);
        var connectionId = Guid.NewGuid().ToString();
        var wsUrl = GetWebSocketUrl(mintUrl);

        var clientWebSocket = new ClientWebSocket();
        var connectionCts = CancellationTokenSource.CreateLinkedTokenSource(_disposeCts.Token, ct);

        try
        {
            await clientWebSocket.ConnectAsync(new Uri(wsUrl), ct);
        }
        catch
        {
            connectionCts.Dispose();
            clientWebSocket.Dispose();
            throw;
        }

        var connection = new WebsocketConnection
        {
            Id = connectionId,
            MintUrl = normalized,
            WebSocket = clientWebSocket,
            State = WebSocketState.Open,
            CancellationTokenSource = connectionCts,
            LastMessageReceived = DateTime.UtcNow,
        };

        _connections[normalized] = connection;
        OnConnectionStateChanged(connectionId, WebSocketState.Open);

        _ = RunWithErrorHandlingAsync(
            () => ListenForMessagesAsync(connection, connectionCts.Token),
            connection
        );

        _ = RunWithErrorHandlingAsync(
            () => RunHeartbeatLoopAsync(connection, connectionCts.Token),
            connection
        );

        return connection;
    }

    public async Task<WebsocketConnection> LazyConnectAsync(
        string mintUrl,
        CancellationToken ct = default
    )
    {
        var normalized = NormalizeMintUrl(mintUrl);
        var connectionLock = _connectionLocks.GetOrAdd(normalized, _ => new SemaphoreSlim(1, 1));

        await connectionLock.WaitAsync(ct);
        try
        {
            if (_connections.TryGetValue(normalized, out var existing))
            {
                if (existing is { State: WebSocketState.Open, WebSocket.State: WebSocketState.Open })
                {
                    return existing;
                }

                _connections.TryRemove(normalized, out _);
                existing.Dispose();
            }

            return await ConnectAsync(mintUrl, ct);
        }
        finally
        {
            connectionLock.Release();
        }
    }

    public async Task DisconnectAsync(string mintUrl, CancellationToken ct = default)
    {
        var normalized = NormalizeMintUrl(mintUrl);

        if (!_connections.TryRemove(normalized, out var connection))
        {
            return;
        }

        try
        {
            if (connection.State == WebSocketState.Open)
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

                await connection.WebSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Client disconnecting",
                    timeoutCts.Token
                );
            }
        }
        catch
        {
            // graceful close failed, continue with cleanup
        }
        finally
        {
            connection.State = WebSocketState.Closed;
            connection.CancellationTokenSource?.Cancel();
            connection.Dispose();

            await CleanupConnectionSubscriptionsAsync(connection);
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
        var requestId = GetNextRequestId();

        var channel = Channel.CreateBounded<WsMessage>(new BoundedChannelOptions(_options.MaxChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = false,
        });

        var request = new WsRequest
        {
            JsonRpc = "2.0",
            Method = WsRequestMethod.Subscribe,
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
        _subscriptionInfos[subId] = new SubscriptionInfo
        {
            MintUrl = normalized,
            Kind = kind,
            Filters = filters,
        };

        var tcs = new TaskCompletionSource<RequestResult>();
        _pendingRequests[requestId] = new PendingRequest
        {
            Tcs = tcs,
            SubscriptionId = subId,
            CreatedAt = DateTime.UtcNow,
        };

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
                _subscriptionInfos.TryRemove(subId, out _);
                await subscription.CloseInternalAsync();
                throw new TimeoutException("Subscription request timed out");
            }

            var result = await tcs.Task;

            if (result is RequestResult.Failure failure)
            {
                _subscriptions.TryRemove(subId, out _);
                _subscriptionInfos.TryRemove(subId, out _);
                await subscription.CloseInternalAsync();
                throw new InvalidOperationException($"Subscription failed: {failure.Message}");
            }

            return subscription;
        }
        catch
        {
            subscription.NotificationChannel.Writer.TryComplete();
            _subscriptions.TryRemove(subId, out _);
            _subscriptionInfos.TryRemove(subId, out _);
            throw;
        }
        finally
        {
            _pendingRequests.TryRemove(requestId, out _);
        }
    }

    public async Task UnsubscribeAsync(string subId, CancellationToken ct = default)
    {
        if (!_subscriptions.TryRemove(subId, out var subscription))
            return;

        _subscriptionInfos.TryRemove(subId, out _);
        subscription.NotificationChannel.Writer.TryComplete();

        var connection = _connections.Values.FirstOrDefault(c => c.Id == subscription.ConnectionId);
        if (connection is null || connection.State != WebSocketState.Open)
        {
            return;
        }

        var requestId = GetNextRequestId();
        var tcs = new TaskCompletionSource<RequestResult>();
        _pendingRequests[requestId] = new PendingRequest
        {
            Tcs = tcs,
            SubscriptionId = subId,
            CreatedAt = DateTime.UtcNow,
        };

        try
        {
            var request = new WsRequest
            {
                JsonRpc = "2.0",
                Method = WsRequestMethod.Unsubscribe,
                Params = new WsRequestParams { SubId = subId },
                Id = requestId,
            };

            await SendMessageAsync(connection, request, ct);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(30));

            var completed = await Task.WhenAny(tcs.Task, Task.Delay(Timeout.Infinite, cts.Token))
                .ConfigureAwait(false);

            if (completed == tcs.Task)
            {
                await tcs.Task.ConfigureAwait(false);
            }
        }
        catch
        {
            // unsubscribe failed, local cleanup already done
        }
        finally
        {
            _pendingRequests.TryRemove(requestId, out _);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        _disposeCts.Cancel();

        var mintUrls = _connections.Keys.ToList();
        foreach (var mintUrl in mintUrls)
        {
            try
            {
                await DisconnectAsync(mintUrl);
            }
            catch
            {
                // continue disposing other connections
            }
        }

        _subscriptions.Clear();
        _connections.Clear();
        _pendingRequests.Clear();
        _subscriptionInfos.Clear();

        foreach (var semaphore in _connectionLocks.Values)
        {
            semaphore.Dispose();
        }
        _connectionLocks.Clear();

        _disposeCts.Dispose();
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
            return Enumerable.Empty<Subscription>();
        }
        return _subscriptions.Values.Where(s => s.ConnectionId == connection.Id);
    }

    public IEnumerable<WebsocketConnection> GetConnections() => _connections.Values;

    public bool IsConnected(string mintUrl)
    {
        var normalized = NormalizeMintUrl(mintUrl);
        return _connections.TryGetValue(normalized, out var conn)
            && conn.State == WebSocketState.Open;
    }

    #region Message Handling

    private async Task ListenForMessagesAsync(WebsocketConnection connection, CancellationToken ct)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(4096);
        using var messageBuffer = new MemoryStream();

        try
        {
            while (connection.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                var result = await connection.WebSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    ct
                );

                connection.LastMessageReceived = DateTime.UtcNow;

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    connection.State = WebSocketState.Closed;
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    messageBuffer.Write(buffer, 0, result.Count);
                    if (result.EndOfMessage)
                    {
                        var message = Encoding.UTF8.GetString(messageBuffer.GetBuffer(), 0, (int)messageBuffer.Length);
                        messageBuffer.SetLength(0);
                        ProcessMessage(message);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on cancellation
        }
        catch
        {
            connection.State = WebSocketState.Aborted;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            connection.CancellationTokenSource?.Cancel();

            if (connection.State != WebSocketState.Closed)
            {
                OnConnectionStateChanged(connection.Id, WebSocketState.Aborted);
            }

            await CleanupConnectionSubscriptionsAsync(connection);

            if (_options.AutoReconnect && !_disposed && !_disposeCts.IsCancellationRequested)
            {
                _ = ReconnectAsync(connection.MintUrl, _disposeCts.Token);
            }
        }
    }

    private void ProcessMessage(string message)
    {
        try
        {
            using var doc = JsonDocument.Parse(message);
            var root = doc.RootElement;

            if (root.TryGetProperty("method", out var methodProp)
                && methodProp.GetString() == "subscribe")
            {
                var notification = JsonSerializer.Deserialize<WsNotification>(message, JsonOptions);
                if (notification != null)
                {
                    OnNotificationReceived(notification);
                }
            }
            else if (root.TryGetProperty("result", out _))
            {
                var response = JsonSerializer.Deserialize<WsResponse>(message, JsonOptions);
                if (response != null)
                {
                    HandleResponse(response);
                }
            }
            else if (root.TryGetProperty("error", out _))
            {
                var error = JsonSerializer.Deserialize<WsError>(message, JsonOptions);
                if (error != null)
                {
                    HandleError(error);
                }
            }
        }
        catch
        {
            // invalid message format, ignore
        }
    }

    private async Task SendMessageAsync<T>(WebsocketConnection connection, T message, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(message, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);

        await connection.WebSocket.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            true,
            ct
        );
    }

    private void HandleResponse(WsResponse response)
    {
        if (!_pendingRequests.TryGetValue(response.Id, out var pr))
            return;

        var result = new RequestResult.Success(response.Result.SubId, response.Result.Status);
        pr.Tcs.TrySetResult(result);

        if (_subscriptions.TryGetValue(pr.SubscriptionId, out var sub))
        {
            sub.NotificationChannel.Writer.TryWrite(new WsMessage.Response(response));
        }
    }

    private void HandleError(WsError error)
    {
        if (!_pendingRequests.TryGetValue(error.Id, out var pr))
            return;

        var result = new RequestResult.Failure(error.Error.Code, error.Error.Message, error.Id);
        pr.Tcs.TrySetResult(result);

        if (_subscriptions.TryGetValue(pr.SubscriptionId, out var sub))
        {
            sub.NotificationChannel.Writer.TryWrite(new WsMessage.Error(error));
        }
    }

    private void OnNotificationReceived(WsNotification notification)
    {
        if (_subscriptions.TryGetValue(notification.Params.SubId, out var sub))
        {
            sub.NotificationChannel.Writer.TryWrite(new WsMessage.Notification(notification));
        }
    }

    #endregion

    #region Heartbeat & Reconnect

    private async Task RunHeartbeatLoopAsync(WebsocketConnection connection, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && connection.State == WebSocketState.Open)
        {
            try
            {
                await Task.Delay(_options.HeartbeatInterval, ct);

                if (connection.State != WebSocketState.Open)
                    break;

                var lastReceived = connection.LastMessageReceived ?? connection.ConnectedAt;
                var timeSinceLastMessage = DateTime.UtcNow - lastReceived;

                if (timeSinceLastMessage > _options.HeartbeatInterval + _options.HeartbeatTimeout)
                {
                    connection.State = WebSocketState.Aborted;
                    connection.CancellationTokenSource?.Cancel();
                    break;
                }

                connection.LastPingSent = DateTime.UtcNow;
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ReconnectAsync(string mintUrl, CancellationToken ct)
    {
        var normalized = NormalizeMintUrl(mintUrl);
        var delay = _options.InitialReconnectDelay;

        for (int attempt = 1; attempt <= _options.MaxReconnectAttempts && !ct.IsCancellationRequested; attempt++)
        {
            try
            {
                await Task.Delay(delay, ct);

                var connectionLock = _connectionLocks.GetOrAdd(normalized, _ => new SemaphoreSlim(1, 1));
                await connectionLock.WaitAsync(ct);

                try
                {
                    if (_connections.TryGetValue(normalized, out var existing)
                        && existing.State == WebSocketState.Open)
                    {
                        return; // already reconnected
                    }

                    await ConnectAsync(mintUrl, ct);
                    await ResubscribeAllAsync(normalized, ct);
                    return;
                }
                finally
                {
                    connectionLock.Release();
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch
            {
                delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, _options.MaxReconnectDelay.Ticks));
            }
        }

        OnConnectionStateChanged(normalized, WebSocketState.Closed);
    }

    private async Task ResubscribeAllAsync(string mintUrl, CancellationToken ct)
    {
        var subsToRestore = _subscriptionInfos
            .Where(kvp => kvp.Value.MintUrl == mintUrl)
            .ToList();

        foreach (var (subId, info) in subsToRestore)
        {
            try
            {
                _subscriptions.TryRemove(subId, out var oldSub);
                _subscriptionInfos.TryRemove(subId, out _);
                if (oldSub != null)
                {
                    await oldSub.CloseInternalAsync();
                }

                await SubscribeAsync(mintUrl, info.Kind, info.Filters, ct);
            }
            catch
            {
                // failed to re-subscribe, continue with others
            }
        }
    }

    #endregion

    #region Cleanup & Utilities

    private async Task CleanupConnectionSubscriptionsAsync(WebsocketConnection connection)
    {
        var subscriptionsToClose = _subscriptions
            .Where(s => s.Value.ConnectionId == connection.Id)
            .ToList();

        foreach (var (subId, sub) in subscriptionsToClose)
        {
            try
            {
                await sub.CloseInternalAsync();
            }
            catch
            {
                // continue cleanup
            }
            _subscriptions.TryRemove(subId, out _);
        }
    }

    private async Task RunRequestCleanupLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_options.RequestCleanupInterval, ct);
                CleanupStaleRequests();
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void CleanupStaleRequests()
    {
        var staleThreshold = DateTime.UtcNow - _options.RequestTimeout;
        var staleRequests = _pendingRequests
            .Where(pr => pr.Value.CreatedAt < staleThreshold)
            .Select(pr => pr.Key)
            .ToList();

        foreach (var id in staleRequests)
        {
            if (_pendingRequests.TryRemove(id, out var pr))
            {
                pr.Tcs.TrySetException(new TimeoutException("Request expired"));
            }
        }
    }

    private async Task RunWithErrorHandlingAsync(Func<Task> action, WebsocketConnection connection)
    {
        try
        {
            await action();
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
        catch
        {
            connection.State = WebSocketState.Aborted;
            OnConnectionStateChanged(connection.Id, WebSocketState.Aborted);
        }
    }

    private int GetNextRequestId() => Interlocked.Increment(ref _nextRequestId);

    private void OnConnectionStateChanged(string connectionId, WebSocketState state)
    {
        ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs
        {
            ConnectionId = connectionId,
            State = state
        });
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

    private string GetWebSocketUrl(string mintUrl)
    {
        var uri = new Uri(NormalizeMintUrl(mintUrl));
        var scheme = uri.Scheme == "https" ? "wss" : "ws";
        var hostPort = uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}";
        var path = uri.AbsolutePath.TrimEnd('/');
        return $"{scheme}://{hostPort}{path}/v1/ws";
    }

    #endregion

    private class SubscriptionInfo
    {
        public required string MintUrl { get; init; }
        public required SubscriptionKind Kind { get; init; }
        public required string[] Filters { get; init; }
    }
}
