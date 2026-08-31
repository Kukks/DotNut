using DotNut.BLS12_381.Curve.G1;

namespace DotNut;

/// <summary>
/// A BLS12-381 G1 compressed point (48 bytes / 96 hex chars).
/// Used for proof commitments (C) and blinded messages (B_) in v3 keysets.
/// </summary>
public class BlsG1PubKey
{
    public readonly G1Affine Point;

    public BlsG1PubKey(G1Affine point)
    {
        if (point.IsInfinity)
            throw new ArgumentException("G1 point at infinity is not valid");
        Point = point;
    }

    public BlsG1PubKey(string hex, bool compressed = true)
    {
        var bytes = Convert.FromHexString(hex);
        if (compressed)
        {
            if (!G1Affine.TryFromCompressed(bytes, out var pt))
                throw new ArgumentException($"Invalid G1 compressed point: {hex}");
            if (pt.IsInfinity)
                throw new ArgumentException("G1 point at infinity is not valid");
            Point = pt;
        }
        else
        {
            if (!G1Affine.TryFromUncompressed(bytes, out var pt))
                throw new ArgumentException($"Invalid G1 uncompressed point: {hex}");
            if (pt.IsInfinity)
                throw new ArgumentException("G1 point at infinity is not valid");
            Point = pt;
        }
    }

    public BlsG1PubKey(byte[] compressed)
    {
        if (!G1Affine.TryFromCompressed(compressed, out var point))
            throw new ArgumentException("Invalid G1 compressed bytes");
        if (point.IsInfinity)
            throw new ArgumentException("G1 point at infinity is not valid");
        Point = point;
    }

    public byte[] ToCompressedBytes() => Point.ToCompressed();

    // 96 hex chars (48 compressed bytes)
    public override string ToString() => Convert.ToHexString(ToCompressedBytes()).ToLower();

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj)) return true;
        return obj is BlsG1PubKey other && Point == other.Point;
    }

    public override int GetHashCode() => Point.GetHashCode();

    public static implicit operator G1Affine(BlsG1PubKey a) => a.Point;
    public static implicit operator G1Projective(BlsG1PubKey a) => a.Point.ToProjective();
    public static implicit operator BlsG1PubKey(G1Affine a) => new(a);
}
