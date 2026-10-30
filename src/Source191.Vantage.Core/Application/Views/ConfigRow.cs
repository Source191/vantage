using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>
/// A row of the Config tab: a setting Vantage keeps in config.json, or a piece of setup it reads but does not edit.
/// Each case carries the facts its words are made from; <c>Wording</c> names the row and says its value.
/// </summary>
/// <param name="Key">The selection key, stable across a save so the row just edited stays selected.</param>
public abstract record ConfigRow(string Key)
{
    public sealed record RepositoryRoot(int Index, FolderPath Path) : ConfigRow($"root:{Index}");

    public sealed record WorktreeRoot(FolderPath Path) : ConfigRow("worktree-root");

    public sealed record OpenFolderWith(EditorChoice Editor) : ConfigRow("open-folder-with");

    public sealed record DefaultBase(WorktreeBase Base) : ConfigRow("default-base");

    public sealed record CheckForUpdates(bool On) : ConfigRow("check-for-updates");

    /// <summary>A piece of setup: what it is and what was read about it.</summary>
    public sealed record Setup(SetupItem Item, SetupState State) : ConfigRow($"setup:{Item}");
}

/// <summary>The setup the Config tab reads, in the order its rows read.</summary>
public enum SetupItem
{
    SettingsFile,
    TerminalProfiles,
    ClaudeBell,
    DiagnosticLog,
    LastUpdateCheck,
}

/// <summary>What was read about a piece of setup.</summary>
public abstract record SetupState
{
    /// <summary>Not read yet: the setup is read off the UI thread when the tab is shown.</summary>
    public sealed record Reading : SetupState;

    public sealed record Unreadable : SetupState;

    /// <summary>In place at this path: the settings file, the profiles' fragment, the diagnostic log.</summary>
    public sealed record At(string Path) : SetupState;

    /// <summary>Not set up: Vantage's advice is to run <c>vantage setup-terminal</c>.</summary>
    public sealed record NotSetUp : SetupState;

    /// <summary>Claude's bell is on: its notification channel is the terminal bell.</summary>
    public sealed record Enabled : SetupState;

    /// <summary>Claude notifies through another channel, named as its settings name it.</summary>
    public sealed record OtherChannel(string Channel) : SetupState;

    /// <summary>Switched off: no diagnostic log, or no update check.</summary>
    public sealed record Disabled : SetupState;

    public sealed record Never : SetupState;

    public sealed record Checked(DateTimeOffset When, string? Newest) : SetupState;
}

/// <summary>Everything the setup rows show, read together; see <see cref="SetupFactsReader"/>.</summary>
public sealed record SetupFacts(SetupState SettingsFile, SetupState TerminalProfiles, SetupState ClaudeBell, SetupState DiagnosticLog, SetupState LastUpdateCheck);