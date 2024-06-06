using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNut.JsonConverters;

public class PrivKeyJsonConverter : JsonConverter<PrivKey>
{
    public override PrivKey? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.String ||
            reader.GetString() is not { } str ||
            string.IsNullOrEmpty(str))
        {
            throw new JsonException("Expected string");
        }

        return new PrivKey(str);
    }

    public override void Write(Utf8JsonWriter writer, PrivKey? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.ToString());
    }
}