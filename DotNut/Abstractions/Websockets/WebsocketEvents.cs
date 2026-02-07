using System.Net.WebSockets;

namespace DotNut.Abstractions.Websockets;

public class NotificationEventArgs : EventArgs
{
    public string ConnectionId { get; set; } = string.Empty;
    public WsNotification Notification { get; set; } = new();
}

public class ConnectionStateChangedEventArgs : EventArgs
{
    public string ConnectionId { get; set; } = string.Empty;
    public WebSocketState State { get; set; }
}
