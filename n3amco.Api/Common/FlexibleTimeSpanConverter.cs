using System;
using System.Text.Json;
using System.Text.Json.Serialization;

public class FlexibleTimeSpanConverter : JsonConverter<TimeSpan>
{
    public override TimeSpan Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("TimeSpan value must be a string.");

        var value = reader.GetString();

        if (TimeSpan.TryParse(value, out var time))
            return time;

        throw new JsonException(
            $"Invalid time format '{value}'. Expected HH:mm or HH:mm:ss");
    }

    public override void Write(
        Utf8JsonWriter writer,
        TimeSpan value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(@"hh\:mm"));
    }
}
