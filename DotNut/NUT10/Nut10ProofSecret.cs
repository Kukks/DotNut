using System.Text.Json.Serialization;

namespace DotNut;

public class Nut10ProofSecret
{
    [JsonPropertyName("nonce")]
    public string Nonce { get; set; }

    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Data { get; set; }

    [JsonPropertyName("tags")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[][]? Tags { get; set; }

    public override bool Equals(object obj) => this.Equals(obj as Nut10ProofSecret);

    public bool Equals(Nut10ProofSecret s)
    {
        if (s is null)
        {
            return false;
        }

        if (Object.ReferenceEquals(this, s))
        {
            return true;
        }

        if (this.GetType() != s.GetType())
        {
            return false;
        }

        return this.Nonce == s.Nonce
            && this.Data == s.Data
            && (
                (this.Tags == null && s.Tags == null)
                || (
                    this.Tags != null
                    && s.Tags != null
                    && this.Tags.Length == s.Tags.Length
                    && this.Tags.Zip(s.Tags).All(pair => pair.First.SequenceEqual(pair.Second))
                )
            );
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(this.Nonce);
        hash.Add(this.Data);

        if (this.Tags == null)
        {
            return hash.ToHashCode();
        }
        foreach (var tagArray in this.Tags)
        {
            foreach (var tag in tagArray)
            {
                hash.Add(tag);
            }
        }
        return hash.ToHashCode();
    }

    public static bool operator ==(Nut10ProofSecret first, Nut10ProofSecret second)
    {
        if (first is null)
        {
            return second is null;
        }
        return first.Equals(second);
    }

    public static bool operator !=(Nut10ProofSecret first, Nut10ProofSecret second) =>
        !(first == second);

    /// <summary>
    /// Helper for SIG_ALL equality check. Every proof has to have identical data and tags fields
    /// </summary>
    public bool SigAllEquals(Nut10ProofSecret other)
    {
       return other is { } o
            && this.Data == o.Data
            && (
                (this.Tags == null && o.Tags == null)
                || (
                    this.Tags != null
                    && o.Tags != null
                    && this.Tags.Length == o.Tags.Length
                    && this.Tags.Zip(o.Tags).All(pair => pair.First.SequenceEqual(pair.Second))
                )
            );
    }
}
