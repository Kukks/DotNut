using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNut.JsonConverters;

public class Nut10SecretJsonConverter : JsonConverter<Nut10Secret>
{
    
    
    
    public override Nut10Secret? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if(reader.TokenType == JsonTokenType.Null)
            return null;
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Expected array");
        }
        reader.Read();
        if(reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected string");
        var key = reader.GetString();
        reader.Read();
        
        Nut10ProofSecret? proofSecret;
        switch (key)
        {
            case P2PKProofSecret.Key:
                proofSecret = JsonSerializer.Deserialize<P2PKProofSecret>(ref reader, options);
                break;
            case HTLCProofSecret.Key:
                proofSecret = JsonSerializer.Deserialize<HTLCProofSecret>(ref reader, options);

                break;
            default:
                throw new JsonException("Unknown secret type");
        }
        if(proofSecret is null)
            throw new JsonException("Invalid proof secret");
        reader.Read();
        if (reader.TokenType != JsonTokenType.EndArray)
        {
            throw new JsonException("Expected end array");
        }

        return new Nut10Secret(key,  proofSecret);
        

    }

    public override void Write(Utf8JsonWriter writer, Nut10Secret? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }
        
        writer.WriteStartArray();
        JsonSerializer.Serialize(writer, value.Key, options);
        JsonSerializer.Serialize(writer, value.ProofSecret, options);
        writer.WriteEndArray();
    }
}