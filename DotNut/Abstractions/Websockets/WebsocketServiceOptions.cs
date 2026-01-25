namespace DotNut.Abstractions.Websockets;

/// <summary>
/// Configuration options for WebsocketService
/// </summary>
public class WebsocketServiceOptions
{
    /// <summary>
    /// Whether to automatically reconnect when connection is lost.
    /// Default: true
    /// </summary>
    public bool AutoReconnect { get; set; } = true;

    /// <summary>
    /// Maximum number of reconnect attempts before giving up.
    /// Default: 10
    /// </summary>
    public int MaxReconnectAttempts { get; set; } = 10;

    /// <summary>
    /// Initial delay before first reconnect attempt.
    /// Default: 1 second
    /// </summary>
    public TimeSpan InitialReconnectDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Maximum delay between reconnect attempts (exponential backoff cap).
    /// Default: 5 minutes
    /// </summary>
    public TimeSpan MaxReconnectDelay { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Interval between heartbeat checks.
    /// Default: 30 seconds
    /// </summary>
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Timeout for heartbeat response before considering connection dead.
    /// Default: 10 seconds
    /// </summary>
    public TimeSpan HeartbeatTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Maximum number of messages in subscription channel before dropping oldest.
    /// Default: 1000
    /// </summary>
    public int MaxChannelCapacity { get; set; } = 1000;

    /// <summary>
    /// Timeout for pending requests before they are cleaned up.
    /// Default: 5 minutes
    /// </summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Interval for cleaning up stale pending requests.
    /// Default: 1 minute
    /// </summary>
    public TimeSpan RequestCleanupInterval { get; set; } = TimeSpan.FromMinutes(1);
}
