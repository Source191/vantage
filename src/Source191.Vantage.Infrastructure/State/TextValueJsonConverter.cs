using Source191.Vantage.Domain;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Source191.Vantage.Adapters.State;

/// <summary>
/// Writes a text value (a <see cref="FolderPath"/>, a <see cref="BranchName"/>) as the plain string the state files have
/// always held, and reads it back through its <c>TryParse</c>. A string the type refuses makes the file unreadable,
/// which the store treats as it treats any corrupt file.
/// </summary>
public sealed class TextValueJsonConverter<T> : JsonConverter<T>
    where T : class, ITextValue<T>
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        T.TryParse(reader.GetString(), out T? value) ? value : throw new JsonException($"A state file holds a {typeof(T).Name} it cannot read.");

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) => writer.WriteStringValue(value.Value);
}