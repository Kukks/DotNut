using System.Text.Json;
using System.Text.Json.Serialization;
using DotNut.JsonConverters;
using NBitcoin.Secp256k1;

namespace DotNut;

[JsonConverter(typeof(Nut10SecretJsonConverter))]
public class Nut10Secret : ISecret
{
    private readonly string? _originalString;

    public Nut10Secret(string key, Nut10ProofSecret proofSecret)
    {
        Key = key;
        ProofSecret = proofSecret;
    }

    public Nut10Secret(string originalString)
    {
        _originalString = originalString;
    }

    public string Key { get; set; }
    public Nut10ProofSecret ProofSecret { get; set; }


    public byte[] GetBytes()
    {
        return _originalString != null
            ? System.Text.Encoding.UTF8.GetBytes(_originalString)
            : JsonSerializer.SerializeToUtf8Bytes(this);
    }

    public ECPubKey ToCurve()
    {
        return Cashu.HashToCurve(GetBytes());
    }
}