using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>
/// What the Open folder with dialog knows of the presets on this machine: nothing yet as it opens, then, once its look
/// off the UI thread has answered, which presets it could not find. Explorer and Custom are never looked for.
/// </summary>
public abstract record PresetSearch
{
    public sealed record NotYetLooked : PresetSearch;

    public sealed record Looked(IReadOnlySet<EditorPreset> Missing) : PresetSearch;
}