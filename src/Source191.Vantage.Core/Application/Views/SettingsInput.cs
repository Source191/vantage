using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>The fields a setting's dialog can show, in the order Tab visits them.</summary>
public enum SettingsField
{
    Roots,
    WorktreeRoot,
    Editor,
    Executable,
    Arguments,
    Base,
    Place,
}

/// <summary>What happened in a setting's dialog: each case is one control event or button, raised by the dialog that binds them.</summary>
public abstract record SettingsInput
{
    /// <summary>The repository roots, one per line, as typed.</summary>
    public sealed record RootsChanged(string Text) : SettingsInput;

    /// <summary>The folders chosen in the picker, in the order chosen; none when it was cancelled.</summary>
    public sealed record FoldersChosen(IReadOnlyList<string> Folders) : SettingsInput;

    public sealed record WorktreeRootChanged(string Text) : SettingsInput;

    public sealed record PresetChanged(EditorPreset Preset) : SettingsInput;

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

/// <summary>What the dialog does after a reduce: nothing, save these settings, or close unchanged.</summary>
public abstract record SettingsEffect
{
    /// <summary>Nothing: the dialog pushes the new state back to its controls.</summary>
    public sealed record None : SettingsEffect;

    public sealed record Save(Settings Settings) : SettingsEffect;

    public sealed record Close : SettingsEffect;
}

public sealed record SettingsUpdate(SettingsState State, SettingsEffect Effect);

/// <summary>What the settings dialog settles on: write config.json and use these settings from now on.</summary>
public sealed record SaveSettings(Settings Settings) : ViewEffect;