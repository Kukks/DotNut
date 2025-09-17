using System.Text.Json.Serialization;
using DotNut.JsonConverters;
using NBitcoin.Secp256k1;

namespace DotNut;

[JsonConverter(typeof(PubKeyJsonConverter))]
public class PubKey
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)] public readonly ECPubKey Key;

    public PubKey(string hex, bool onlyAllowCompressed = false)
    {
        if (onlyAllowCompressed && hex.Length != 66)
        {
            throw new ArgumentException("Only compressed public keys are allowed");
        }
        Key = hex.ToPubKey();
    }

    private PubKey(ECPubKey ecPubKey)
    {
        Key = ecPubKey;
    }

    public override string ToString()
    {
        return Convert.ToHexString(Key.ToBytes()).ToLower();
    }
    
    public static implicit operator PubKey(ECPubKey ecPubKey)
    {
        return new PubKey(ecPubKey);
    }

    public static implicit operator ECPubKey(PubKey pubKey)
    {
        return pubKey.Key;
    }
    
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj)) return true;
        if (obj is not PubKey other) return false;
        return this.Key == other.Key;
    }
    
    public override int GetHashCode()
    {
        return Key.GetHashCode();
    }
 
}