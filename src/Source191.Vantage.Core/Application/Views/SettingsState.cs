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

    /// <summary>Whether the dialog's Browse opens the folder picker: for several repository roots, for the one worktree root, or not at all.</summary>
    public SettingsBrowse Browses => Scope switch
    {
        SettingsScope.Roots => SettingsBrowse.SeveralFolders,
        SettingsScope.WorktreeRoot => SettingsBrowse.OneFolder,
        _ => SettingsBrowse.No,
    };

    /// <summary>Where the folder picker opens: the folder holding the first root typed that is a full path, or the worktree root typed (the folder itself when it has no parent); nothing when there is none.</summary>
    public FolderPath? BrowseFrom => FolderTyped() is FolderPath typed ? typed.Parent ?? typed : null;

    /// <summary>The executable and arguments fields show only for a custom editor.</summary>
    public bool ShowsCustomEditor => Text.Preset == EditorPreset.Custom;

    private FolderPath? FolderTyped() => Browses switch
    {
        SettingsBrowse.SeveralFolders => Text.Roots.Split('\n').Select(line => line.Trim()).Parsed<FolderPath>().FirstOrDefault(),
        SettingsBrowse.OneFolder => FolderPath.TryParse(Text.WorktreeRoot.Trim(), out FolderPath? root) ? root : null,
        SettingsBrowse.No => null,
    };
}

/// <summary>What a setting's dialog can browse for: nothing, the one worktree root, or several repository roots.</summary>
public enum SettingsBrowse
{
    No,
    OneFolder,
    SeveralFolders,
}