using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>What happened in a setting's dialog: each case is one control event or button, raised by the dialog that binds them.</summary>
public abstract record SettingsInput
{
    /// <summary>The repository roots, one per line, as typed.</summary>
    public sealed record RootsChanged(string Text) : SettingsInput;

    /// <summary>The folders chosen in the picker, in the order chosen; none when it was cancelled.</summary>
    public sealed record FoldersChosen(IReadOnlyList<string> Folders) : SettingsInput;

    /// <summary>The file chosen in the picker for a custom editor's executable; empty when it was cancelled.</summary>
    public sealed record FileChosen(string Path) : SettingsInput;

    public sealed record WorktreeRootChanged(string Text) : SettingsInput;

    public sealed record PresetChanged(EditorPreset Preset) : SettingsInput;

    /// <summary>The dialog's look for the presets has answered: these could not be found.</summary>
    public sealed record PresetsLocated(IReadOnlySet<EditorPreset> Missing) : SettingsInput;

    public sealed record ExecutableChanged(string Text) : SettingsInput;

    public sealed record ArgumentsChanged(string Text) : SettingsInput;

    public sealed record BaseChanged(WorktreeBase Base) : SettingsInput;

    public sealed record PlaceChanged(LaunchMode Place) : SettingsInput;

    /// <summary>A field took focus, by Tab, a click or the dialog itself.</summary>
    public sealed record FieldFocused(SettingsField Field) : SettingsInput;

    /// <summary>F5 or the Save button.</summary>
    public sealed record Save : SettingsInput;

    /// <summary>Esc or the Cancel button.</summary>
    public sealed record Cancel : SettingsInput;
}