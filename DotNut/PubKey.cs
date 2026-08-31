using System.Text.Json.Serialization;
using DotNut.BLS12_381.Curve.G1;
using DotNut.BLS12_381.Curve.G2;
using DotNut.JsonConverters;
using NBitcoin.Secp256k1;

namespace DotNut;

[JsonConverter(typeof(PubKeyJsonConverter))]
public class PubKey
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public readonly ECPubKey? Key;

    private readonly byte[]? _blsG1Bytes;  // 48 bytes, BLS12-381 G1
    private readonly byte[]? _blsG2Bytes;  // 96 bytes, BLS12-381 G2

    /// <summary>True when this holds a BLS12-381 G1 point (v3 proof C / blinded message B_).</summary>
    public bool IsBlsG1 => _blsG1Bytes != null;

    /// <summary>True when this holds a BLS12-381 G2 point (v3 mint key).</summary>
    public bool IsBlsG2 => _blsG2Bytes != null;

    public PubKey(string hex, bool onlyAllowCompressed = false)
    {
        if (hex.Length == 96)  // BLS G1 compressed: 48 bytes
        {
            _blsG1Bytes = Convert.FromHexString(hex);
            return;
        }
        if (hex.Length == 192)  // BLS G2 compressed: 96 bytes
        {
            _blsG2Bytes = Convert.FromHexString(hex);
            return;
        }
        if (onlyAllowCompressed && hex.Length != 66)
            throw new ArgumentException("Only compressed public keys are allowed");
        Key = hex.ToPubKey();
    }

    private PubKey(ECPubKey ecPubKey)
    {
        Key = ecPubKey;
    }

    internal PubKey(G1Affine g1Point)
    {
        if (g1Point.IsInfinity)
            throw new ArgumentException("G1 point at infinity is not valid");
        _blsG1Bytes = g1Point.ToCompressed();
    }

    internal PubKey(G2Affine g2Point)
    {
        if (g2Point.IsInfinity)
            throw new ArgumentException("G2 point at infinity is not valid");
        _blsG2Bytes = g2Point.ToCompressed();
    }

    public G1Affine GetBlsG1Point()
    {
        if (_blsG1Bytes == null)
            throw new InvalidOperationException("Not a BLS G1 point. Check IsBlsG1 first.");
        if (!G1Affine.TryFromCompressed(_blsG1Bytes, out var p))
            throw new InvalidOperationException("Stored BLS G1 bytes are invalid");
        return p;
    }

    public G2Affine GetBlsG2Point()
    {
        if (_blsG2Bytes == null)
            throw new InvalidOperationException("Not a BLS G2 point. Check IsBlsG2 first.");
        if (!G2Affine.TryFromCompressed(_blsG2Bytes, out var p))
            throw new InvalidOperationException("Stored BLS G2 bytes are invalid");
        return p;
    }

    public override string ToString()
    {
        if (_blsG1Bytes != null) return Convert.ToHexString(_blsG1Bytes).ToLower();
        if (_blsG2Bytes != null) return Convert.ToHexString(_blsG2Bytes).ToLower();
        return Convert.ToHexString(Key!.ToBytes()).ToLower();
    }

    public static implicit operator PubKey(ECPubKey ecPubKey) => new(ecPubKey);

    public static implicit operator ECPubKey(PubKey pubKey) =>
        pubKey.Key ?? throw new InvalidOperationException(
            "This PubKey holds a BLS point. Use GetBlsG1Point() or GetBlsG2Point().");

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj)) return true;
        if (obj is not PubKey other) return false;
        if (IsBlsG1 != other.IsBlsG1 || IsBlsG2 != other.IsBlsG2) return false;
        if (IsBlsG1) return _blsG1Bytes!.SequenceEqual(other._blsG1Bytes!);
        if (IsBlsG2) return _blsG2Bytes!.SequenceEqual(other._blsG2Bytes!);
        return Key == other.Key;
    }

    public override int GetHashCode()
    {
        if (_blsG1Bytes != null)
        {
            var h = new HashCode();
            foreach (var b in _blsG1Bytes) h.Add(b);
            return h.ToHashCode();
        }
        if (_blsG2Bytes != null)
        {
            var h = new HashCode();
            foreach (var b in _blsG2Bytes) h.Add(b);
            return h.ToHashCode();
        }
        return Key!.GetHashCode();
    }
}
