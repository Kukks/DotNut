using System.Text.Json.Serialization;

public class BlindSignature
{
    [JsonPropertyName("amount")] public int Amount { get; set; }

    [JsonConverter(typeof(KeysetIdJsonConverter))]
    [JsonPropertyName("id")]
    public KeysetId Id { get; set; }

    [JsonPropertyName("C_")] public PubKey C_ { get; set; }
}