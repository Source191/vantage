using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using System.Globalization;

namespace Source191.Vantage.Application;

/// <summary>The Config tab's words: each row's name and value, and what to do about a piece of setup.</summary>
public static partial class Wording
{
    private const string SetupTerminal = "run vantage setup-terminal";

    /// <summary>The SETTING cell.</summary>
    public static string ConfigSetting(ConfigRow row)
    {
        return row switch
        {
            ConfigRow.RepositoryRoot => "Repository root",
            ConfigRow.WorktreeRoot => "Worktree root",
            ConfigRow.OpenFolderWith => "Open folder with",
            ConfigRow.DefaultBase => "Default base",
            ConfigRow.CheckForUpdates => "Check for updates",
            ConfigRow.Setup setup => ConfigSetting(setup.Item),
            _ => row.ToString(),
        };
    }

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
            SettingsScope.Root => "Repository root",
            SettingsScope.NewRoot => "Add a repository root",
            SettingsScope.WorktreeRoot => "Worktree root",
            SettingsScope.Editor => "Open folder with",
            SettingsScope.Base => "Worktree base",
            _ => scope.ToString(),
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