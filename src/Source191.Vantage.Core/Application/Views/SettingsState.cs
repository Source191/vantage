using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>
/// Everything a setting's dialog shows: the saved settings as text with the value being edited as typed, the problem
/// that stopped the last save (null when there is none), the focused field, the folder check the roots are validated
/// with, and which setting the dialog edits.
/// </summary>
public sealed record SettingsState(SettingsText Text, SettingsProblem? Problem, SettingsField Focus, Func<string, bool> FolderExists, SettingsScope Scope)
{
    /// <summary>The dialog as it opens on one setting, showing it as saved.</summary>
    public static SettingsState Open(Settings settings, SettingsScope scope, Func<string, bool> folderExists)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(scope);

        return new SettingsState(settings.ToText(), Problem: null, scope.Field, folderExists, scope);
    }

    /// <summary>Where the folder picker opens: the folder holding the first root typed that is a full path (the root itself when it has no parent), or nothing when there is none.</summary>
    public FolderPath? BrowseFrom => Text.Roots.Split('\n').Select(line => line.Trim()).Parsed<FolderPath>().FirstOrDefault() is FolderPath first ? first.Parent ?? first : null;

    /// <summary>The executable and arguments fields show only for a custom editor.</summary>
    public bool ShowsCustomEditor => Text.Preset == EditorPreset.Custom;
}