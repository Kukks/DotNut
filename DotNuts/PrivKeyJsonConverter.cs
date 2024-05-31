using System.Text.Json;
using System.Text.Json.Serialization;

public class PrivKeyJsonConverter : JsonConverter<PrivKey>
{
    public override PrivKey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Expected string");
        }

        return new PrivKey(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, PrivKey value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.ToString());
    }
}