using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using System.Diagnostics;
using System.Globalization;

namespace Source191.Vantage.Application;

/// <summary>The words around the table screens: the tab title, the header's values, the table title, the key hints and the help.</summary>
public static partial class Wording
{
    private const string UnknownVersion = "unknown";

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
        DialogName.Settings => "Settings",
        DialogName.FolderPicker => "Browse",
        DialogName.DeleteSession => "Delete session",
        DialogName.DeleteBranch => "Delete branch",
        DialogName.CloseEmpty => "Close empty",
        DialogName.ForceRemove => "Force remove",
        DialogName.OpenSolution => "Open solution",
        DialogName.Update => "Update",
        DialogName.Memory => "Memory",
        DialogName.ProblemReport => ProblemReportDialogTitle,
    };

    /// <summary>An amount of memory as the settings row, the detail pane and the breakdown write it: whole megabytes below 1,000 MB, gigabytes to one decimal from there.</summary>
    public static string Memory(MemorySize size)
    {
        double megabytes = (double)size.Bytes / MemorySize.BytesPerMegabyte;
        long whole = (long)Math.Round(megabytes);

        return whole < 1000 ? $"{whole} MB" : $"{(megabytes / 1024).ToString("F1", CultureInfo.InvariantCulture)} GB";
    }

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

    /// <summary>The keys every tab shares for moving between tabs and scrolling a table too wide for the terminal; only the help lists them.</summary>
    private static readonly KeyHelp[] TabKeyHelp =
    [
        KeyHelp.HelpOnly("1", "the Sessions tab", tip: "Show the Sessions tab: every Claude Code session on this machine."),
        KeyHelp.HelpOnly("2", "the Worktrees tab", tip: "Show the Worktrees tab: every worktree under your roots."),
        KeyHelp.HelpOnly("3", "the Config tab", tip: "Show the Config tab: the settings and how Vantage is set up."),
        KeyHelp.HelpOnly("←", "the tab to the left", tip: "Show the tab to the left of this one."),
        KeyHelp.HelpOnly("→", "the tab to the right", tip: "Show the tab to the right of this one."),
        KeyHelp.HelpOnly("<", "scroll the columns left", tip: "Scroll a table too wide for the terminal to the left."),
        KeyHelp.HelpOnly(">", "scroll the columns right", tip: "Scroll a table too wide for the terminal to the right."),
    ];

    public static IReadOnlyList<KeyHelp> SessionsKeys { get; } =
    [
        new("enter", "Open", "open in its own tab, or switch to it", Tip: "Open this session in its own tab, or switch to that tab if it is open."),
        new("n", "New session", "new session", Tip: "Start a new session, in a new worktree or on a current branch."),
        new("r", "Rename", "rename (restarts the session)", Tip: "Give the session a new name; it restarts under it, conversation kept."),
        new("x", "Stop", "stop", Tip: "Stop the session; it stays listed as stopped and can be resumed later."),
        new("d", "Delete", "delete, and its worktree if nothing is lost", Tip: "Delete the session, and its worktree too when no work would be lost."),
        new("R", "Resume", "resume every stopped or failed session shown, each in its own tab", Tip: "Resume each stopped or failed session shown, in its own tab; / filters which."),
        new("C", "Close empty", "close tabs of unused terminal sessions", Tip: "Close the tabs of terminal sessions that were never sent a message."),
        new("b", "Adopt", "move a terminal session under Vantage", Tip: "Move a plain claude session from its own tab under Vantage's supervision."),
        new("/", "Filter", "filter", Tip: "Type to show only the rows that match; Esc clears the filter."),
        new("?", "Help", "help", Tip: "Show every key and what each column means."),
        new("q", "Quit", "quit", Tip: "Quit Vantage; your sessions keep running in the background."),
        new("c", "Copy path", "copy the folder path", Tip: "Copy the row's folder path to the clipboard."),
        new("f", "Open folder", "open the folder in your editor", Tip: "Open the row's folder in the editor chosen on the Config tab."),
        new("s", "Open solution", "open its solution", Tip: "Open the row's Visual Studio solution, asking when there are several."),
        new("p", "Open PR", "open its pull request in the browser", Tip: "Open the row's pull request in your browser."),
        new("m", "Memory", "what its memory is made of", Tip: "Show what the session's memory is made of; the detail pane shows the total."),
        new("U", "Update", UpdateLine, Tip: "Update Vantage to the newest release, in a tab of its own."),
        new("!", "Report", ReportLine, Tip: "Report a problem: preview the diagnostics, then open a GitHub issue."),
        .. TabKeyHelp,
    ];

    public static IReadOnlyList<KeyHelp> WorktreesKeys { get; } =
    [
        new("d", "Remove", "remove (refused when work would be lost)", Tip: "Remove this worktree; refused when it holds work that would be lost."),
        new("D", "Force remove", "force remove after typing its name", Tip: "Remove this worktree whatever it holds, after you type its name."),
        new("P", "Prune", "prune stale registrations in its repository", Tip: "Prune the stale worktree registrations of this row's repository."),
        new("n", "New session", "start a session in this worktree", Tip: "Start a new session in this worktree, on the branch checked out there."),
        new("o", "Open shell", "open a PowerShell tab there", Tip: "Open a PowerShell tab in this worktree's folder."),
        new("enter", "Open session", "open its session's tab, or switch to it", Tip: "Open this worktree's session in its own tab, or switch to that tab if open."),
        new("esc", "Sessions", "back to sessions (clears a filter first)", Tip: "Clear the filter, or go back to the sessions view when there is none."),
        new("/", "Filter", "filter", Tip: "Type to show only the rows that match; Esc clears the filter."),
        new("?", "Help", "help", Tip: "Show every key and what each column means."),
        new("q", "Quit", "quit", Tip: "Quit Vantage; your sessions keep running in the background."),
        new("c", "Copy path", "copy the folder path", Tip: "Copy the row's folder path to the clipboard."),
        new("f", "Open folder", "open the folder in your editor", Tip: "Open the row's folder in the editor chosen on the Config tab."),
        new("s", "Open solution", "open its solution", Tip: "Open the row's Visual Studio solution, asking when there are several."),
        new("p", "Open PR", "open its pull request in the browser", Tip: "Open the row's pull request in your browser."),
        new("U", "Update", UpdateLine, Tip: "Update Vantage to the newest release, in a tab of its own."),
        new("!", "Report", ReportLine, Tip: "Report a problem: preview the diagnostics, then open a GitHub issue."),
        .. TabKeyHelp,
    ];

    public static IReadOnlyList<KeyHelp> ConfigKeys { get; } =
    [
        new("enter", "Edit", "edit the setting, or say how to set up", Tip: "Edit this setting; on a repository root, every root one per line; on a setup row, say how that setup is changed."),
        new("esc", "Sessions", "back to sessions (clears a filter first)", Tip: "Clear the filter, or go back to the sessions view when there is none."),
        new("/", "Filter", "filter", Tip: "Type to show only the rows that match; Esc clears the filter."),
        new("?", "Help", "help", Tip: "Show every key and what each column means."),
        new("q", "Quit", "quit", Tip: "Quit Vantage; your sessions keep running in the background."),
        new("c", "Copy", "copy the path", Tip: "Copy the row's path to the clipboard."),
        new("f", "Open folder", "open the folder, or the one holding the file", Tip: "Open the row's folder, or the folder holding its file, in the editor chosen above."),
        new("U", "Update", UpdateLine, Tip: "Update Vantage to the newest release, in a tab of its own."),
        new("!", "Report", ReportLine, Tip: "Report a problem: preview the diagnostics, then open a GitHub issue."),
        .. TabKeyHelp,
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

    /// <summary>The glyph before a worktree health's word, so the health reads without its colour: a closed enum, so a new kind fails the build here.</summary>
    public static string HealthGlyph(WorktreeHealthKind kind) => kind switch
    {
        WorktreeHealthKind.Ok => "✓",
        WorktreeHealthKind.Locked => "◆",
        WorktreeHealthKind.Orphan => "△",
        WorktreeHealthKind.Broken => "✗",
        WorktreeHealthKind.Empty => "□",
        WorktreeHealthKind.Elsewhere => "↗",
    };

    /// <summary>The HEALTH cell: the health's glyph and its word, in the row's colour.</summary>
    public static string HealthCell(WorktreeHealthKind kind) => $"{HealthGlyph(kind)} {HealthWord(kind)}";

    /// <summary>A health's word, the one the HEALTH cell, the counts and the pane's Health line all use.</summary>
    private static string HealthWord(WorktreeHealthKind kind) => kind.ToString().ToLowerInvariant();

    public static IReadOnlyList<ColumnHelp> SessionsColumns { get; } =
    [
        new("NAME", "the session name given at launch, or the folder name for an unnamed one"),
        new("TAB", $"a block in the tab's colour, faded as the tab looks from Vantage's own tab,\nwhile a Vantage tab shows the session; {TerminalSwatch} grey for a plain claude in its own terminal"),
        new("STATE", "◐ needs input (amber), ● working (cyan), ○ idle (white),\n✓ completed (green), ✗ failed (red), ■ stopped (grey)"),
        new("REPOSITORY", "the repository it runs in, or the folder's label outside a repository"),
        new("PR", "its pull request: #123 on GitHub (open, draft, merged, closed),\n!123 on Azure DevOps (active, draft, completed, abandoned); blank if none or not yet known"),
        new("AGE", "time since the session started"),
    ];

    public static IReadOnlyList<ColumnHelp> WorktreesColumns { get; } =
    [
        new("WORKTREE", "the owning repository › the folder's slug, or its label when not named after it;\nwhite ok, amber locked, red broken or orphan, grey empty"),
        new("LAST COMMIT", "time since the last commit"),
        new("SESSION", "the session running in it, if any"),
    ];

    /// <summary>The sessions pane's lines, for the help.</summary>
    public static IReadOnlyList<ColumnHelp> SessionsPane { get; } =
    [
        new("Waiting", "what it waits for: Claude Code's reason, a first prompt, or nothing"),
        new("Started", "when the session started"),
        new("Prompted", "when Vantage last saw it go from waiting or idle to working"),
        new("Folder", "the folder it runs in"),
        new("Session", "the session id"),
        new("Worktree", "the worktree's slug, the path inside the repository, or the main checkout"),
        new("Branch", "the branch checked out there"),
        new("Tab", "its Vantage tab and the tab's colour, or its own terminal"),
        new("Changes", "changed files, unpushed commits and the last commit there"),
        new("Memory", "what the session and everything it started use; m for the breakdown"),
    ];

    /// <summary>The worktrees pane's lines, for the help.</summary>
    public static IReadOnlyList<ColumnHelp> WorktreesPane { get; } =
    [
        new("Health", "ok; locked (by another tool); orphan (a folder git has forgotten);\nbroken (registration or owner gone); empty (a leftover folder);\nelsewhere (a worktree of a repository outside the roots); then the reason in full"),
        new("Branch", "the branch checked out there, and whether it is merged into origin's default branch"),
        new("PR", "its pull request, as the sessions table shows it, or none; p opens it"),
        new("Folder", "the worktree's folder"),
        new("Created", "when Vantage created it, or not by Vantage"),
        new("Changes", "changed or untracked files, and commits on its branch that no remote has"),
        new("Unpushed", "the newest five unpushed commits, or the changed files when none, and how many more"),
    ];

    public static IReadOnlyList<ColumnHelp> ConfigColumns { get; } =
    [
        new("SETTING", "a setting Vantage keeps in config.json, or, in grey, setup it reads but does not change"),
        new("VALUE", "its value; a setup row that needs attention says what to run"),
    ];

    /// <summary>The sessions view's help: its keys and columns, then the Claude Code executable Vantage drives.</summary>
    public static string SessionsHelp(string claudeExecutable) => $"{Help(SessionsKeys, SessionsColumns, SessionsPane)}\n\nClaude Code: {claudeExecutable}";

    public static string WorktreesHelp => Help(WorktreesKeys, WorktreesColumns, WorktreesPane);

    public static string ConfigHelp => Help(ConfigKeys, ConfigColumns, pane: []);

    /// <summary>The keys two to a line, so the dialog stays short enough for a small terminal.</summary>
    private static string KeyColumns(IReadOnlyList<KeyHelp> keys)
    {
        int keyWidth = Math.Max(keys.Max(key => key.HelpKey.Length) + 1, 7);
        string Entry(KeyHelp key) => $"{key.HelpKey.PadRight(keyWidth)}{key.Line}";
        int width = keys.Where((_, index) => index % 2 == 0).Max(key => Entry(key).Length) + 3;

        return string.Join('\n', keys.Chunk(2).Select(pair => pair.Length == 1 ? Entry(pair[0]) : Entry(pair[0]).PadRight(width) + Entry(pair[1])));
    }

    /// <summary>The keys, the columns, and the pane's lines when the view has a pane.</summary>
    private static string Help(IReadOnlyList<KeyHelp> keys, IReadOnlyList<ColumnHelp> columns, IReadOnlyList<ColumnHelp> pane)
    {
        IEnumerable<string> paneSection = pane.Count == 0 ? [] : [string.Empty, "Pane", .. Explained(pane)];

        return string.Join('\n', ["Keys", KeyColumns(keys), string.Empty, "Columns", .. Explained(columns), .. paneSection]);
    }

    /// <summary>Each name padded to the longest, its meaning beside it and its further lines indented to match.</summary>
    private static IEnumerable<string> Explained(IReadOnlyList<ColumnHelp> entries)
    {
        int width = entries.Max(entry => entry.Name.Length) + 2;
        string indent = new(' ', width);

        return entries.Select(entry => $"{entry.Name.PadRight(width)}{entry.Meaning.Replace("\n", "\n" + indent, StringComparison.Ordinal)}");
    }
}