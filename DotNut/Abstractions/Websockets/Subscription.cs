using System.Threading.Channels;

namespace DotNut.Abstractions.Websockets;

public class Subscription
{
    public string Id { get; set; } = string.Empty;
    public string ConnectionId { get; set; } = string.Empty;
    public SubscriptionKind Kind { get; set; }
    public string[] Filters { get; set; } = Array.Empty<string>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Channel<WsMessage> NotificationChannel { get; set; } = Channel.CreateUnbounded<WsMessage>();
    
    public EventHandler<WsError>? OnError { get; set; }
    
    private readonly WeakReference<IWebsocketService>? _serviceRef;
    
    public Subscription(IWebsocketService? websocketService = null)
    {
        _serviceRef = websocketService != null ? 
            new WeakReference<IWebsocketService>(websocketService) : null;
    }

    public async Task CloseAsync()
    {
        NotificationChannel.Writer.TryComplete();
        if (_serviceRef != null && _serviceRef.TryGetTarget(out var service))
        {
            await service.UnsubscribeAsync(Id);
        }
    }
}
