using System.Text.Json.Serialization;

namespace DotNut.ApiModels;

public class GetKeysetsResponse
{
    [JsonPropertyName("keysets")] public KeysetItemResponse[] Keysets { get; set; }

    public class KeysetItemResponse
    {
        [JsonPropertyName("id")] public KeysetId Id { get; set; }
        [JsonPropertyName("unit")] public string Unit { get; set; }
        [JsonPropertyName("active")] public bool Active { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("input_fee_ppk")] public int? InputFee { get; set; }
    }
}