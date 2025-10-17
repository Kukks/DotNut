using System.Text.Json.Serialization;

namespace DotNut.Abstractions.Websockets;

public class WsRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc = "2.0";

    [JsonPropertyName("method")]
    public WsRequestMethod Method { get; set; }

    [JsonPropertyName("params")]
    public WsRequestParams Params { get; set; } = new();

    [JsonPropertyName("id")]
    public int Id { get; set; }
}

public class WsRequestParams
{
    [JsonPropertyName("kind")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SubscriptionKind? Kind { get; set; }

    [JsonPropertyName("subId")]
    public string SubId { get; set; } = string.Empty;

    [JsonPropertyName("filters")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? Filters { get; set; }
}

public class WsResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("result")]
    public WsResult Result { get; set; } = new();

    [JsonPropertyName("id")]
    public int Id { get; set; }
}

public class WsResult
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("subId")]
    public string SubId { get; set; } = string.Empty;
}

public class WsError
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("error")]
    public WsErrorDetails Error { get; set; } = new();

    [JsonPropertyName("id")]
    public int Id { get; set; }
}

public class WsErrorDetails
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

public class WsNotification
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("method")]
    public string Method { get; set; } = "subscribe";

    [JsonPropertyName("params")]
    public WsNotificationParams Params { get; set; } = new();
}

public class WsNotificationParams
{
    [JsonPropertyName("subId")]
    public string SubId { get; set; } = string.Empty;

    [JsonPropertyName("payload")]
    public object? Payload { get; set; }
}
