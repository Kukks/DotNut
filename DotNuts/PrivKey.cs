using System.Text.Json.Serialization;
using NBitcoin.Secp256k1;

[JsonConverter(typeof(PrivKeyJsonConverter))]
public class PrivKey
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)] public readonly ECPrivKey Key;

    public PrivKey(string hex)
    {
        Key = hex.ToPrivKey();
    }

    private PrivKey(ECPrivKey ecPrivKey)
    {
        Key = ecPrivKey;
    }

    public override string ToString()
    {
        return Convert.ToHexString(Key.ToBytes()).ToLower();
    }
    
    public static implicit operator PrivKey(ECPrivKey ecPubKey)
    {
        return new PrivKey(ecPubKey);
    }

    public static implicit operator ECPrivKey(PrivKey privKey)
    {
        return privKey.Key;
    }
}