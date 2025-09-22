using System.Text.Json.Serialization;
using DotNut.JsonConverters;
using NBitcoin.Secp256k1;

namespace DotNut;

[JsonConverter(typeof(SecretJsonConverter))]
public interface ISecret
{
    byte[] GetBytes();
    ECPubKey ToCurve();
}