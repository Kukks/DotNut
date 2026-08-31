using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNut.JsonConverters;

public class BlsKeysetJsonConverter : JsonConverter<BlsKeyset>
{
    public override BlsKeyset? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected object");
        }

        var keyset = new BlsKeyset();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return keyset;
            }

            ulong amount;
            if (reader.TokenType == JsonTokenType.Number)
            {
                amount = reader.GetUInt64();
            }
            else if (reader.TokenType is JsonTokenType.String or JsonTokenType.PropertyName)
            {
                var s = reader.GetString();
                if (string.IsNullOrEmpty(s))
                {
                    throw new JsonException("Expected string key");
                }
                amount = ulong.Parse(s);
            }
            else
            {
                throw new JsonException("Expected number or string key");
            }

            reader.Read();
            var hex = reader.GetString();
            if (string.IsNullOrEmpty(hex) || hex.Length != 192)
            {
                throw new JsonException($"Expected 192-char G2 compressed hex, got length {hex?.Length}");
            }

            var g2Key = new BlsG2PubKey(hex);
            keyset[amount] = new PubKey(g2Key.Point);
        }
        throw new JsonException("Missing end object");
    }

    public override void Write(Utf8JsonWriter writer, BlsKeyset? value, JsonSerializerOptions options)
    {
        if (value is null) { writer.WriteNullValue(); return; }
        writer.WriteStartObject();
        foreach (var (amount, key) in value)
        {
            writer.WritePropertyName(amount.ToString());
            writer.WriteStringValue(key.ToString());  // 192-hex for G2 PubKey
        }
        writer.WriteEndObject();
    }
}
