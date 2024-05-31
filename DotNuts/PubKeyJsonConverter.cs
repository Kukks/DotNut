using System.Text.Json;
using System.Text.Json.Serialization;

public class PubKeyJsonConverter : JsonConverter<PubKey>
{
    public override PubKey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Expected string");
        }

        return new PubKey(reader.GetString(), true);
    }

    public override void Write(Utf8JsonWriter writer, PubKey value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        } 

        writer.WriteStringValue(value.ToString());
    }
}