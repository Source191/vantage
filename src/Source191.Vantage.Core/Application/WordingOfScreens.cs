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

    private static string Counted(int count, string noun) => count == 1 ? $"{count} {noun}" : $"{count} {noun}s";

    public static IReadOnlyList<KeyHelp> SessionsKeys { get; } =
    [
        new("enter", "Attach", "attach (Ctrl+Z or Esc comes back)", Column: 0),
        new("o", "Open in tab", "open in its own tab, or switch to it", Column: 0),
        new("n", "New session", "new session", Column: 0),
        new("r", "Rename", "rename (restarts the session)", Column: 0),
        new("x", "Stop", "stop", Column: 0),
        new("d", "Delete", "delete, and its worktree if nothing is lost", Column: 0),
        new("R", "Resume stopped", "resume after a reboot", Column: 1),
        new("C", "Close empty tabs", "close tabs of unused terminal sessions", Column: 1),
        new("b", Hint: null, "move a terminal session under Vantage", Column: 1),
        new("a", "Agent view", "native agent view", Column: 1),
        new("w", "Worktrees", "worktrees view", Column: 1),
        new("/", "Filter", "filter", Column: 1),
        new("?", "Help", "help", Column: 1),
        new("q", "Quit", "quit", Column: 1),
        new(",", "Settings", SettingsLine, Column: 0),
        new("c", "Copy path", "copy the folder path", Column: 2),
        new("e", "Editor", "open the folder in your editor", Column: 2),
        new("v", "Solution", "open its solution", Column: 2),
        new("p", "Open PR", "open its pull request in the browser", Column: 2),
        new("U", Hint: null, UpdateLine, Column: 1),
    ];

    public static IReadOnlyList<KeyHelp> WorktreesKeys { get; } =
    [
        new("d", "Remove", "remove (refused when work would be lost)", Column: 0),
        new("D", "Force remove", "force remove after typing its name", Column: 0),
        new("P", "Prune", "prune stale registrations in its repository", Column: 0),
        new("s", "Start session", "start a session in this worktree", Column: 0),
        new("o", "Open shell", "open a PowerShell tab there", Column: 0),
        new("enter", "Go to session", "go to the session running in it", Column: 0),
        new("w", "Sessions", "back to sessions", Column: 1),
        new("esc", "Sessions", "back to sessions (clears a filter first)", Column: 1),
        new("/", "Filter", "filter", Column: 1),
        new("?", "Help", "help", Column: 1),
        new("q", "Quit", "quit", Column: 1),
        new(",", "Settings", SettingsLine, Column: 1),
        new("c", "Copy path", "copy the folder path", Column: 2),
        new("e", "Editor", "open the folder in your editor", Column: 2),
        new("v", "Solution", "open its solution", Column: 2),
        new("p", "Open PR", "open its pull request in the browser", Column: 2),
        new("U", Hint: null, UpdateLine, Column: 1),
    ];

    public static IReadOnlyList<ColumnHelp> SessionsColumns { get; } =
    [
        new("NAME", "the session name given at launch, or the folder name for an unnamed one"),
        new("STATE", "needs input (amber), working (cyan), idle (white),\ncompleted (green), failed (red), stopped (grey)"),
        new("REPOSITORY", "the repository the session runs in"),
        new("FOLDER", "where it runs, relative to the repository or worktree root that holds it, else ~\\ from your home folder, else in full"),
        new("BRANCH", "the branch checked out there"),
        new("PR", "its pull request: #123 on GitHub (open, draft, merged, closed), !123 on Azure DevOps\n(active, draft, completed, abandoned); blank when there is none or it is still being looked up"),
        new("AGE", "time since the session started"),
        new("TAB", "terminal for a plain claude started in its own tab, open while a Vantage tab shows it"),
    ];

    public static IReadOnlyList<ColumnHelp> WorktreesColumns { get; } =
    [
        new("REPOSITORY", "the repository that owns the worktree"),
        new("FOLDER", "the worktree's folder, relative to the root that holds it"),
        new("BRANCH", "the branch checked out there"),
        new("PR", "its pull request: #123 on GitHub (open, draft, merged, closed), !123 on Azure DevOps\n(active, draft, completed, abandoned); blank when there is none or it is still being looked up"),
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