using DotNut.BLS12_381.Curve.G2;

namespace DotNut;

/// <summary>
/// A BLS12-381 G2 compressed point (96 bytes / 192 hex chars).
/// Used for mint public keys in v3 keysets.
/// </summary>
public class BlsG2PubKey
{
    public readonly G2Affine Point;

    public BlsG2PubKey(G2Affine point)
    {
        if (point.IsInfinity)
            throw new ArgumentException("G2 point at infinity is not valid");
        Point = point;
    }

    public BlsG2PubKey(string hex)
    {
        var bytes = Convert.FromHexString(hex);
        if (!G2Affine.TryFromCompressed(bytes, out var point))
            throw new ArgumentException($"Invalid G2 compressed point: {hex}");
        if (point.IsInfinity)
            throw new ArgumentException("G2 point at infinity is not valid");
        Point = point;
    }

    public BlsG2PubKey(byte[] compressed)
    {
        if (!G2Affine.TryFromCompressed(compressed, out var point))
            throw new ArgumentException("Invalid G2 compressed bytes");
        if (point.IsInfinity)
            throw new ArgumentException("G2 point at infinity is not valid");
        Point = point;
    }

    public byte[] ToCompressedBytes() => Point.ToCompressed();

    // 192 hex chars (96 compressed bytes)
    public override string ToString() => Convert.ToHexString(ToCompressedBytes()).ToLower();

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj)) return true;
        return obj is BlsG2PubKey other && Point == other.Point;
    }

    public override int GetHashCode() => Point.GetHashCode();
}
