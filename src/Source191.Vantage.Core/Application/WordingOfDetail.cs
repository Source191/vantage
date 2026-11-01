using Source191.Vantage.Application.Detail;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using System.Diagnostics;
using System.Globalization;

namespace Source191.Vantage.Application;

/// <summary>One fact of the detail pane: a label in its column's label width and its text, in a tone, across one or more of the pane's three columns.</summary>
public sealed record DetailLine(string Label, string Text, TextTone Tone = TextTone.Plain, int Span = 1);

/// <summary>What the pane shows for one row: its title, its facts in rows of up to three columns, and the lines beneath them (a worktree's commits or changed files).</summary>
public sealed record DetailContent(string Title, IReadOnlyList<IReadOnlyList<DetailLine>> Facts, IReadOnlyList<DetailLine> Below)
{
    /// <summary>An empty pane: no title and no lines, shown before a row is selected and when the selection clears.</summary>
    public static readonly DetailContent Empty = new(string.Empty, [], []);
}

/// <summary>The detail pane's words: what the selected session's or worktree's row does not show.</summary>
public static partial class Wording
{
    /// <summary>The rows every session's and worktree's facts take.</summary>
    public const int DetailFactRows = 4;

    /// <summary>The most commits or changed files a worktree's pane lists; a line beneath says how many more.</summary>
    public const int DetailListedItems = 5;

    private const string Unavailable = "unavailable";
    private const string NotInARepository = "not in a repository";

    /// <summary>A session's facts in four rows, the folder and its changes two columns wide; its state and pull request are in the row.</summary>
    public static DetailContent Detail(SessionDetail detail, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(detail);
        SessionRow row = detail.Row;
        Session session = row.Session;
        IReadOnlyList<IReadOnlyList<DetailLine>> facts =
        [
            [
                WaitingLine(WaitingFor(row)),
                new("Started", TimeAgo(session.StartedAt, now)),
                new("Prompted", detail.LastPromptedAt is LastPrompt.Seen prompted ? TimeAgo(prompted.At, now) : "not seen since Vantage started"),
            ],
            [new("Folder", session.WorkingDirectory.Value, Span: 2), new("Session", session.SessionId.Value)],
            [new("Worktree", PlaceOf(row)), new("Branch", HeadLine(row.Head)), new("Tab", TabLine(row))],
            [
                new("Changes", CountsLine(detail.Counts, now), Span: 2),
                new("Memory", MemoryLine(row.Memory)),
            ],
        ];

        return new DetailContent(session.Name, facts, []);
    }

    /// <summary>A worktree's facts in four rows, its health and changes across the pane, and its unpushed commits or changed files beneath.</summary>
    public static DetailContent Detail(WorktreeDetail detail, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(detail);
        WorktreeRow row = detail.Row;
        IReadOnlyList<IReadOnlyList<DetailLine>> facts =
        [
            [new("Health", HealthLine(row.Health), ToneOf(row.Health.Kind), Span: 3)],
            [new("Branch", BranchLine(row.Head, detail.Merge), Span: 2), new("PR", PullRequestLine(row.PullRequest))],
            [
                new("Folder", row.Path.Value, Span: 2),
                new("Created", detail.Created is WorktreeOrigin.CreatedByVantage created ? $"by Vantage, {DayAndTime(created.Record.CreatedAt, now)}" : "not by Vantage"),
            ],
            [new("Changes", $"{Count(row.DirtyFiles, "changed file")} · {Count(row.UnpushedCommits, "unpushed commit")}", row.DirtyFiles + row.UnpushedCommits > 0 ? TextTone.Amber : TextTone.Plain, Span: 3)],
        ];

        return new DetailContent(row.FolderName, facts, Listing(detail, now));
    }

    /// <summary>The listing's reason, a first prompt for a session that needs input and has never had one, or nothing.</summary>
    private static WaitingReason WaitingFor(SessionRow row) => row switch
    {
        { Session.WaitingFor: WaitingReason.Given given } => given,
        { Session.State.Kind: DisplayStateKind.NeedsInput, HasTranscript: false } => new WaitingReason.Given("a first prompt"),
        _ => new WaitingReason.None(),
    };

    private static DetailLine WaitingLine(WaitingReason reason) => reason switch
    {
        WaitingReason.Given given => new("Waiting", given.Text, TextTone.Amber),
        WaitingReason.None => new("Waiting", "nothing"),
        _ => throw new UnreachableException(),
    };

    private static string MemoryLine(SessionMemory memory) => memory switch
    {
        SessionMemory.Measured measured => $"{Memory(measured.Size)} (m for the breakdown)",
        SessionMemory.NothingToMeasure => "nothing to measure",
        _ => throw new UnreachableException(),
    };

    /// <summary>Where in its repository the session runs: the main checkout, a path inside it, or a linked worktree's slug (its folder's name without the repository's).</summary>
    private static string PlaceOf(SessionRow row) => row.Location switch
    {
        SessionLocation.InRepository inRepository => PlaceIn(inRepository, row.Session.WorkingDirectory),
        SessionLocation.Outside => NotInARepository,
        _ => throw new UnreachableException(),
    };

    private static string PlaceIn(SessionLocation.InRepository repository, FolderPath folder)
    {
        FolderPath checkout = repository.Checkout;
        if (folder.Equals(checkout))
        {
            return "main checkout";
        }

        if (folder.IsUnder(checkout))
        {
            return folder.Value[(checkout.Value.Length + 1)..];
        }

        string prefix = repository.Name + "-";

        return folder.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? folder.Name[prefix.Length..] : folder.Name;
    }

    private static string TabLine(SessionRow row) => row switch
    {
        { Session: InteractiveSession } => "its own terminal",
        { Tab: LiveTab.Open open } => $"open, {open.Colour.Name.ToLowerInvariant()}",
        _ => "none",
    };

    private static string HeadLine(Head head) => head switch
    {
        Head.OnBranch on => on.Branch.Value,
        Head.Detached => "none",
        Head.Unknown => "none",
        _ => throw new UnreachableException(),
    };

    private static string PullRequestLine(PullRequestLookup lookup) => lookup is PullRequestLookup.Found ? $"{PullRequestCell(lookup)} (p opens it)" : "none";

    private static string CountsLine(DetailFact<WorktreeCounts> counts, DateTimeOffset now) => counts switch
    {
        DetailFact<WorktreeCounts>.Known known => CountsText(known.Value, now),
        DetailFact<WorktreeCounts>.NotApplicable => NotInARepository,
        _ => Unavailable,
    };

    private static string CountsText(WorktreeCounts counts, DateTimeOffset now)
    {
        string last = counts.LastCommitAt is LastCommit.Made made ? $" · last commit {Durations.Age(now - made.At)} ago" : string.Empty;

        return $"{Count(counts.DirtyFiles, "changed file")} · {Count(counts.UnpushedCommits, "unpushed commit")}{last}";
    }

    private static string HealthLine(WorktreeHealth health)
    {
        string kind = health.Kind.ToString().ToLowerInvariant();

        return health.Reason is HealthReason.None ? kind : $"{kind}: {Describe(health.Reason)}";
    }

    private static string BranchLine(Head head, DetailFact<MergeState> merge) => (head, merge) switch
    {
        (Head.Detached, _) => "none",
        (Head.Unknown, _) => "none",
        (Head.OnBranch on, DetailFact<MergeState>.Known known) => $"{on.Branch.Value}, {(known.Value.Merged ? "merged" : "not merged")} into {known.Value.Into}",
        (Head.OnBranch on, DetailFact<MergeState>.Unavailable) => $"{on.Branch.Value}, merge state unavailable",
        (Head.OnBranch on, _) => on.Branch.Value,
        _ => throw new UnreachableException(),
    };

    /// <summary>The unpushed commits as far as they were read, then how many more; or the changed files; or that git could not be read.</summary>
    private static List<DetailLine> Listing(WorktreeDetail detail, DateTimeOffset now) => (detail.Commits, detail.ChangedFiles) switch
    {
        (DetailFact<IReadOnlyList<UnpushedCommit>>.Known commits, _) => Listed("Unpushed", [.. commits.Value.Select(commit => $"{commit.Hash}  {Durations.Age(now - commit.At)}  {commit.Subject}")], detail.Row.UnpushedCommits),
        (_, DetailFact<IReadOnlyList<string>>.Known files) => Listed("Changed", files.Value, detail.Row.DirtyFiles),
        (DetailFact<IReadOnlyList<UnpushedCommit>>.Unavailable, _) or (_, DetailFact<IReadOnlyList<string>>.Unavailable) => [new("Git", Unavailable)],
        _ => [],
    };

    private static List<DetailLine> Listed(string label, IReadOnlyList<string> items, int total)
    {
        List<DetailLine> lines = [.. items.Select((item, index) => new DetailLine(index == 0 ? label : string.Empty, item))];
        if (total > items.Count)
        {
            lines.Add(new DetailLine(string.Empty, $"… {total - items.Count} more", TextTone.Grey));
        }

        return lines;
    }

    /// <summary>A time today as its hour and minute with how long ago; another day with its date.</summary>
    private static string TimeAgo(DateTimeOffset at, DateTimeOffset now) => $"{DayAndTime(at, now)} ({Durations.Age(now - at)} ago)";

    private static string DayAndTime(DateTimeOffset at, DateTimeOffset now)
    {
        DateTimeOffset local = at.ToOffset(now.Offset);

        return local.Date == now.Date ? local.ToString("HH:mm", CultureInfo.InvariantCulture) : local.ToString("d MMM HH:mm", CultureInfo.InvariantCulture);
    }
}