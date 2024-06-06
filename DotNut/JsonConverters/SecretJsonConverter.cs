using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNut.JsonConverters;

public class SecretJsonConverter : JsonConverter<ISecret>
{
    public override ISecret? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.StartArray && reader.CurrentDepth == 0)
        {
            //we are converting a nut10 secret directly 
            return JsonSerializer.Deserialize<Nut10Secret>(ref reader, options);
        }
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Expected string");
        }

        var str = reader.GetString();
        if (string.IsNullOrEmpty(str))
        {
            throw new JsonException("Secret was not nut10 or a (not empty) string");
        }
        try
        {
            return JsonSerializer.Deserialize<Nut10Secret>(str);
        }
        catch (Exception e)
        {
           
            return new StringSecret(str);
        }
    }

    public override void Write(Utf8JsonWriter writer, ISecret? value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                return;
            case Nut10Secret nut10Secret:
                writer.WriteStringValue(JsonSerializer.Serialize(nut10Secret));
                return;
            case StringSecret stringSecret:
                writer.WriteStringValue(stringSecret.Secret);
                break;
        }
    }
}