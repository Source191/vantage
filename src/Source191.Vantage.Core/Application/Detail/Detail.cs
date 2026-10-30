using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Detail;

/// <summary>One fact of a row's detail, read on its own: known, not applicable to this row, or unavailable because reading it failed.</summary>
public abstract record DetailFact<T>
{
    public sealed record Known(T Value) : DetailFact<T>;

    /// <summary>The row has nothing of this kind, such as git counts for a folder outside a repository.</summary>
    public sealed record NotApplicable : DetailFact<T>;

    public sealed record Unavailable : DetailFact<T>;
}

/// <summary>The bottom of a session's screen, or why there is none.</summary>
public abstract record SessionOutput
{
    public sealed record Screen(IReadOnlyList<string> Rows) : SessionOutput;

    /// <summary>A stopped or completed session has no process, so there is no screen to read.</summary>
    public sealed record NoProcess : SessionOutput;

    /// <summary>A plain claude in its own terminal: only the supervisor's jobs have logs.</summary>
    public sealed record NotSupervised : SessionOutput;

    public sealed record Unavailable : SessionOutput;

    /// <summary>Not read this time (the output is read every other refresh); the pane keeps what it showed for the same session.</summary>
    public sealed record NotRead : SessionOutput;
}

/// <summary>Whether a branch is merged, and into what (origin's default branch, or HEAD when the repository has none).</summary>
public sealed record MergeState(string Into, bool Merged);

/// <summary>Everything the pane shows for a selected session beyond the row itself.</summary>
public sealed record SessionDetail(SessionRow Row, DateTimeOffset? LastPromptedAt, DetailFact<WorktreeCounts> Counts, SessionOutput Output);

/// <summary>Everything the pane shows for a selected worktree beyond the row itself.</summary>
public sealed record WorktreeDetail(
    WorktreeRow Row,
    CreatedWorktree? Created,
    DetailFact<MergeState> Merge,
    DetailFact<IReadOnlyList<UnpushedCommit>> Commits,
    DetailFact<IReadOnlyList<string>> ChangedFiles);