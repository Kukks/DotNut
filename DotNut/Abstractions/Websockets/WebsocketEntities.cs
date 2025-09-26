using System.Net.WebSockets;

namespace DotNut.Abstractions.Websockets;

public class WebSocketConnection
{
    public string Id { get; set; } = string.Empty;
    public string MintUrl { get; set; } = string.Empty;
    public ClientWebSocket WebSocket { get; set; } = new();
    public WebSocketState State { get; set; }
    public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
    
    public bool Equals(WebSocketConnection? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(MintUrl, other.MintUrl, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj)
    {
        return obj is WebSocketConnection other && Equals(other);
    }

    public override int GetHashCode()
    {
        return MintUrl?.GetHashCode(StringComparison.OrdinalIgnoreCase) ?? 0;
    }

    public static bool operator ==(WebSocketConnection? left, WebSocketConnection? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(WebSocketConnection? left, WebSocketConnection? right)
    {
        return !Equals(left, right);
    }
}

public class Subscription
{
    public string Id { get; set; } = string.Empty;
    public string ConnectionId { get; set; } = string.Empty;
    public SubscriptionKind Kind { get; set; }
    public string[] Filters { get; set; } = Array.Empty<string>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
