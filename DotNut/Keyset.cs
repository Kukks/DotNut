using System.Text.Json.Serialization;
using DotNut.JsonConverters;
using SHA256 = System.Security.Cryptography.SHA256;

namespace DotNut;

[JsonConverter(typeof(KeysetJsonConverter))]

public class Keyset : Dictionary<ulong, PubKey>
{
    [Obsolete("Not really used anymore")]
    public KeysetId GetKeysetV0Id(byte version = 0x00)
    {
        // 1 - sort public keys by their amount in ascending order
        // 2 - concatenate all public keys to one byte array
        // 3 - HASH_SHA256 the concatenated public keys
        // 4 - take the first 14 characters of the hex-encoded hash
        // 5 - prefix it with a keyset ID version byte

        var preimage = this.OrderBy(x => x.Key).Select(pair => pair.Value.Key.ToBytes())
            .Aggregate((a, b) => a.Concat(b).ToArray());
        using SHA256 sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(preimage);
        return new KeysetId(Convert.ToHexString(new []{version}) + Convert.ToHexString(hash).Substring(0, 14).ToLower());
    }
    
    public KeysetId GetKeysetId()
    {
        //     1 - sort keyset by amount
        //     2 - concatenate all (sorted) public keys to one string
        //     3 - HASH_SHA256 the concatenated public keys
        //     4 - take the first 12 characters of the hash
    
        var preimage = this.OrderBy(x => x.Key).Select(pair => pair.Value.Key.ToBytes())
            .Aggregate((a, b) => a.Concat(b).ToArray());
        using SHA256 sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(preimage);
        return new KeysetId(Convert.ToHexString(hash).Substring(0, 12).ToLower());
    }
}

