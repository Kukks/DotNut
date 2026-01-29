using System.Text.Json.Serialization;

namespace DotNut.ApiModels.Info;

public class MPPInfo
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyName("methods")]
    public MPPMethod[]? Methods { get; set; }

    public class MPPMethod
    {
        [JsonPropertyName("method")]
        public string Method { get; set; }

        [JsonPropertyName("unit")]
        public string Unit { get; set; }
    }
}
