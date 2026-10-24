using Source191.Vantage.Domain;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Source191.Vantage.Adapters.State;

/// <summary>Writes a <see cref="BranchName"/> as the plain string created.json has always held.</summary>
public sealed class BranchNameJsonConverter : JsonConverter<BranchName>
{
    public override BranchName Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        BranchName.TryParse(reader.GetString()) ?? throw new JsonException("A state file holds a branch name that is blank or holds whitespace.");

    public override void Write(Utf8JsonWriter writer, BranchName value, JsonSerializerOptions options) => writer.WriteStringValue(value.Value);
}

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
        string text when SessionId.TryParse(text) is SessionId id => new TabOwner.ForSession(id),
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