using System.Net.WebSockets;

namespace DotNut.Abstractions.Websockets;

public class WebsocketConnection : IDisposable
{
    public string Id { get; set; } = string.Empty;
    public string MintUrl { get; set; } = string.Empty;
    public ClientWebSocket WebSocket { get; set; } = new();
    public WebSocketState State { get; set; }
    public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
    public CancellationTokenSource? CancellationTokenSource { get; set; }

    public DateTime? LastPingSent { get; set; }
    public DateTime? LastMessageReceived { get; set; }
    public int ReconnectAttempts { get; set; }

    public bool Equals(WebsocketConnection? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return string.Equals(MintUrl, other.MintUrl, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj)
    {
        return obj is WebsocketConnection other && Equals(other);
    }

    public override int GetHashCode()
    {
        return MintUrl?.GetHashCode(StringComparison.OrdinalIgnoreCase) ?? 0;
    }

    public static bool operator ==(WebsocketConnection? left, WebsocketConnection? right)
    {
        return object.Equals(left, right);
    }

    public static bool operator !=(WebsocketConnection? left, WebsocketConnection? right)
    {
        return !object.Equals(left, right);
    }

    public void Dispose()
    {
        CancellationTokenSource?.Cancel();
        CancellationTokenSource?.Dispose();
        WebSocket?.Dispose();
    }
}
