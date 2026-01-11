using System.Text.Json.Serialization;

namespace DotNut.ApiModels;

public class SwapInfo
{
    [JsonPropertyName("methods")]
    public SwapMethod[] Methods { get; set; }

    [JsonPropertyName("disabled")]
    public bool Disabled { get; set; }

    public class SwapMethod
    {
        [JsonPropertyName("method")]
        public string Method { get; set; }

        [JsonPropertyName("unit")]
        public string Unit { get; set; }

        [JsonPropertyName("min_amount")]
        public ulong MinAmount { get; set; }

        [JsonPropertyName("max_amount")]
        public ulong MaxAmount { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("options")]
        public SwapOptions? Options { get; set; }

        public class SwapOptions
        {
            [JsonPropertyName("description")]
            public bool? Description { get; set; }
        }
    }
}
