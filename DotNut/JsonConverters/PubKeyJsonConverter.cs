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

        // Accept both secp256k1 compressed (66 chars) and BLS G1 compressed (96 chars)
        if (str.Length != 66 && str.Length != 96)
            throw new JsonException($"Expected 66-char secp or 96-char BLS G1 hex, got length {str.Length}");
        return new PubKey(str);
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
