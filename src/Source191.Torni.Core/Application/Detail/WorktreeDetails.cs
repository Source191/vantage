using Source191.Torni.Application.Views;

namespace Source191.Torni.Application.Detail;

/// <summary>The Worktrees tab's pane: a worktree's facts and up to five of its unpushed commits or changed files, keyed by its folder.</summary>
public sealed class WorktreeDetails(DetailLoader loader, TimeProvider clock) : IDetailSource<WorktreeRow>
{
    /// <summary>The facts, the listed items and the line saying how many more.</summary>
    public int Rows => Wording.DetailFactRows + Wording.DetailListedItems + 1;

    public object KeyOf(WorktreeRow row) => row.Path;

    public async Task<DetailContent> ReadAsync(WorktreeRow row, CancellationToken cancellationToken) =>
        Wording.Detail(await loader.WorktreeAsync(row, Wording.DetailListedItems, cancellationToken), clock.GetUtcNow(), clock.LocalTimeZone);
}