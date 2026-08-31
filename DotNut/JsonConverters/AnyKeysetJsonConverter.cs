using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNut.JsonConverters;

/// <summary>
/// Reads a keyset JSON object as either <see cref="Keyset"/> (secp256k1, 66-char hex values)
/// or <see cref="BlsKeyset"/> (BLS12-381 G2, 192-char hex values).
/// Registered as the [JsonConverter] on <see cref="Keyset"/> so any field typed as
/// <see cref="Keyset"/> gets polymorphic dispatch automatically.
/// </summary>
public class AnyKeysetJsonConverter : JsonConverter<Keyset>
{
    public override Keyset? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected object for keyset");

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        bool isBls = false;
        foreach (var prop in root.EnumerateObject())
        {
            var val = prop.Value.GetString();
            if (val is null) continue;
            if (val.Length == 192) { isBls = true; break; }
            if (val.Length == 66 || val.Length == 96) break;
        }

        var jsonBytes = System.Text.Encoding.UTF8.GetBytes(root.GetRawText());

        if (isBls)
        {
            // BlsKeyset has [JsonConverter(typeof(BlsKeysetJsonConverter))] — no recursion.
            return JsonSerializer.Deserialize<BlsKeyset>(jsonBytes, options);
        }

        // Call KeysetJsonConverter directly to avoid re-entering this converter
        // (Keyset itself is now annotated with AnyKeysetJsonConverter).
        var innerReader = new Utf8JsonReader(jsonBytes);
        innerReader.Read();
        return new KeysetJsonConverter().Read(ref innerReader, typeof(Keyset), options);
    }

    public override void Write(Utf8JsonWriter writer, Keyset? value, JsonSerializerOptions options)
    {
        if (value is BlsKeyset blsKeyset)
            JsonSerializer.Serialize(writer, blsKeyset, options);
        else if (value is not null)
            new KeysetJsonConverter().Write(writer, value, options);
        else
            writer.WriteNullValue();
    }
}
