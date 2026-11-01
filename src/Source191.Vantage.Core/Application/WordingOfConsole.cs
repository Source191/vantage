using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Application;

/// <summary>How a piece of the frame's text is coloured; the screen maps each tone to a colour of its palette.</summary>
public enum TextTone
{
    Plain,
    Amber,
    Cyan,
    Green,
    Red,
    Grey,
    Orange,
}

/// <summary>A piece of the frame's text in its tone, such as "◐ 2 need input" in amber or "Vantage " in orange.</summary>
public sealed record TextPart(string Text, TextTone Tone);

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
    private const int BarStart = 1;
    private const int BarGap = 2;

    private static readonly string[] SecondBarRowForSessions = ["c", "f", "s", "p", "/", "?", "!", "q"];
    private static readonly string[] SecondBarRowForConfig = ["c", "f", "/", "?", "!", "q"];
    private static readonly string[] FirstBarRowForWorktrees = ["d", "D", "P", "n", "o", "enter"];

    private static readonly TextPart Dot = new(" · ", TextTone.Grey);

    /// <summary>The console's settings row, labels orange and values white: Vantage and Claude; the roots are on the Config tab.</summary>
    public static IReadOnlyList<TextPart> SettingsRow(ToolVersion vantage, ClaudeCodeVersion claude, UpdateStatus update, MemorySize vantageMemory) =>
        [Label("Vantage "), Value(VantageVersion(vantage, update, vantageMemory)), Dot, Label("Claude "), Value(ClaudeVersion(claude))];

    /// <summary>The Config counts beside the tabs: how many settings, then how much setup is left to do, in amber.</summary>
    public static IReadOnlyList<TextPart> ConfigCounts(IReadOnlyList<ConfigRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        int settings = rows.Count(row => row is not ConfigRow.Setup);
        int toSetUp = rows.Count(row => row is ConfigRow.Setup { State: SetupState.NotSetUp });

        return [new TextPart(Counted(settings, "setting"), TextTone.Plain), .. toSetUp > 0 ? [new TextPart($"{toSetUp} to set up", TextTone.Amber)] : Array.Empty<TextPart>()];
    }

    /// <summary>The Config tab's key bar: edit, then Update when a release is known; the folder and view keys below.</summary>
    public static KeyBar ConfigBar(ConfigViewState state, bool updateKnown)
    {
        ArgumentNullException.ThrowIfNull(state);
        IReadOnlyList<string> first = ["enter", .. updateKnown ? ["U"] : Array.Empty<string>()];

        return new KeyBar(Placed(ConfigKeys, first, []), Placed(ConfigKeys, SecondBarRowForConfig, []));
    }

    /// <summary>A tab's title, which never changes, so the tab strip never has to measure it again.</summary>
    public static string TabTitleFor(ConsoleTab tab) => tab switch
    {
        ConsoleTab.Sessions => "Sessions",
        ConsoleTab.Worktrees => "Worktrees",
        ConsoleTab.Config => "Config",
    };

    private static TextPart Label(string text) => new(text, TextTone.Orange);

    private static TextPart Value(string text) => new(text, TextTone.Plain);

    /// <summary>The sessions counts beside the tabs: a glyph, a count and a state word per state present, in attention order, then the automated runs and their memory while any run.</summary>
    public static IReadOnlyList<TextPart> SessionCounts(IReadOnlyList<SessionRow> rows, AutomatedRuns automated)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(automated);
        DisplayStateKind[] order = [DisplayStateKind.NeedsInput, DisplayStateKind.Working, DisplayStateKind.Idle, DisplayStateKind.Completed, DisplayStateKind.Failed, DisplayStateKind.Stopped, DisplayStateKind.Unknown];

        return [.. order
            .Select(kind => (Kind: kind, Rows: rows.Where(row => row.Session.State.Kind == kind).ToList()))
            .Where(group => group.Rows.Count > 0)
            .Select(group => new TextPart($"{StateGlyph(group.Kind)} {group.Rows.Count} {CountedState(group.Kind, group.Rows)}", ToneOf(group.Kind))),
            .. automated.Count > 0 ? [new TextPart($"◇ {automated.Count} automated {Memory(automated.Memory)}", TextTone.Grey)] : Array.Empty<TextPart>()];
    }

    /// <summary>The worktrees counts beside the tabs: a count per health present, then how many have changes and unpushed commits.</summary>
    public static IReadOnlyList<TextPart> WorktreeCounts(IReadOnlyList<WorktreeRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        IEnumerable<TextPart> byHealth = Enum.GetValues<WorktreeHealthKind>()
            .Select(kind => (Kind: kind, Count: rows.Count(row => row.Health.Kind == kind)))
            .Where(group => group.Count > 0)
            .Select(group => new TextPart($"{group.Count} {group.Kind.ToString().ToLowerInvariant()}", ToneOf(group.Kind)));
        int dirty = rows.Count(row => row.DirtyFiles > 0);
        int unpushed = rows.Count(row => row.UnpushedCommits > 0);
        IEnumerable<TextPart> atRisk =
        [
            .. dirty > 0 ? [new TextPart($"{dirty} with changes", TextTone.Amber)] : Array.Empty<TextPart>(),
            .. unpushed > 0 ? [new TextPart($"{unpushed} with unpushed commits", TextTone.Amber)] : Array.Empty<TextPart>(),
        ];

        return [.. byHealth, .. atRisk];
    }

    /// <summary>The sessions tab's key bar: the keys that act on the selected row, then the ones the listed rows call for, New session and Update; the folder and view keys below.</summary>
    public static KeyBar SessionsBar(SessionsViewState state, bool updateKnown)
    {
        ArgumentNullException.ThrowIfNull(state);
        (IReadOnlyList<string> Keys, IReadOnlyList<BarWord> Words) forRow = state.Selected switch
        {
            null => ([], []),
            { Session: BackgroundSession { Process: JobProcess.Running } } => (["enter", "x", "d", "r", "m"], []),
            { Session: BackgroundSession } => (["enter", "d", "r"], []),
            { Session: InteractiveSession, HasTranscript: false } => (["b", "d", "m"], [CloseTab]),
            _ => (["b", "m"], []),
        };
        bool stoppedListed = state.Rows.Any(row => row.Session.State.Kind is DisplayStateKind.Stopped or DisplayStateKind.Failed);
        bool emptyTerminalListed = state.Rows.Any(row => row is { Session: InteractiveSession, HasTranscript: false });
        IReadOnlyList<string> first =
        [
            .. forRow.Keys,
            .. stoppedListed ? ["R"] : Array.Empty<string>(),
            .. emptyTerminalListed ? ["C"] : Array.Empty<string>(),
            "n",
            .. updateKnown ? ["U"] : Array.Empty<string>(),
        ];

        return new KeyBar(Placed(SessionsKeys, first, forRow.Words), Placed(SessionsKeys, SecondBarRowForSessions, []));
    }

    /// <summary>The worktrees tab's key bar: the keys that act on a worktree and Update when a release is known; the folder and view keys below.</summary>
    public static KeyBar WorktreesBar(WorktreesViewState state, bool updateKnown)
    {
        ArgumentNullException.ThrowIfNull(state);
        IReadOnlyList<string> first = [.. FirstBarRowForWorktrees, .. updateKnown ? ["U"] : Array.Empty<string>()];

        return new KeyBar(Placed(WorktreesKeys, first, []), Placed(WorktreesKeys, SecondBarRowForSessions, []));
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

    private static TextTone ToneOf(DisplayStateKind kind) => kind switch
    {
        DisplayStateKind.NeedsInput => TextTone.Amber,
        DisplayStateKind.Working => TextTone.Cyan,
        DisplayStateKind.Idle => TextTone.Plain,
        DisplayStateKind.Completed => TextTone.Green,
        DisplayStateKind.Failed => TextTone.Red,
        DisplayStateKind.Stopped => TextTone.Grey,
        DisplayStateKind.Unknown => TextTone.Grey,
    };

    private static TextTone ToneOf(WorktreeHealthKind kind) => kind switch
    {
        WorktreeHealthKind.Ok => TextTone.Plain,
        WorktreeHealthKind.Locked => TextTone.Amber,
        WorktreeHealthKind.Orphan => TextTone.Red,
        WorktreeHealthKind.Broken => TextTone.Red,
        WorktreeHealthKind.Empty => TextTone.Grey,
        WorktreeHealthKind.Elsewhere => TextTone.Amber,
    };

    /// <summary>The items of one bar row, each placed two cells after the last; a word given for a key overrides its hint and tip.</summary>
    private static List<KeyBarItem> Placed(IReadOnlyList<KeyHelp> keys, IReadOnlyList<string> row, IReadOnlyList<BarWord> words)
    {
        List<KeyBarItem> items = [];
        int x = BarStart;
        foreach (string key in row)
        {
            KeyHelp help = keys.First(candidate => candidate.Key == key);
            KeyBarItem item = words.FirstOrDefault(word => word.Key == key) is BarWord word
                ? new(help.HelpKey, word.Word, x, word.Tip)
                : OwnItem(help, x);
            items.Add(item);
            x = item.End + BarGap;
        }

        return items;
    }

    /// <summary>A key's item in its own words: its hint, or its help line for a key the bar has no hint for.</summary>
    private static KeyBarItem OwnItem(KeyHelp help, int x) => new(help.HelpKey, help.OnBar ? help.Hint : help.Line, x, help.Tip);

    private const string TerminalTabTip = "Close this terminal session's tab; it was never sent a message, so nothing is lost.";

    /// <summary>On an empty terminal session's row, d closes its tab rather than deleting a session.</summary>
    private static readonly BarWord CloseTab = new("d", "Close tab", TerminalTabTip);

    /// <summary>A bar word that differs from the key's own hint, with the tip that goes with it.</summary>
    private sealed record BarWord(string Key, string Word, string Tip);

}