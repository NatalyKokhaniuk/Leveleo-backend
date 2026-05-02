using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LeveLEO.Infrastructure.Delivery.Json;

/// <summary>НП інколи віддає широту/довготу рядком з багатьма знаками після коми.</summary>
public sealed class NovaPoshtaDoubleConverter : JsonConverter<double>
{
    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => reader.GetDouble(),
            JsonTokenType.String => double.TryParse(
                reader.GetString(),
                NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture,
                out var d)
                ? d
                : 0,
            _ => 0
        };
    }

    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(value);
}
