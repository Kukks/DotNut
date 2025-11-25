using System.Text.Json.Serialization;

namespace DotNut;

public class CashuToken
{
    public class Token
    {
        public Token()
        {
        }

        public Token(string mint, List<Proof> proofs)
        {
            Mint = mint;
            Proofs = proofs;
        }

        [JsonPropertyName("mint")] public string Mint { get; set; }
        [JsonPropertyName("proofs")] public List<Proof> Proofs { get; set; }
    }

    [JsonPropertyName("token")] public List<Token> Tokens { get; set; }

    [JsonPropertyName("unit")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Unit { get; set; }

    [JsonPropertyName("memo")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Memo { get; set; }
}