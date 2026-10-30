using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Application;

/// <summary>How a count beside the tabs is coloured; the screen maps each tone to a colour of its palette.</summary>
public enum CountTone
{
    Plain,
    Amber,
    Cyan,
    Green,
    Red,
    Grey,
}

/// <summary>One count beside the tabs, such as "◐ 2 need input" in amber.</summary>
public sealed record CountPart(string Text, CountTone Tone);

/// <summary>One item of the key bar: the key as the help names it, its word, the cell it starts at and the tooltip it shows.</summary>
public sealed record KeyBarItem(string Key, string Word, int X, string Tip)
{
    /// <summary>The cell after the item's last character.</summary>
    public int End => X + Key.Length + 1 + Word.Length;
}

/// <summary>The two rows of the key bar; <see cref="Width"/> is the cells the wider row reaches.</summary>
public sealed record KeyBar(IReadOnlyList<KeyBarItem> First, IReadOnlyList<KeyBarItem> Second)
{
    public int Width => First.Concat(Second).Select(item => item.End).DefaultIfEmpty(0).Max();
}

/// <summary>The words of the console's frame: the settings line, the tab titles, the counts beside them and the key bar.</summary>
public static partial class Wording
{
    private const string Ellipsis = "…";
    private const int BarStart = 1;
    private const int BarGap = 2;

    private static readonly string[] SecondBarRowForSessions = ["c", "e", "v", "p", "w", "/", ",", "?", "!", "q"];
    private static readonly string[] FirstBarRowForWorktrees = ["d", "D", "P", "s", "o", "enter"];

    /// <summary>The first row of the console: Vantage, Claude, the repository roots and the worktree root, the roots cut first and the worktree root next when the line is wider than <paramref name="width"/>.</summary>
    public static string SettingsLine(ToolVersion vantage, ClaudeCodeVersion claude, Settings settings, UpdateStatus update, MemorySize vantageMemory, int width)
    {
        ArgumentNullException.ThrowIfNull(settings);
        string head = $"Vantage {VantageVersion(vantage, update, vantageMemory)} · Claude {ClaudeVersion(claude)} · Repository roots ";
        string roots = string.Join("; ", settings.RepositoryRoots);
        string worktreeRoot = settings.WorktreeRoot.Value;
        string Line(string rootsText, string worktreeText) => $"{head}{rootsText} · Worktree root {worktreeText}";

        string whole = Line(roots, worktreeRoot);
        if (whole.Length <= width)
        {
            return whole;
        }

        int rootsRoom = width - Line(string.Empty, worktreeRoot).Length;
        if (rootsRoom >= Ellipsis.Length)
        {
            return Line(CutEnd(roots, rootsRoom), worktreeRoot);
        }

        int worktreeRoom = width - Line(Ellipsis, string.Empty).Length;

        return worktreeRoom >= Ellipsis.Length
            ? Line(Ellipsis, CutEnd(worktreeRoot, worktreeRoom))
            : CutEnd(Line(Ellipsis, Ellipsis), Math.Max(width, Ellipsis.Length));
    }

    /// <summary>A tab's title: its name, with the rows it lists in brackets once loaded.</summary>
    public static string TabTitleFor(ConsoleTab tab, int? count)
    {
        string name = tab switch
        {
            ConsoleTab.Sessions => "Sessions",
            ConsoleTab.Worktrees => "Worktrees",
        };

        return count is int rows ? $"{name} ({rows})" : name;
    }

    /// <summary>The sessions counts beside the tabs: a glyph, a count and a state word per state present, in attention order.</summary>
    public static IReadOnlyList<CountPart> SessionCounts(IReadOnlyList<SessionRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        DisplayStateKind[] order = [DisplayStateKind.NeedsInput, DisplayStateKind.Working, DisplayStateKind.Idle, DisplayStateKind.Completed, DisplayStateKind.Failed, DisplayStateKind.Stopped, DisplayStateKind.Unknown];

        return [.. order
            .Select(kind => (Kind: kind, Rows: rows.Where(row => row.Session.State.Kind == kind).ToList()))
            .Where(group => group.Rows.Count > 0)
            .Select(group => new CountPart($"{StateGlyph(group.Kind)} {group.Rows.Count} {CountedState(group.Kind, group.Rows)}", ToneOf(group.Kind)))];
    }

    /// <summary>The worktrees counts beside the tabs: a count per health present, then how many have changes and unpushed commits.</summary>
    public static IReadOnlyList<CountPart> WorktreeCounts(IReadOnlyList<WorktreeRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        IEnumerable<CountPart> byHealth = Enum.GetValues<WorktreeHealthKind>()
            .Select(kind => (Kind: kind, Count: rows.Count(row => row.Health.Kind == kind)))
            .Where(group => group.Count > 0)
            .Select(group => new CountPart($"{group.Count} {group.Kind.ToString().ToLowerInvariant()}", ToneOf(group.Kind)));
        int dirty = rows.Count(row => row.DirtyFiles > 0);
        int unpushed = rows.Count(row => row.UnpushedCommits > 0);
        IEnumerable<CountPart> atRisk =
        [
            .. dirty > 0 ? [new CountPart($"{dirty} with changes", CountTone.Amber)] : Array.Empty<CountPart>(),
            .. unpushed > 0 ? [new CountPart($"{unpushed} with unpushed commits", CountTone.Amber)] : Array.Empty<CountPart>(),
        ];

        return [.. byHealth, .. atRisk];
    }

    /// <summary>The sessions tab's key bar: the keys that act on the selected row, then the ones the listed rows call for, New session and Update; the folder and view keys below.</summary>
    public static KeyBar SessionsBar(SessionsViewState state, bool updateKnown)
    {
        ArgumentNullException.ThrowIfNull(state);
        IReadOnlyList<(string Key, string? Word)> forRow = state.Selected switch
        {
            null => [],
            { Session: BackgroundSession { ProcessId: not null } } => [("enter", null), ("o", null), ("x", null), ("d", null), ("r", null), ("m", null)],
            { Session: BackgroundSession } => [("enter", null), ("o", null), ("d", null), ("r", null)],
            { Session: InteractiveSession, HasTranscript: false } => [("b", null), ("d", "Close tab"), ("m", null)],
            _ => [("b", null), ("m", null)],
        };
        bool stoppedListed = state.Rows.Any(row => row.Session.State.Kind is DisplayStateKind.Stopped or DisplayStateKind.Failed);
        bool emptyTerminalListed = state.Rows.Any(row => row is { Session: InteractiveSession, HasTranscript: false });
        IReadOnlyList<(string Key, string? Word)> first =
        [
            .. forRow,
            .. stoppedListed ? [("R", (string?)null)] : Array.Empty<(string, string?)>(),
            .. emptyTerminalListed ? [("C", (string?)null)] : Array.Empty<(string, string?)>(),
            ("n", null),
            .. updateKnown ? [("U", (string?)null)] : Array.Empty<(string, string?)>(),
        ];

        return new KeyBar(Placed(SessionsKeys, first), Placed(SessionsKeys, [.. SecondBarRowForSessions.Select(key => (key, (string?)null))]));
    }

    /// <summary>The worktrees tab's key bar: the keys that act on a worktree and Update when a release is known; the folder and view keys below.</summary>
    public static KeyBar WorktreesBar(WorktreesViewState state, bool updateKnown)
    {
        ArgumentNullException.ThrowIfNull(state);
        IReadOnlyList<(string Key, string? Word)> first =
        [
            .. FirstBarRowForWorktrees.Select(key => (key, (string?)null)),
            .. updateKnown ? [("U", (string?)null)] : Array.Empty<(string, string?)>(),
        ];

        return new KeyBar(Placed(WorktreesKeys, first), Placed(WorktreesKeys, [.. SecondBarRowForSessions.Select(key => (key, (string?)null))]));
    }

    private static string CountedState(DisplayStateKind kind, List<SessionRow> rows) => kind switch
    {
        DisplayStateKind.NeedsInput => rows.Count == 1 ? "needs input" : "need input",
        DisplayStateKind.Working => rows[0].Session.State.Label,
        DisplayStateKind.Idle => rows[0].Session.State.Label,
        DisplayStateKind.Completed => rows[0].Session.State.Label,
        DisplayStateKind.Failed => rows[0].Session.State.Label,
        DisplayStateKind.Stopped => rows[0].Session.State.Label,
        DisplayStateKind.Unknown => rows[0].Session.State.Label,
    };

    private static CountTone ToneOf(DisplayStateKind kind) => kind switch
    {
        DisplayStateKind.NeedsInput => CountTone.Amber,
        DisplayStateKind.Working => CountTone.Cyan,
        DisplayStateKind.Idle => CountTone.Plain,
        DisplayStateKind.Completed => CountTone.Green,
        DisplayStateKind.Failed => CountTone.Red,
        DisplayStateKind.Stopped => CountTone.Grey,
        DisplayStateKind.Unknown => CountTone.Grey,
    };

    private static CountTone ToneOf(WorktreeHealthKind kind) => kind switch
    {
        WorktreeHealthKind.Ok => CountTone.Plain,
        WorktreeHealthKind.Locked => CountTone.Amber,
        WorktreeHealthKind.Orphan => CountTone.Red,
        WorktreeHealthKind.Broken => CountTone.Red,
        WorktreeHealthKind.Empty => CountTone.Grey,
        WorktreeHealthKind.Elsewhere => CountTone.Amber,
    };

    /// <summary>The items of one bar row, each placed two cells after the last; a word given overrides the key's hint.</summary>
    private static List<KeyBarItem> Placed(IReadOnlyList<KeyHelp> keys, IReadOnlyList<(string Key, string? Word)> row)
    {
        List<KeyBarItem> items = [];
        int x = BarStart;
        foreach ((string key, string? word) in row)
        {
            KeyHelp help = keys.First(candidate => candidate.Key == key);
            KeyBarItem item = new(help.HelpKey, word ?? help.Hint ?? help.Line, x, word is null ? help.Tip : TerminalTabTip);
            items.Add(item);
            x = item.End + BarGap;
        }

        return items;
    }

    private const string TerminalTabTip = "Close this terminal session's tab; it was never sent a message, so nothing is lost.";

    private static string CutEnd(string text, int width) => text.Length <= width ? text : text[..Math.Max(width - Ellipsis.Length, 0)] + Ellipsis;
}
