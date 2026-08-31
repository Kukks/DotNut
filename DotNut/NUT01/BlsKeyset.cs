using System.Text;
using System.Text.Json.Serialization;
using DotNut.JsonConverters;
using SHA256 = System.Security.Cryptography.SHA256;

namespace DotNut;

/// <summary>
/// A v3 (BLS12-381) keyset: maps amounts to G2 public keys (96 bytes / 192 hex chars each).
/// Entries in the base <see cref="Keyset"/> dictionary are <see cref="PubKey"/> instances
/// with <see cref="PubKey.IsBlsG2"/> == true.
/// </summary>
[JsonConverter(typeof(BlsKeysetJsonConverter))]
public class BlsKeyset : Keyset
{
    public KeysetId GetKeysetId(
        string? unit = null,
        ulong? inputFeePpk = null,
        ulong? finalExpiration = null
    )
    {
        if (Count == 0)
        {
            throw new InvalidOperationException("Keyset cannot be empty.");
        }
        if (string.IsNullOrWhiteSpace(unit))
        {
            throw new ArgumentNullException(nameof(unit), "Unit is required for v3 keyset ID.");
        }

        var sortedKeys = this.OrderBy(x => x.Key);

        using var sha256 = SHA256.Create();
        using var stream = new MemoryStream();

        var preimage = string.Join(",",
            sortedKeys.Select(p => $"{p.Key}:{p.Value.ToString().ToLowerInvariant()}"));
        stream.Write(Encoding.UTF8.GetBytes(preimage));
        stream.Write(Encoding.UTF8.GetBytes($"|unit:{unit.Trim().ToLowerInvariant()}"));

        if (inputFeePpk.HasValue && inputFeePpk.Value != 0)
        {
            stream.Write(Encoding.UTF8.GetBytes($"|input_fee_ppk:{inputFeePpk.Value}"));
        }

        if (finalExpiration is not null)
        {
            stream.Write(Encoding.UTF8.GetBytes($"|final_expiry:{finalExpiration}"));
        }
        var hash = sha256.ComputeHash(stream.ToArray());
        return new KeysetId("02" + Convert.ToHexString(hash).ToLower());
    }

    public override bool VerifyKeysetId(
        KeysetId keysetId,
        string? unit = null,
        ulong? inputFeePpk = null,
        ulong? finalExpiration = null
    )
    {
        var derived = GetKeysetId(unit, inputFeePpk, finalExpiration).ToString();
        var presented = keysetId.ToString();
        if (presented.Length > derived.Length)
        {
            return false;
        }
        return string.Equals(derived, presented, StringComparison.InvariantCultureIgnoreCase)
            || derived.StartsWith(presented, StringComparison.InvariantCultureIgnoreCase);
    }
}
