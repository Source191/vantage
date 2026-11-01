using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

/// <summary>
/// Removes a worktree under the removal rules. The decision comes from the domain; this class adds the facts only it
/// can settle (did Vantage create the branch, is it merged, and for a leftover whether its folder exists and what it
/// holds now, read from the disc as the key is pressed rather than taken from the last scan) and hands the decision to
/// <see cref="WorktreeRemover"/>.
/// Not a dispatcher handler: the screen may ask about the branch afterwards, so it needs the outcome.
/// </summary>
public sealed class RemoveWorktreeHandler(IGit git, ICreatedWorktrees createdWorktrees, IFolders folders, CurrentSettings settings)
{
    private readonly WorktreeInspector inspector = new(git);
    private readonly WorktreeRemover remover = new(git, createdWorktrees, folders, settings);

    public async Task<RemoveWorktreeOutcome> RemoveAsync(WorktreeRow scanned, bool forced, CancellationToken cancellationToken)
    {
        WorktreeRow row = AsOnDisc(scanned);
        CreatedWorktree? created = (await createdWorktrees.ReadCreatedWorktreesAsync())
            .FirstOrDefault(candidate => candidate.Path == row.Path);
        // Merge state needs a branch and a repository that still exists.
        bool merged = created is not null && row is { Head: Head.OnBranch { Branch: BranchName branch }, OwnerExists: true } && await inspector.IsMergedAsync(row.Repository, branch, cancellationToken);
        RemovalDecision decision = RemovalDecision.For(row.Facts(created is not null, merged), forced);

        return await remover.CarryOutAsync(row, decision, createdByVantage: created is not null, cancellationToken);
    }

    /// <summary>A leftover row with its folder's facts read again; a live worktree is git's to judge, through git worktree remove.</summary>
    private WorktreeRow AsOnDisc(WorktreeRow row) => row.Health.Kind is WorktreeHealthKind.Orphan or WorktreeHealthKind.Broken or WorktreeHealthKind.Empty
        ? row with { FolderExists = folders.DirectoryExists(row.Path.Value), LeftoverFileCount = LeftoverFiles.Count(folders, row.Path) }
        : row;
}