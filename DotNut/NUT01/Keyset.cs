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
    public KeysetId GetKeysetId(
        byte version = 0x00,
        string? unit = null,
        ulong? inputFeePpk = null,
        ulong? finalExpiration = null
    )
    {
        // 1 - sort public keys by their amount in ascending order
        if (Count == 0)
            throw new InvalidOperationException("Keyset cannot be empty.");
        var sortedKeys = this.OrderBy(x => x.Key);

        using SHA256 sha256 = SHA256.Create();

        switch (version)
        {
            case 0x00:
            {
                // 2 - concatenate all public keys to a single byte array
                var sortedBytes = sortedKeys
                    .Select(pair => pair.Value.Key.ToBytes())
                    .SelectMany(b => b)
                    .ToArray();
                // 3 - HASH_SHA256 the concatenated public keys
                var hash = sha256.ComputeHash(sortedBytes);
                // 4 - take the first 14 characters of the hex-encoded hash
                // 5 - prefix it with a keyset ID version byte
                return new KeysetId(
                    Convert.ToHexString(new[] { version })
                        + Convert.ToHexString(hash).Substring(0, 14).ToLower()
                );
            }

            case 0x01:
            {
                MemoryStream stream = new MemoryStream();

                // 2 - concatenate each amount and its corresponding public key hex string (as "amount:publickey_hex")
                // to a single byte array, separating each pair with a comma (",")
                var sortedBytes = Encoding.UTF8.GetBytes(
                    string.Join(
                        ",",
                        sortedKeys.Select(pair =>
                            $"{pair.Key}:{pair.Value.ToString().ToLowerInvariant()}"
                        )
                    )
                );

                stream.Write(sortedBytes, 0, sortedBytes.Length);

                // 3 - add the lowercase UTF8-encoded unit string prefixed with "|unit:" to the byte array (e.g. "|unit:sat")
                if (String.IsNullOrWhiteSpace(unit))
                {
                    throw new ArgumentNullException(
                        nameof(unit),
                        $"Unit parameter is required with version: {version}"
                    );
                }

                var unitBytes = Encoding.UTF8.GetBytes($"|unit:{unit.Trim().ToLowerInvariant()}");
                stream.Write(unitBytes, 0, unitBytes.Length);

                // 4 - If input_fee_ppk is specified and non-zero, add the UTF8-encoded string prefixed with
                // "|input_fee_ppk:" (e.g. "|input_fee_ppk:100").
                // If input_fee_ppk is omitted, null, or 0, it MUST be omitted from the preimage.
                if (inputFeePpk.HasValue && inputFeePpk.Value != 0)
                {
                    var feeBytes = Encoding.UTF8.GetBytes($"|input_fee_ppk:{inputFeePpk.Value}");
                    stream.Write(feeBytes, 0, feeBytes.Length);
                }

                // 5 - If a final expiration is specified, add the UTF8-encoded string prefixed with "|final_expiry:" (e.g. "|final_expiry:1896187313")
                if (finalExpiration is not null)
                {
                    var expiryBytes = Encoding.UTF8.GetBytes(
                        $"|final_expiry:{finalExpiration.ToString()}"
                    );
                    stream.Write(expiryBytes, 0, expiryBytes.Length);
                }

                // 6 - HASH_SHA256 the concatenated byte array
                var hash = sha256.ComputeHash(stream.ToArray());

                // 7 - prefix it with a keyset ID version byte "01"
                return new KeysetId(
                    Convert.ToHexString(new[] { version }) + Convert.ToHexString(hash).ToLower()
                );
            }
            default:
                throw new ArgumentException($"Unsupported keyset version: {version}");
        }
    }

    public bool VerifyKeysetId(
        KeysetId keysetId,
        string? unit = null,
        ulong? inputFeePpk = null,
        ulong? finalExpiration = null
    )
    {
        byte version = keysetId.GetVersion();
        var derived = GetKeysetId(version, unit, inputFeePpk, finalExpiration).ToString();
        var presented = keysetId.ToString();
        if (presented.Length > derived.Length)
            return false;
        return string.Equals(derived, presented, StringComparison.InvariantCultureIgnoreCase)
            || derived.StartsWith(presented, StringComparison.InvariantCultureIgnoreCase);
    }
}
