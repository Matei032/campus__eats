using System.Text.Json;
using System.Text.Json.Serialization;

namespace CampusEats.Frontend.Models.Converters;

// Converter care acceptă atât string, cât și array și mapează în List<string>
public class StringOrArrayConverter : JsonConverter<List<string>?>
{
    public override List<string>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString();
            if (string.IsNullOrWhiteSpace(s)) return new List<string>();
            return new List<string> { s };
        }

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var list = new List<string>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                    break;

                if (reader.TokenType == JsonTokenType.String)
                {
                    var val = reader.GetString();
                    if (!string.IsNullOrWhiteSpace(val))
                        list.Add(val);
                    continue;
                }

                // Ignoră elemente non-string
                reader.Skip();
            }
            return list;
        }

        // Tip neprevăzut -> ignorăm și returnăm listă goală
        reader.Skip();
        return new List<string>();
    }

    public override void Write(Utf8JsonWriter writer, List<string>? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartArray();
        foreach (var s in value)
        {
            if (!string.IsNullOrWhiteSpace(s))
                writer.WriteStringValue(s);
        }
        writer.WriteEndArray();
    }
}