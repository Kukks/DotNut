using System.Text.Json;
using System.Text.Json.Serialization;
using DotNut.ApiModels.Mint.bolt12;

namespace DotNut.ApiModels;

public class PostMintQuotesByPubkeyResponseConverter : JsonConverter<PostMintQuotesByPubkeyResponse>
{
    private const string Bolt12Discriminator = "amount_paid";

    public override PostMintQuotesByPubkeyResponse Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var bolt11 = new List<PostMintQuoteBolt11Response>();
        var bolt12 = new List<PostMintQuoteBolt12Response>();

        foreach (var element in root.GetProperty("quotes").EnumerateArray())
        {
            var raw = element.GetRawText();
            if (element.TryGetProperty(Bolt12Discriminator, out _))
                bolt12.Add(JsonSerializer.Deserialize<PostMintQuoteBolt12Response>(raw, options)!);
            else
                bolt11.Add(JsonSerializer.Deserialize<PostMintQuoteBolt11Response>(raw, options)!);
        }

        return new PostMintQuotesByPubkeyResponse
        {
            Bolt11Quotes = bolt11.ToArray(),
            Bolt12Quotes = bolt12.ToArray()
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        PostMintQuotesByPubkeyResponse value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteStartArray("quotes");

        foreach (var q in value.Bolt11Quotes)
            JsonSerializer.Serialize(writer, q, options);
        foreach (var q in value.Bolt12Quotes)
            JsonSerializer.Serialize(writer, q, options);

        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}