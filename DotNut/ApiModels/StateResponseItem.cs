using System.Text.Json.Serialization;

namespace DotNut.ApiModels;

public class StateResponseItem
{
    
    public string Y { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TokenState State { get; set; }
    public string? Witness { get; set; }

    public enum TokenState
    {
        UNSPENT,
        PENDING,
        SPENT
    }
}