using System.Text.Json.Serialization;

namespace DotNut.ApiModels;

public class PostBatchedMintRequest
{
    [JsonPropertyName("quotes")]
    public string[] QuoteIds { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyName("amounts")]
    public ulong?[]? Amounts { get; set; }
    
    [JsonPropertyName("outputs")]
    public BlindedMessage[] Outputs { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyName("signatures")]
    public string?[]? Signatures { get; set; }
}