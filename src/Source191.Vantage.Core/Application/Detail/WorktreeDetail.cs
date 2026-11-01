using Source191.Vantage.Application.Views;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Detail;

/// <summary>Everything the pane shows for a selected worktree beyond the row itself.</summary>
public sealed record WorktreeDetail(
    WorktreeRow Row,
    WorktreeOrigin Created,
    DetailFact<MergeState> Merge,
    DetailFact<IReadOnlyList<UnpushedCommit>> Commits,
    DetailFact<IReadOnlyList<string>> ChangedFiles);