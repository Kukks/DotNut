using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace DotNut.Abstractions.Websockets;

public class Subscription : IAsyncDisposable
{
    public string Id { get; init; } = string.Empty;
    public string ConnectionId { get; init; } = string.Empty;
    public SubscriptionKind Kind { get; init; }
    public string[] Filters { get; init; } = Array.Empty<string>();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public Channel<WsMessage> NotificationChannel { get; init; } =
        Channel.CreateUnbounded<WsMessage>();

    /// <summary>
    /// Indicates whether the subscription is still active (channel not completed).
    /// </summary>
    public bool IsActive => !_isClosed;

    private volatile bool _isClosed;
    private readonly WeakReference<IWebsocketService>? _serviceRef;

    public Subscription(IWebsocketService? websocketService = null)
    {
        _serviceRef =
            websocketService != null
                ? new WeakReference<IWebsocketService>(websocketService)
                : null;
    }

    /// <summary>
    /// Reads all notifications as an async stream. Completes when the subscription is closed.
    /// </summary>
    public async IAsyncEnumerable<WsMessage> ReadAllAsync(
        [EnumeratorCancellation] CancellationToken ct = default
    )
    {
        await foreach (var msg in NotificationChannel.Reader.ReadAllAsync(ct))
        {
            yield return msg;
        }
    }

    /// <summary>
    /// Closes the subscription and sends unsubscribe request to the server.
    /// </summary>
    public async Task CloseAsync()
    {
        if (_isClosed)
            return;
        _isClosed = true;

        NotificationChannel.Writer.TryComplete();
        if (_serviceRef != null && _serviceRef.TryGetTarget(out var service))
        {
            await service.UnsubscribeAsync(Id);
        }
    }

    /// <summary>
    /// Internal close - only closes the channel without server notification.
    /// Used when connection is already closed or during cleanup.
    /// </summary>
    internal Task CloseInternalAsync()
    {
        _isClosed = true;
        NotificationChannel.Writer.TryComplete();
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await CloseAsync();
    }
}
