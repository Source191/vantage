using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using System.Globalization;

namespace Source191.Vantage.Application;

/// <summary>The Config tab's words: each row's name and value, and what to do about a piece of setup.</summary>
public static partial class Wording
{
    private const string SetupTerminal = "run vantage setup-terminal";

    /// <summary>The repository roots dialog's button that opens the folder picker.</summary>
    public static string BrowseButton => "_Browse… (F3)";

    /// <summary>The folder picker's own title, inside the dialog; Vantage's tab says <see cref="DialogName.FolderPicker"/>.</summary>
    public static string FolderPickerTitle(SettingsScope scope) => scope is SettingsScope.WorktreeRoot ? "Choose the worktree root" : "Add repository roots";

    /// <summary>The caption above the Default place dialog's choice.</summary>
    public static string PlaceCaption => "Where the wizard opened with n starts a session (↑↓ then Space)";

    /// <summary>The SETTING cell.</summary>
    public static string ConfigSetting(ConfigRow row)
    {
        return row switch
        {
            ConfigRow.RepositoryRoot { Index: 0 } => "Repository roots",
            ConfigRow.RepositoryRoot => string.Empty,
            ConfigRow.WorktreeRoot => "Worktree root",
            ConfigRow.OpenFolderWith => "Open folder with",
            ConfigRow.DefaultBase => "Default base",
            ConfigRow.DefaultPlace => "Default place",
            ConfigRow.CheckForUpdates => "Check for updates",
            ConfigRow.Setup setup => ConfigSetting(setup.Item),
            _ => row.ToString(),
        };
    }

    /// <summary>What a Config row's setting does, or what its setup is for: the status line for the selected row and the row's tooltip.</summary>
    public static string ConfigDescription(ConfigRow row) => row switch
    {
        ConfigRow.RepositoryRoot => "A folder Vantage searches for git repositories; Enter edits every root, one per line.",
        ConfigRow.WorktreeRoot => "Where new worktrees go, each in a folder named <repository>-<slug> on a branch named <slug>.",
        ConfigRow.OpenFolderWith => "The program e opens a row's folder with: one of the editors Vantage knows, or a program of your own.",
        ConfigRow.DefaultBase => "What a new worktree's branch starts from: origin's default branch, or the commit checked out now.",
        ConfigRow.DefaultPlace => "Where the wizard opened with n starts a session: a new worktree, or the repository's current branch.",
        ConfigRow.CheckForUpdates => "Whether Vantage asks nuget.org once a day for a newer release, which U then installs.",
        ConfigRow.Setup setup => SetupDescription(setup.Item),
        _ => string.Empty,
    };

    /// <summary>The VALUE cell; <paramref name="now"/> says whether the last update check was today.</summary>
    public static string ConfigValue(ConfigRow row, DateTimeOffset now)
    {
        return row switch
        {
            ConfigRow.RepositoryRoot root => root.Path.Value,
            ConfigRow.WorktreeRoot worktreeRoot => worktreeRoot.Path.Value,
            ConfigRow.OpenFolderWith open => open.Editor.Preset == EditorPreset.Custom ? $"{open.Editor.Executable} {CommandArguments.Join(open.Editor.Arguments)}" : Describe(open.Editor.Preset),
            ConfigRow.DefaultBase { Base: WorktreeBase.Head } => "current HEAD",
            ConfigRow.DefaultBase => "origin's default branch",
            ConfigRow.DefaultPlace place => Describe(place.Place),
            ConfigRow.CheckForUpdates check => check.On ? "on" : "off",
            ConfigRow.Setup setup => SetupValue(setup.Item, setup.State, now),
            _ => row.ToString(),
        };
    }

    /// <summary>The title of a setting's dialog.</summary>
    public static string SettingsDialogTitle(SettingsScope scope)
    {
        return scope switch
        {
            SettingsScope.Roots => "Repository roots",
            SettingsScope.WorktreeRoot => "Worktree root",
            SettingsScope.Editor => "Open folder with",
            SettingsScope.Base => "Worktree base",
            SettingsScope.Place => "Default place",
            _ => scope.ToString(),
        };
    }

    /// <summary>A place a new session starts, as the Default place row and its dialog name it.</summary>
    public static string Describe(LaunchMode place)
    {
        return place switch
        {
            LaunchMode.CurrentBranch => "Current branch",
            LaunchMode.NewWorktree => "New worktree",
        };
    }

    /// <summary>Enter on a setup row: how that setup is changed, since the tab only reads it.</summary>
    public static string Describe(SetupItem item)
    {
        return item switch
        {
            SetupItem.SettingsFile => "This tab writes the settings file: edit a setting above to change it.",
            SetupItem.TerminalProfiles => "Run vantage setup-terminal, then restart Windows Terminal, to add the Vantage profiles.",
            SetupItem.ClaudeBell => "Run vantage setup-terminal to make Claude ring the terminal bell when a session needs you.",
            SetupItem.DiagnosticLog => "Set the VANTAGE_LOG environment variable to a file path before starting Vantage to keep a diagnostic log.",
            SetupItem.LastUpdateCheck => "Vantage asks nuget.org at most once a day; turn the check on or off with Check for updates.",
        };
    }

    private static string SetupDescription(SetupItem item)
    {
        return item switch
        {
            SetupItem.SettingsFile => "The file Vantage keeps the settings above in; e opens the folder holding it.",
            SetupItem.TerminalProfiles => "Windows Terminal profiles that give Vantage tabs a bell and the Vantage icon.",
            SetupItem.ClaudeBell => "Claude's terminal bell, which rings when a session finishes or needs permission while you are away.",
            SetupItem.DiagnosticLog => "The file VANTAGE_LOG names, where Vantage writes its diagnostic log as it runs.",
            SetupItem.LastUpdateCheck => "When Vantage last asked nuget.org for a newer release, and the newest it found.",
        };
    }

    private static string ConfigSetting(SetupItem item)
    {
        return item switch
        {
            SetupItem.SettingsFile => "Settings file",
            SetupItem.TerminalProfiles => "Windows Terminal profiles",
            SetupItem.ClaudeBell => "Claude bell",
            SetupItem.DiagnosticLog => "Diagnostic log",
            SetupItem.LastUpdateCheck => "Last update check",
        };
    }

    private static string SetupValue(SetupItem item, SetupState state, DateTimeOffset now)
    {
        return (item, state) switch
        {
            (_, SetupState.Reading) => "reading…",
            (_, SetupState.Unreadable) => "could not be read",
            (SetupItem.TerminalProfiles, SetupState.At at) => $"installed, {at.Path}",
            (SetupItem.TerminalProfiles, SetupState.NotSetUp) => $"not set up, {SetupTerminal}",
            (_, SetupState.NotSetUp) => $"not set, {SetupTerminal}",
            (_, SetupState.At at) => at.Path,
            (_, SetupState.Enabled) => "on",
            (_, SetupState.OtherChannel other) => $"{other.Channel}, not the terminal bell",
            (_, SetupState.Disabled) => "off",
            (_, SetupState.Never) => "never",
            (_, SetupState.Checked check) => $"{When(check.When, now)}{(check.Newest is null ? string.Empty : $", newest {check.Newest}")}",
            _ => state.ToString(),
        };
    }

    private static string When(DateTimeOffset when, DateTimeOffset now)
    {
        DateTimeOffset at = when.ToOffset(now.Offset);

        return at.Date == now.Date
            ? at.ToString("HH:mm 'today'", CultureInfo.InvariantCulture)
            : at.ToString("d MMMM yyyy HH:mm", CultureInfo.InvariantCulture);
    }
}