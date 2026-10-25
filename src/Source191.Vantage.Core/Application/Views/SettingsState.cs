using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>
/// Everything the settings dialog shows: the fields as typed, the problem that stopped the last save (null when there
/// is none), the focused field, and the folder check the roots are validated with.
/// </summary>
public sealed record SettingsState(SettingsText Text, SettingsProblem? Problem, SettingsField Focus, Func<string, bool> FolderExists)
{
    /// <summary>The dialog as it opens, showing the settings saved now, with focus in the roots.</summary>
    public static SettingsState Open(Settings settings, Func<string, bool> folderExists)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return new SettingsState(settings.ToText(), Problem: null, SettingsField.Roots, folderExists);
    }

    /// <summary>The executable and arguments fields show only for a custom editor.</summary>
    public bool ShowsCustomEditor => Text.Preset == EditorPreset.Custom;
}
