using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using System.Diagnostics;

namespace Source191.Vantage.Application;

/// <summary>The words around the table screens: the tab title, the header's values, the table title, the key hints and the help.</summary>
public static partial class Wording
{
    private const string UnknownVersion = "unknown";

    private const string SettingsLine = "edit roots, worktree root and editor";

    private const string UpdateLine = "update Vantage in a new tab";

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
    };

    /// <summary>The header's first four values, one per line: Vantage's version, the Claude Code version, the repository roots and the worktree root.</summary>
    public static string HeaderValues(ToolVersion vantage, ClaudeCodeVersion claude, Settings settings, UpdateStatus update)
    => string.Join('\n', VantageVersion(vantage, update), ClaudeVersion(claude), string.Join("; ", settings.RepositoryRoots), settings.WorktreeRoot);

    /// <summary>The Claude Code header value: the version as the CLI printed it, and the minimum with a prompt to update when it is older.</summary>
    private static string ClaudeVersion(ClaudeCodeVersion claude) => claude switch
    {
        ClaudeCodeVersion.Unknown => UnknownVersion,
        ClaudeCodeVersion.Reported reported => reported.Text,
        ClaudeCodeVersion.BelowMinimum old => $"{old.Installed} (below {old.Minimum}, update Claude Code)",
        _ => throw new UnreachableException(),
    };

    /// <summary>The Vantage header value: the running version, and the newer release with the update key when one is known.</summary>
    private static string VantageVersion(ToolVersion vantage, UpdateStatus update)
        => update is UpdateStatus.Available available ? $"{vantage.Text} ({available.Newest} available, U to update)" : vantage.Text;

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

    /// <summary>The header's Sessions line: how many are listed, with a count for each state other than needs input, busiest first.</summary>
    public static string SessionsSummary(IReadOnlyList<SessionRow> rows)
    {
        string[] byState = [.. rows
            .Where(row => row.Session.State.Kind != DisplayStateKind.NeedsInput)
            .GroupBy(row => row.Session.State.Label)
            .OrderByDescending(group => group.Count())
            .Select(group => $"{group.Count()} {group.Key}")];

        return (rows.Count, byState.Length) switch
        {
            (0, _) => "none",
            (_, 0) => $"{rows.Count}",
            _ => $"{rows.Count} ({string.Join(", ", byState)})",
        };
    }

    private static string Counted(int count, string noun) => count == 1 ? $"{count} {noun}" : $"{count} {noun}s";

    public static IReadOnlyList<KeyHelp> SessionsKeys { get; } =
    [
        new("enter", "Attach", "attach (Ctrl+Z or Esc comes back)", Column: 0, Tip: "Attach to this session here; Ctrl+Z brings you back to Vantage."),
        new("o", "Open in tab", "open in its own tab, or switch to it", Column: 0, Tip: "Open this session in its own tab, or switch to that tab if it is open."),
        new("n", "New session", "new session", Column: 0, Tip: "Start a new session, in a new worktree or on a current branch."),
        new("r", "Rename", "rename (restarts the session)", Column: 0, Tip: "Give the session a new name; it restarts under it, conversation kept."),
        new("x", "Stop", "stop", Column: 0, Tip: "Stop the session; it stays listed as stopped and can be resumed later."),
        new("d", "Delete", "delete, and its worktree if nothing is lost", Column: 0, Tip: "Delete the session, and its worktree too when no work would be lost."),
        new("R", "Resume stopped", "resume after a reboot", Column: 1, Tip: "Resume the stopped sessions you choose, for example after a reboot."),
        new("C", "Close empty tabs", "close tabs of unused terminal sessions", Column: 1, Tip: "Close the tabs of terminal sessions that were never sent a message."),
        new("b", Hint: null, "move a terminal session under Vantage", Column: 1, Tip: "Move a plain claude session from its own tab under Vantage's supervision."),
        new("w", "Worktrees", "worktrees view", Column: 1, Tip: "Switch to the worktrees view: every worktree under your roots."),
        new("/", "Filter", "filter", Column: 1, Tip: "Type to show only the rows that match; Esc clears the filter."),
        new("?", "Help", "help", Column: 1, Tip: "Show every key and what each column means."),
        new("q", "Quit", "quit", Column: 1, Tip: "Quit Vantage; your sessions keep running in the background."),
        new(",", "Settings", SettingsLine, Column: 0, Tip: "Edit the repository roots, the worktree root, the editor and the base."),
        new("c", "Copy path", "copy the folder path", Column: 2, Tip: "Copy the row's folder path to the clipboard."),
        new("e", "Open folder", "open the folder in your editor", Column: 2, Tip: "Open the row's folder in the editor chosen in Settings."),
        new("v", "Solution", "open its solution", Column: 2, Tip: "Open the row's Visual Studio solution, asking when there are several."),
        new("p", "Open PR", "open its pull request in the browser", Column: 2, Tip: "Open the row's pull request in your browser."),
        new("U", Hint: null, UpdateLine, Column: 1, Tip: "Update Vantage to the newest release, in a tab of its own."),
    ];

    public static IReadOnlyList<KeyHelp> WorktreesKeys { get; } =
    [
        new("d", "Remove", "remove (refused when work would be lost)", Column: 0, Tip: "Remove this worktree; refused when it holds work that would be lost."),
        new("D", "Force remove", "force remove after typing its name", Column: 0, Tip: "Remove this worktree whatever it holds, after you type its name."),
        new("P", "Prune", "prune stale registrations in its repository", Column: 0, Tip: "Prune the stale worktree registrations of this row's repository."),
        new("s", "Start session", "start a session in this worktree", Column: 0, Tip: "Start a new session in this worktree, on the branch checked out there."),
        new("o", "Open shell", "open a PowerShell tab there", Column: 0, Tip: "Open a PowerShell tab in this worktree's folder."),
        new("enter", "Go to session", "go to the session running in it", Column: 0, Tip: "Go to the session running in this worktree, in the sessions view."),
        new("w", "Sessions", "back to sessions", Column: 1, Tip: "Go back to the sessions view."),
        new("esc", "Sessions", "back to sessions (clears a filter first)", Column: 1, Tip: "Clear the filter, or go back to the sessions view when there is none."),
        new("/", "Filter", "filter", Column: 1, Tip: "Type to show only the rows that match; Esc clears the filter."),
        new("?", "Help", "help", Column: 1, Tip: "Show every key and what each column means."),
        new("q", "Quit", "quit", Column: 1, Tip: "Quit Vantage; your sessions keep running in the background."),
        new(",", "Settings", SettingsLine, Column: 1, Tip: "Edit the repository roots, the worktree root, the editor and the base."),
        new("c", "Copy path", "copy the folder path", Column: 2, Tip: "Copy the row's folder path to the clipboard."),
        new("e", "Open folder", "open the folder in your editor", Column: 2, Tip: "Open the row's folder in the editor chosen in Settings."),
        new("v", "Solution", "open its solution", Column: 2, Tip: "Open the row's Visual Studio solution, asking when there are several."),
        new("p", "Open PR", "open its pull request in the browser", Column: 2, Tip: "Open the row's pull request in your browser."),
        new("U", Hint: null, UpdateLine, Column: 1, Tip: "Update Vantage to the newest release, in a tab of its own."),
    ];

    public static IReadOnlyList<ColumnHelp> SessionsColumns { get; } =
    [
        new("NAME", "the session name given at launch, or the folder name for an unnamed one"),
        new("STATE", "needs input (amber), working (cyan), idle (white),\ncompleted (green), failed (red), stopped (grey)"),
        new("REPOSITORY", "the repository the session runs in"),
        new("FOLDER", "where it runs, relative to the repository or worktree root that holds it,\nelse ~\\ from your home folder, else in full"),
        new("BRANCH", "the branch checked out there"),
        new("PR", "its pull request: #123 on GitHub (open, draft, merged, closed),\n!123 on Azure DevOps (active, draft, completed, abandoned); blank if none or not yet known"),
        new("AGE", "time since the session started"),
        new("TAB", "terminal for a plain claude started in its own tab, open while a Vantage tab shows it"),
    ];

    public static IReadOnlyList<ColumnHelp> WorktreesColumns { get; } =
    [
        new("REPOSITORY", "the repository that owns the worktree"),
        new("FOLDER", "the worktree's folder, relative to the root that holds it"),
        new("BRANCH", "the branch checked out there"),
        new("PR", "its pull request: #123 on GitHub (open, draft, merged, closed),\n!123 on Azure DevOps (active, draft, completed, abandoned); blank if none or not yet known"),
        new("DIRTY", "changed or untracked files"),
        new("UNPUSHED", "commits on its branch that no remote has"),
        new("LAST COMMIT", "time since the last commit"),
        new("SESSION", "the session running in it, if any"),
        new("HEALTH", "ok; locked (by another tool); orphan (a folder git has forgotten);\nbroken (registration or owner gone); empty (a leftover folder)"),
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