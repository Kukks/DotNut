using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNut.JsonConverters;

/// <summary>
/// For arrays/lists of objects carrying an "id" keyset id: drops elements whose id isn't a keyset id
/// format we understand (e.g. a newer keyset version), so one such entry doesn't fail the whole payload.
/// </summary>
public class SkipUnknownKeysetsJsonConverter : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsArray
        || (typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(List<>));

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeToConvert.IsArray
            ? typeof(ArrayConverter<>).MakeGenericType(typeToConvert.GetElementType()!)
            : typeof(ListConverter<>).MakeGenericType(typeToConvert.GetGenericArguments()[0]);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    private static List<T>? ReadKnown<T>(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        using var doc = JsonDocument.ParseValue(ref reader);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("Expected array");
        }

        var items = new List<T>();
        foreach (var element in doc.RootElement.EnumerateArray())
        {
            if (
                element.ValueKind == JsonValueKind.Object
                && element.TryGetProperty("id", out var id)
                && id.ValueKind == JsonValueKind.String
                && !KeysetId.TryParse(id.GetString(), out _)
            )
            {
                continue;
            }
            items.Add(element.Deserialize<T>(options)!);
        }
        return items;
    }

    private static void WriteItems<T>(Utf8JsonWriter writer, IEnumerable<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var item in value)
        {
            JsonSerializer.Serialize(writer, item, options);
        }
        writer.WriteEndArray();
    }

    private class ArrayConverter<T> : JsonConverter<T[]>
    {
        public override T[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            ReadKnown<T>(ref reader, options)?.ToArray();

        public override void Write(Utf8JsonWriter writer, T[] value, JsonSerializerOptions options) =>
            WriteItems(writer, value, options);
    }

    private class ListConverter<T> : JsonConverter<List<T>>
    {
        public override List<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            ReadKnown<T>(ref reader, options);

        public override void Write(Utf8JsonWriter writer, List<T> value, JsonSerializerOptions options) =>
            WriteItems(writer, value, options);
    }
}
