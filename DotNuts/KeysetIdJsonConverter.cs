using System.Text.Json;
using System.Text.Json.Serialization;


public class KeysetIdJsonConverter : JsonConverter<KeysetId>
{
    public override KeysetId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }
        else if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Expected string");
        }

        return new KeysetId(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, KeysetId value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.ToString());
    }
}