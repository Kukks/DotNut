using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNut.JsonConverters;

public class UnixDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        var val =
            reader.TokenType == JsonTokenType.Number
                ? reader.GetInt64()
                : long.Parse(reader.GetString()!);

        return DateTimeOffset.FromUnixTimeSeconds(val);
    }

    public override void Write(
        Utf8JsonWriter writer,
        DateTimeOffset value,
        JsonSerializerOptions options
    )
    {
        writer.WriteNumberValue(value.ToUnixTimeSeconds());
    }
}
