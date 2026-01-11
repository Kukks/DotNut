using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNut.JsonConverters;

public class PubKeyJsonConverter : JsonConverter<PubKey>
{
    public override PubKey? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (
            reader.TokenType != JsonTokenType.String
            || reader.GetString() is not { } str
            || string.IsNullOrEmpty(str)
        )
        {
            throw new JsonException("Expected string");
        }

        return new PubKey(str, true);
    }

    public override void Write(Utf8JsonWriter writer, PubKey? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.ToString());
    }
}
