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

/// <summary>Whether a branch is merged, and into what (origin's default branch, or HEAD when the repository has none).</summary>
public sealed record MergeState(string Into, bool Merged);

/// <summary>Everything the pane shows for a selected session beyond the row itself.</summary>
public sealed record SessionDetail(SessionRow Row, LastPrompt LastPromptedAt, DetailFact<WorktreeCounts> Counts);

/// <summary>Everything the pane shows for a selected worktree beyond the row itself.</summary>
public sealed record WorktreeDetail(
    WorktreeRow Row,
    CreatedWorktree? Created,
    DetailFact<MergeState> Merge,
    DetailFact<IReadOnlyList<UnpushedCommit>> Commits,
    DetailFact<IReadOnlyList<string>> ChangedFiles);