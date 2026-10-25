using Source191.Vantage.Domain;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Source191.Vantage.Adapters.State;

/// <summary>
/// Writes a tab's <see cref="TabOwner"/> as the string tab files have always held: the session id, or the word
/// "vantage" for Vantage's own tab. No session id can be that word, since the CLI's ids are GUIDs.
/// </summary>
public sealed class TabOwnerJsonConverter : JsonConverter<TabOwner>
{
    private const string Vantage = "vantage";

    public override TabOwner Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => reader.GetString() switch
    {
        Vantage => TabOwner.Vantage,
        string text when SessionId.TryParse(text, out SessionId? id) => new TabOwner.ForSession(id),
        _ => throw new JsonException("A tab file has no owner."),
    };

    public override void Write(Utf8JsonWriter writer, TabOwner value, JsonSerializerOptions options) => writer.WriteStringValue(value switch
    {
        TabOwner.ForSession session => session.Id.Value,
        _ => Vantage,
    });
}

/// <summary>Keeps the JSON names state files were written with when a field was renamed in code.</summary>
public static class StateFileNames
{
    /// <summary>A tab registration's owner was its session id; files keep the "sessionId" key.</summary>
    public static void KeepTabOwnerAsSessionId(JsonTypeInfo info)
    {
        if (info.Type != typeof(TabRegistration))
        {
            return;
        }

        foreach (JsonPropertyInfo property in info.Properties.Where(property => property.Name == "owner"))
        {
            property.Name = "sessionId";
        }
    }
}