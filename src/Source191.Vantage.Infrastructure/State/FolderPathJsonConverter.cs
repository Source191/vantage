using Source191.Vantage.Domain;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Source191.Vantage.Adapters.State;

/// <summary>
/// Writes a <see cref="FolderPath"/> as the plain path string the state files have always held, and reads it back. A
/// value that is not a full path makes the file unreadable, which the store treats as it treats any corrupt file.
/// </summary>
public sealed class FolderPathJsonConverter : JsonConverter<FolderPath>
{
    public override FolderPath Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return FolderPath.TryParse(reader.GetString()) ?? throw new JsonException("A state file holds a path that is not a full path.");
    }

    public override void Write(Utf8JsonWriter writer, FolderPath value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}