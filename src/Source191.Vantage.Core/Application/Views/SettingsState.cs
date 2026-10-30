using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>
/// Everything a setting's dialog shows: the saved settings as text with the one value being edited as typed, the
/// problem that stopped the last save (null when there is none), the focused field, the folder check the roots are
/// validated with, and which setting the dialog edits.
/// </summary>
public sealed record SettingsState(SettingsText Text, SettingsProblem? Problem, SettingsField Focus, Func<string, bool> FolderExists, SettingsScope Scope)
{
    /// <summary>The dialog as it opens on one setting, showing it as saved; a new root starts empty after the others.</summary>
    public static SettingsState Open(Settings settings, SettingsScope scope, Func<string, bool> folderExists)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(scope);
        SettingsText text = settings.ToText();
        SettingsText opened = scope is SettingsScope.NewRoot ? text with { Roots = $"{text.Roots}\n" } : text;

        return new SettingsState(opened, Problem: null, scope.Field, folderExists, scope);
    }

    /// <summary>The executable and arguments fields show only for a custom editor.</summary>
    public bool ShowsCustomEditor => Text.Preset == EditorPreset.Custom;

    /// <summary>Whether the dialog edits a repository root, an existing one or a new one.</summary>
    public bool EditsARoot => Scope is SettingsScope.Root or SettingsScope.NewRoot;

    /// <summary>The repository root being edited, as typed; empty when the dialog edits something else.</summary>
    public string RootText => EditsARoot ? RootLines[RootIndex] : string.Empty;

    private string[] RootLines => Text.Roots.Split('\n');

    /// <summary>A new root is the line after the saved ones, which <see cref="Open"/> added.</summary>
    private int RootIndex => Scope is SettingsScope.Root root ? root.Index : RootLines.Length - 1;

    /// <summary>This state with the root being edited replaced by <paramref name="typed"/>, every other root kept.</summary>
    public SettingsState WithRoot(string typed)
    {
        string[] lines = RootLines;
        lines[RootIndex] = typed;

        return this with { Text = Text with { Roots = string.Join('\n', lines) } };
    }
}