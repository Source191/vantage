using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>
/// Everything a setting's dialog shows: the saved settings as text with the value being edited as typed, the problem
/// line (the problem that stopped the last save, or none), the focused field, the folder check the roots and the file
/// check a custom executable are validated with, which setting the dialog edits, and what the Open folder with dialog has
/// found of the presets.
/// </summary>
public sealed record SettingsState(SettingsText Text, SettingsProblemLine Problem, SettingsField Focus, Func<string, bool> FolderExists, Func<string, bool> FileExists, SettingsScope Scope, PresetSearch Presets)
{
    /// <summary>The dialog as it opens on one setting, showing it as saved.</summary>
    public static SettingsState Open(Settings settings, SettingsScope scope, Func<string, bool> folderExists, Func<string, bool> fileExists)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(scope);

        return new SettingsState(settings.ToText(), new SettingsProblemLine.None(), scope.Field, folderExists, fileExists, scope, new PresetSearch.NotYetLooked());
    }

    /// <summary>Whether the dialog's Browse opens a picker: the folder picker for several repository roots or the one worktree root, the file picker while a custom editor is chosen, or none.</summary>
    public SettingsBrowse Browses => Scope switch
    {
        SettingsScope.Roots => SettingsBrowse.SeveralFolders,
        SettingsScope.WorktreeRoot => SettingsBrowse.OneFolder,
        SettingsScope.Editor when ShowsCustomEditor => SettingsBrowse.OneFile,
        _ => SettingsBrowse.No,
    };

    /// <summary>
    /// Where the picker opens: the folder holding the first root typed that is a full path, or the worktree root typed (the
    /// folder itself when it has no parent), or the folder holding a custom executable typed as a full path; nothing when there is none.
    /// </summary>
    public BrowseStart BrowseFrom => StartFolder() is FolderPath start ? new BrowseStart.At(start) : new BrowseStart.NoFolderTyped();

    /// <summary>The executable and arguments fields show only for a custom editor.</summary>
    public bool ShowsCustomEditor => Text.Preset == EditorPreset.Custom;

    private FolderPath? StartFolder() => Browses switch
    {
        SettingsBrowse.SeveralFolders => Text.Roots.Split('\n').Select(line => line.Trim()).Parsed<FolderPath>().FirstOrDefault() is FolderPath root ? root.Parent ?? root : null,
        SettingsBrowse.OneFolder => FolderPath.TryParse(Text.WorktreeRoot.Trim(), out FolderPath? root) ? root.Parent ?? root : null,
        SettingsBrowse.OneFile => Path.IsPathFullyQualified(Text.Executable.Trim()) && FolderPath.TryParse(Path.GetDirectoryName(Text.Executable.Trim()), out FolderPath? holding) ? holding : null,
        SettingsBrowse.No => null,
    };
}
