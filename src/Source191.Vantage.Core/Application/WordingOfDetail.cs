using Source191.Vantage.Application.Detail;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using System.Globalization;

namespace Source191.Vantage.Application;

/// <summary>One line of the detail pane: a label in the label column and its text, in a tone.</summary>
public sealed record DetailLine(string Label, string Text, TextTone Tone = TextTone.Plain);

/// <summary>The detail pane's words: a selected session's or worktree's facts, and why a session has no output to show.</summary>
public static partial class Wording
{
    private const string Unavailable = "unavailable";

    public static IReadOnlyList<DetailLine> DetailLines(SessionDetail detail, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(detail);
        SessionRow row = detail.Row;
        Session session = row.Session;

        return
        [
            new("State", StateWithReason(row), ToneOf(session.State.Kind)),
            new("Started", TimeAgo(session.StartedAt, now)),
            new("Prompted", detail.LastPromptedAt is DateTimeOffset prompted ? TimeAgo(prompted, now) : "not seen since Vantage started"),
            new("Session", session.SessionId.Value),
            new("Folder", session.WorkingDirectory.Value),
            new("Branch", row.Branch?.Value ?? "none"),
            new("Pull request", PullRequestLine(row.PullRequest)),
            new("Tab", TabLine(row)),
            new("Worktree", CountsLine(detail.Counts, now)),
            new("Memory", row.Memory is MemorySize memory ? $"{Memory(memory)} (m for the breakdown)" : "nothing to measure"),
        ];
    }

    public static IReadOnlyList<DetailLine> DetailLines(WorktreeDetail detail, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(detail);
        WorktreeRow row = detail.Row;
        IReadOnlyList<DetailLine> facts =
        [
            new("Health", HealthLine(row.Health), ToneOf(row.Health.Kind)),
            new("Created", detail.Created is CreatedWorktree created ? $"by Vantage, {DayAndTime(created.CreatedAt, now)}" : "not by Vantage"),
            new("Folder", row.Path.Value),
            new("Branch", BranchLine(row.Branch, detail.Merge)),
            new("Pull request", PullRequestLine(row.PullRequest)),
            new("Risk", $"{Count(row.DirtyFiles, "changed file")} · {Count(row.UnpushedCommits, "unpushed commit")}", row.DirtyFiles + row.UnpushedCommits > 0 ? TextTone.Amber : TextTone.Plain),
            new("Session", row.SessionName is string name ? $"{name} (Enter goes to it)" : "none"),
        ];

        return [.. facts, .. Listing(detail, now)];
    }

    /// <summary>The output area when there is no screen to show, or the screen's rows; a screen not read this time has no lines of its own.</summary>
    public static IReadOnlyList<string> OutputLines(SessionOutput output) => output switch
    {
        SessionOutput.Screen screen => screen.Rows,
        SessionOutput.NoProcess => ["no output: the session has no process"],
        SessionOutput.NotSupervised => ["no output: a session outside the supervisor has none to show"],
        SessionOutput.Unavailable => ["output unavailable"],
        _ => [],
    };

    private static string StateWithReason(SessionRow row)
    {
        string? reason = row.Session.WaitingFor ?? (row.Session.State.Kind == DisplayStateKind.NeedsInput && !row.HasTranscript ? "no prompt sent yet" : null);

        return reason is null ? row.Session.State.Label : $"{row.Session.State.Label} ({reason})";
    }

    private static string TabLine(SessionRow row) => row switch
    {
        { Session: InteractiveSession } => "its own terminal",
        { HasLiveTab: true, TabColour: TabColour colour } => $"open, {colour.Name.ToLowerInvariant()}",
        _ => "none",
    };

    private static string PullRequestLine(PullRequestLookup lookup) => lookup is PullRequestLookup.Found ? $"{PullRequestCell(lookup)} (p opens it)" : "none";

    private static string CountsLine(DetailFact<WorktreeCounts> counts, DateTimeOffset now) => counts switch
    {
        DetailFact<WorktreeCounts>.Known known => CountsText(known.Value, now),
        DetailFact<WorktreeCounts>.NotApplicable => "not in a repository",
        _ => Unavailable,
    };

    private static string CountsText(WorktreeCounts counts, DateTimeOffset now)
    {
        string last = counts.LastCommitAt is DateTimeOffset at ? $" · last commit {Durations.Age(now - at)} ago" : string.Empty;

        return $"{Count(counts.DirtyFiles, "changed file")} · {Count(counts.UnpushedCommits, "unpushed commit")}{last}";
    }

    private static string HealthLine(WorktreeHealth health)
    {
        string kind = health.Kind.ToString().ToLowerInvariant();

        return health.Reason is HealthReason.None ? kind : $"{kind}: {Describe(health.Reason)}";
    }

    private static string BranchLine(BranchName? branch, DetailFact<MergeState> merge) => (branch, merge) switch
    {
        (null, _) => "none",
        (_, DetailFact<MergeState>.Known known) => $"{branch.Value}, {(known.Value.Merged ? "merged" : "not merged")} into {known.Value.Into}",
        (_, DetailFact<MergeState>.Unavailable) => $"{branch.Value}, merge state unavailable",
        _ => branch.Value,
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