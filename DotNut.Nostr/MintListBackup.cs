using System.Text.Json.Serialization;
using DotNut.JsonConverters;

namespace DotNut.Nostr;

public class MintListBackup
{
    [JsonPropertyName("mints")]
    public string[] Mints { get; set; }

    [JsonPropertyName("timestamp")]
    [JsonConverter(typeof(UnixDateTimeOffsetConverter))]
    public DateTimeOffset Timestamp { get; private set; } = DateTimeOffset.UtcNow;
}
