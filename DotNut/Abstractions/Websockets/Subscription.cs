using System.Threading.Channels;

namespace DotNut.Abstractions.Websockets;

public class Subscription
{
    public string Id { get; set; } = string.Empty;
    public string ConnectionId { get; set; } = string.Empty;
    public SubscriptionKind Kind { get; set; }
    public string[] Filters { get; set; } = Array.Empty<string>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Channel<WsNotificationParams> NotificationChannel { get; set; }
    
    public EventHandler<WsError> OnError { get; set; }
    
    public void Close()
    {
        NotificationChannel.Writer.TryComplete();
    }
}
