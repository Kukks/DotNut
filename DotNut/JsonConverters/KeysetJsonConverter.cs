using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNut.JsonConverters;

public class KeysetJsonConverter : JsonConverter<Keyset>
{
    public override Keyset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }
        else if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected object");
        }

        var keyset = new Keyset();
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
            else if (reader.TokenType is  JsonTokenType.String or JsonTokenType.PropertyName)
            {
                var str = reader.GetString();
                if (string.IsNullOrEmpty(str))
                    throw new JsonException("Expected string");
                amount = ulong.Parse(str);
            }
            else
            {
                throw new JsonException("Expected number or string");
            }


            reader.Read();
            var pubkey = JsonSerializer.Deserialize<PubKey>(ref reader, options);
            if(pubkey is null || pubkey.Key.ToBytes().Length != 33)
                throw new JsonException("Invalid public key (not compressed?)");
            keyset.Add(amount, pubkey);
        }

        throw new JsonException("Missing end object");
    }

    public override void Write(Utf8JsonWriter writer, Keyset? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();
        foreach (var pair in value)
        {
            writer.WritePropertyName(pair.Key.ToString());
            JsonSerializer.Serialize(writer, pair.Value, options);
        }

        writer.WriteEndObject();
    }
}