using System.Text.Json.Serialization;

public class ProofCarol:Proof
{
    [JsonPropertyName("dleq")] public DLEQ DLEQ { get; set; }
}