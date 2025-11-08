using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using DotNut.JsonConverters;

namespace DotNut;

[JsonConverter(typeof(KeysetIdJsonConverter))]
public class KeysetId : IEquatable<KeysetId>,IEqualityComparer<KeysetId>
{
    public bool Equals(KeysetId? x, KeysetId? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (ReferenceEquals(x, null)) return false;
        if (ReferenceEquals(y, null)) return false;
        if (x.GetType() != y.GetType()) return false;
        return string.Equals(x._id, y._id, StringComparison.InvariantCultureIgnoreCase);
    }

    public int GetHashCode(KeysetId obj)
    {
        return StringComparer.InvariantCultureIgnoreCase.GetHashCode(obj._id);
    }

    public bool Equals(KeysetId? other)
    {
        return Equals(this, other);
    }

    public override bool Equals(object? obj)
    {
        
        return Equals(this, obj as KeysetId);
    }

    public override int GetHashCode()
    {
        return StringComparer.InvariantCultureIgnoreCase.GetHashCode(_id);
    }

    public static bool operator ==(KeysetId? left, KeysetId? right)
    {
        return (left is null && right is null) || left?.Equals(right) is true || right?.Equals(left) is true;
    }

    public static bool operator !=(KeysetId? left, KeysetId? right)
    {
        return !(left == right);
    }
    

    private readonly string _id;

    public KeysetId(string Id)
    {
        // Legacy support for all keyset formats
        if (Id.Length != 66 && Id.Length != 16 && Id.Length != 12)
        {
            throw new ArgumentException("KeysetId must be 66, 16 or 12 (legacy) characters long");
        }
        _id = Id;
    }

    public KeysetId(Keyset keyset)
    {
        _id = keyset.GetKeysetId().ToString();
    }

    public override string ToString()
    {
        return _id;
    }

    public byte GetVersion()
    {
        string versionStr = _id.Substring(0, 2);
        return Convert.ToByte(versionStr, 16);
    }

    public byte[] GetBytes()
    {
        return Convert.FromHexString(_id);
    }
}