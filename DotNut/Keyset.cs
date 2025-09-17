using System.Net.Mime;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using DotNut.JsonConverters;
using SHA256 = System.Security.Cryptography.SHA256;

namespace DotNut;

[JsonConverter(typeof(KeysetJsonConverter))]
public class Keyset : Dictionary<ulong, PubKey>
{
    public KeysetId GetKeysetId(byte version = 0x00, string? unit = null, string? finalExpiration = null)
    {
        // 1 - sort public keys by their amount in ascending order
        // 2 - concatenate all public keys to a single byte array
        if (Count == 0) throw new InvalidOperationException("Keyset cannot be empty.");
        var sortedBytes = this
            .OrderBy(x => x.Key)
            .Select(pair => pair.Value.Key.ToBytes())
            .SelectMany(b => b)
            .ToArray();
        
        using SHA256 sha256 = SHA256.Create();


        switch (version)
        {
            // 3 - HASH_SHA256 the concatenated public keys
            // 4 - take the first 14 characters of the hex-encoded hash
            // 5 - prefix it with a keyset ID version byte
            case 0x00:
            {
                var hash = sha256.ComputeHash(sortedBytes);
                return new KeysetId(Convert.ToHexString(new []{version}) + Convert.ToHexString(hash).Substring(0, 14).ToLower());
            }
            // 3 - add the lowercase unit string to the byte array (e.g. "unit:sat")
            // 4 - If a final expiration is specified, convert it into a radix-10 string and add it (e.g "final_expiry:1896187313")
            // 4 - HASH_SHA256 the concatenated byte array
            // 5 - prefix it with a keyset ID version byte
            case 0x01:
            {
                if (String.IsNullOrWhiteSpace(unit))
                { 
                    throw new ArgumentNullException( nameof(unit), $"Unit parameter is required with version: {version}");
                }
                sortedBytes = sortedBytes.Concat(Encoding.UTF8.GetBytes($"unit:{unit.Trim().ToLowerInvariant()}")).ToArray();
                
                if (!string.IsNullOrWhiteSpace(finalExpiration))
                {
                    sortedBytes = sortedBytes.Concat(Encoding.UTF8.GetBytes($"final_expiry:{finalExpiration.Trim()}"))
                        .ToArray();
                }

                var hash = sha256.ComputeHash(sortedBytes);
                return new KeysetId(Convert.ToHexString(new[] { version }) +
                                    Convert.ToHexString(hash).ToLower());
            }
            default:
                throw new ArgumentException($"Unsupported keyset version: {version}");
        }
        
    }

public bool VerifyKeysetId(KeysetId keysetId, string? unit = null, string? finalExpiration = null)
{
    byte version = keysetId.GetVersion();
    var derived = GetKeysetId(version, unit, finalExpiration).ToString();
    var presented = keysetId.ToString();
    if (presented.Length > derived.Length) return false;
    return string.Equals(derived, presented, StringComparison.Ordinal) ||
           derived.StartsWith(presented, StringComparison.Ordinal);
}
}