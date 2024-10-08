using System.Text.Json;
using System.Text.Json.Serialization;
using DotNut.JsonConverters;

namespace DotNut.ApiModels;

public class GetInfoResponse
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyName("pubkey")]
    public string? Pubkey { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyName("description_long")]
    public string? DescriptionLong { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyName("contact")]
    public List<ContactInfo>? Contact { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyName("motd")]
    public string? Motd { get; set; }


    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyName("icon_url")]
    public string? IconUrl { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonConverter(typeof(UnixDateTimeOffsetConverter))]
    [JsonPropertyName("time")]
    public DateTimeOffset? Time { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyName("nuts")]
    public Dictionary<string, JsonDocument>? Nuts { get; set; }
}

public class ContactInfo
{
    [JsonPropertyName("method")] public string Method { get; set; }
    [JsonPropertyName("info")] public string Info { get; set; }
}