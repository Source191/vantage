using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using System.Diagnostics;
using System.Globalization;

namespace Source191.Vantage.Application;

/// <summary>The words around the table screens: the tab title, the header's values, the table title, the key hints and the help.</summary>
public static partial class Wording
{
    private const string UnknownVersion = "unknown";

    private const string SettingsHelpLine = "edit roots, worktree root and editor";

    private const string UpdateLine = "update Vantage in a new tab";
    private const string ReportLine = "report a problem on GitHub";

    /// <summary>The title of Vantage's own tab outside a dialog; every writer of it uses this one value. The version is in the header.</summary>
    public static string TabTitle => "Vantage";

    /// <summary>The tab's title while a dialog is open: Vantage and the dialog's short name, never the dialog's own title.</summary>
    public static string TabTitleDuring(DialogName dialog) => $"{TabTitle} ({DialogWord(dialog)})";

    private static string DialogWord(DialogName dialog) => dialog switch
    {
        DialogName.Keys => "Keys",
        DialogName.NewSession => "New session",
        DialogName.Rename => "Rename",
        DialogName.Resume => "Resume",
        DialogName.Settings => "Settings",
        DialogName.DeleteSession => "Delete session",
        DialogName.DeleteBranch => "Delete branch",
        DialogName.CloseEmpty => "Close empty",
        DialogName.ForceRemove => "Force remove",
        DialogName.OpenSolution => "Open solution",
        DialogName.Update => "Update",
        DialogName.Memory => "Memory",
        DialogName.ProblemReport => ProblemReportDialogTitle,
    };

    /// <summary>An amount of memory as the table and the breakdown write it: whole megabytes below 1,000 MB, gigabytes to one decimal from there.</summary>
    public static string Memory(MemorySize size)
    {
        double megabytes = (double)size.Bytes / MemorySize.BytesPerMegabyte;
        long whole = (long)Math.Round(megabytes);

        return whole < 1000 ? $"{whole} MB" : $"{(megabytes / 1024).ToString("F1", CultureInfo.InvariantCulture)} GB";
    }

    /// <summary>The MEM cell: the figure, or a dash for a session with no process to measure.</summary>
    public static string MemoryCell(MemorySize? size) => size is MemorySize measured ? Memory(measured) : "-";

    /// <summary>The settings line's Claude Code value: the version as the CLI printed it, and the minimum with a prompt to update when it is older.</summary>
    private static string ClaudeVersion(ClaudeCodeVersion claude) => claude switch
    {
        ClaudeCodeVersion.Unknown => UnknownVersion,
        ClaudeCodeVersion.Reported reported => reported.Text,
        ClaudeCodeVersion.BelowMinimum old => $"{old.Installed} (below {old.Minimum}, update Claude Code)",
        _ => throw new UnreachableException(),
    };

    /// <summary>The settings line's Vantage value: the running version, its own process's memory, and the newer release with the update key when one is known.</summary>
    private static string VantageVersion(ToolVersion vantage, UpdateStatus update, MemorySize memory)
    {
        string notice = update is UpdateStatus.Available available ? $"; {available.Newest} available, U to update" : string.Empty;

        return $"{vantage.Text} ({Memory(memory)}{notice})";
    }

    /// <summary>The table's title: "7 sessions, selected: fix-123", or "2 of 7 sessions match "templates", selected: templates-sync" while a filter is set.</summary>
    public static string Describe(TableTitle title)
    {
        string selection = title.Selected is null ? string.Empty : $", selected: {title.Selected}";

        if (string.IsNullOrWhiteSpace(title.Filter))
        {
            return $"{Counted(title.Shown, title.Noun)}{selection}";
        }

        string verb = title.Total == 1 ? "matches" : "match";

        return $"{title.Shown} of {Counted(title.Total, title.Noun)} {verb} \"{title.Filter}\"{selection}";
    }

    private static string Counted(int count, string noun) => count == 1 ? $"{count} {noun}" : $"{count} {noun}s";

    public static IReadOnlyList<KeyHelp> SessionsKeys { get; } =
    [
        new("enter", "Attach", "attach (Ctrl+Z or Esc comes back)", Tip: "Attach to this session here; Ctrl+Z brings you back to Vantage."),
        new("o", "Open tab", "open in its own tab, or switch to it", Tip: "Open this session in its own tab, or switch to that tab if it is open."),
        new("n", "New session", "new session", Tip: "Start a new session, in a new worktree or on a current branch."),
        new("r", "Rename", "rename (restarts the session)", Tip: "Give the session a new name; it restarts under it, conversation kept."),
        new("x", "Stop", "stop", Tip: "Stop the session; it stays listed as stopped and can be resumed later."),
        new("d", "Delete", "delete, and its worktree if nothing is lost", Tip: "Delete the session, and its worktree too when no work would be lost."),
        new("R", "Resume", "resume after a reboot", Tip: "Resume the stopped sessions you choose, for example after a reboot."),
        new("C", "Close empty", "close tabs of unused terminal sessions", Tip: "Close the tabs of terminal sessions that were never sent a message."),
        new("b", "Adopt", "move a terminal session under Vantage", Tip: "Move a plain claude session from its own tab under Vantage's supervision."),
        new("w", "Worktrees", "worktrees view", Tip: "Switch to the worktrees view: every worktree under your roots."),
        new("/", "Filter", "filter", Tip: "Type to show only the rows that match; Esc clears the filter."),
        new("?", "Help", "help", Tip: "Show every key and what each column means."),
        new("q", "Quit", "quit", Tip: "Quit Vantage; your sessions keep running in the background."),
        new(",", "Settings", SettingsHelpLine, Tip: "Edit the repository roots, the worktree root, the editor and the base."),
        new("c", "Copy path", "copy the folder path", Tip: "Copy the row's folder path to the clipboard."),
        new("e", "Open folder", "open the folder in your editor", Tip: "Open the row's folder in the editor chosen in Settings."),
        new("v", "Solution", "open its solution", Tip: "Open the row's Visual Studio solution, asking when there are several."),
        new("p", "Open PR", "open its pull request in the browser", Tip: "Open the row's pull request in your browser."),
        new("m", "Memory", "what its memory is made of", Tip: "Show what the session's memory is made of: Claude Code and each program it runs."),
        new("U", "Update", UpdateLine, Tip: "Update Vantage to the newest release, in a tab of its own."),
        new("!", "Report", ReportLine, Tip: "Report a problem: preview the diagnostics, then open a GitHub issue."),
        new("1", Hint: null, "the Sessions tab", Tip: "Show the Sessions tab: every Claude Code session on this machine."),
        new("2", Hint: null, "the Worktrees tab", Tip: "Show the Worktrees tab: every worktree under your roots."),
    ];

    public static IReadOnlyList<KeyHelp> WorktreesKeys { get; } =
    [
        new("d", "Remove", "remove (refused when work would be lost)", Tip: "Remove this worktree; refused when it holds work that would be lost."),
        new("D", "Force remove", "force remove after typing its name", Tip: "Remove this worktree whatever it holds, after you type its name."),
        new("P", "Prune", "prune stale registrations in its repository", Tip: "Prune the stale worktree registrations of this row's repository."),
        new("s", "Start session", "start a session in this worktree", Tip: "Start a new session in this worktree, on the branch checked out there."),
        new("o", "Open shell", "open a PowerShell tab there", Tip: "Open a PowerShell tab in this worktree's folder."),
        new("enter", "Go to session", "go to the session running in it", Tip: "Go to the session running in this worktree, in the sessions view."),
        new("w", "Sessions", "back to sessions", Tip: "Go back to the sessions view."),
        new("esc", "Sessions", "back to sessions (clears a filter first)", Tip: "Clear the filter, or go back to the sessions view when there is none."),
        new("/", "Filter", "filter", Tip: "Type to show only the rows that match; Esc clears the filter."),
        new("?", "Help", "help", Tip: "Show every key and what each column means."),
        new("q", "Quit", "quit", Tip: "Quit Vantage; your sessions keep running in the background."),
        new(",", "Settings", SettingsHelpLine, Tip: "Edit the repository roots, the worktree root, the editor and the base."),
        new("c", "Copy path", "copy the folder path", Tip: "Copy the row's folder path to the clipboard."),
        new("e", "Open folder", "open the folder in your editor", Tip: "Open the row's folder in the editor chosen in Settings."),
        new("v", "Solution", "open its solution", Tip: "Open the row's Visual Studio solution, asking when there are several."),
        new("p", "Open PR", "open its pull request in the browser", Tip: "Open the row's pull request in your browser."),
        new("U", "Update", UpdateLine, Tip: "Update Vantage to the newest release, in a tab of its own."),
        new("!", "Report", ReportLine, Tip: "Report a problem: preview the diagnostics, then open a GitHub issue."),
        new("1", Hint: null, "the Sessions tab", Tip: "Show the Sessions tab: every Claude Code session on this machine."),
        new("2", Hint: null, "the Worktrees tab", Tip: "Show the Worktrees tab: every worktree under your roots."),
    ];

    /// <summary>The TAB cell of a plain claude in its own terminal; a session a Vantage tab shows has its cell filled with that tab's colour instead.</summary>
    public const string TerminalSwatch = "▯";

    /// <summary>The glyph before a state's word: a closed enum, so a new kind fails the build here.</summary>
    public static string StateGlyph(DisplayStateKind kind) => kind switch
    {
        DisplayStateKind.NeedsInput => "◐",
        DisplayStateKind.Working => "●",
        DisplayStateKind.Idle => "○",
        DisplayStateKind.Completed => "✓",
        DisplayStateKind.Failed => "✗",
        DisplayStateKind.Stopped => "■",
        DisplayStateKind.Unknown => "■",
    };

    public static IReadOnlyList<ColumnHelp> SessionsColumns { get; } =
    [
        new("NAME", "the session name given at launch, or the folder name for an unnamed one"),
        new("TAB", $"a block in the tab's colour while a Vantage tab shows the session,\n{TerminalSwatch} grey for a plain claude in its own terminal"),
        new("STATE", "◐ needs input (amber), ● working (cyan), ○ idle (white),\n✓ completed (green), ✗ failed (red), ■ stopped (grey)"),
        new("WHERE", "repository › slug for a worktree, with the branch in brackets when it differs;\nrepository (branch) for a main checkout; the folder alone outside a repository"),
        new("PR", "its pull request: #123 on GitHub (open, draft, merged, closed),\n!123 on Azure DevOps (active, draft, completed, abandoned); blank if none or not yet known"),
        new("AGE", "time since the session started"),
        new("MEM", "private working set of the session's process and everything it started; - when it has no process"),
    ];

    public static IReadOnlyList<ColumnHelp> WorktreesColumns { get; } =
    [
        new("WORKTREE", "the owning repository › the folder's slug, or its label when not named after it"),
        new("BRANCH", "the branch checked out there"),
        new("PR", "its pull request: #123 on GitHub (open, draft, merged, closed),\n!123 on Azure DevOps (active, draft, completed, abandoned); blank if none or not yet known"),
        new("DIRTY", "changed or untracked files"),
        new("UNPUSHED", "commits on its branch that no remote has"),
        new("LAST COMMIT", "time since the last commit"),
        new("SESSION", "the session running in it, if any"),
        new("HEALTH", "ok; locked (by another tool); orphan (a folder git has forgotten);\nbroken (registration or owner gone); empty (a leftover folder);\nelsewhere (a worktree of a repository outside the roots)"),
    ];

    /// <summary>The sessions view's help: its keys and columns, then the Claude Code executable Vantage drives.</summary>
    public static string SessionsHelp(string claudeExecutable) => $"{Help(SessionsKeys, SessionsColumns)}\n\nClaude Code: {claudeExecutable}";

    public static string WorktreesHelp => Help(WorktreesKeys, WorktreesColumns);

    /// <summary>The keys two to a line, so the dialog stays short enough for a small terminal.</summary>
    private static string KeyColumns(IReadOnlyList<KeyHelp> keys)
    {
        int width = keys.Where((_, index) => index % 2 == 0).Max(key => Entry(key).Length) + 3;

        return string.Join('\n', keys.Chunk(2).Select(pair => pair.Length == 1 ? Entry(pair[0]) : Entry(pair[0]).PadRight(width) + Entry(pair[1])));
    }

    private static string Entry(KeyHelp key) => $"{key.HelpKey,-7}{key.Line}";

    private static string Help(IReadOnlyList<KeyHelp> keys, IReadOnlyList<ColumnHelp> columns)
    {
        int width = columns.Max(column => column.Name.Length) + 2;
        string indent = new(' ', width);
        IEnumerable<string> columnLines = columns.Select(column => $"{column.Name.PadRight(width)}{column.Meaning.Replace("\n", "\n" + indent, StringComparison.Ordinal)}");

        return string.Join('\n', ["Keys", KeyColumns(keys), string.Empty, "Columns", .. columnLines]);
    }
}