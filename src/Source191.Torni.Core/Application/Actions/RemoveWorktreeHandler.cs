using Source191.Torni.Application.Views;
using Source191.Torni.Domain;
using Source191.Torni.Ports;

namespace Source191.Torni.Application.Actions;

/// <summary>
/// Removes a worktree under the removal rules. The decision comes from the domain; this class adds the facts only it
/// can settle (did Torni create the branch, is it merged, and for a leftover whether its folder exists and what it
/// holds now, read from the disc as the key is pressed rather than taken from the last scan) and hands the decision to
/// <see cref="WorktreeRemover"/>.
/// Not a dispatcher handler: the screen may ask about the branch afterwards, so it needs the outcome.
/// </summary>
public sealed class RemoveWorktreeHandler(IGit git, ICreatedWorktrees createdWorktrees, IFolders folders, WorktreeRemover remover)
{
    private readonly WorktreeInspector inspector = new(git);

    public async Task<RemoveWorktreeOutcome> RemoveAsync(WorktreeRow scanned, bool forced, CancellationToken cancellationToken)
    {
        WorktreeRow row = AsOnDisc(scanned);
        CreatedWorktree? created = (await createdWorktrees.ReadCreatedWorktreesAsync())
            .FirstOrDefault(candidate => candidate.Path == row.Path);
        // Merge state needs a branch and a repository that still exists.
        bool merged = created is not null && row is { Head: Head.OnBranch { Branch: BranchName branch }, OwnerExists: true } && await inspector.IsMergedAsync(row.Repository, branch, cancellationToken);
        RemovalDecision decision = RemovalDecision.For(row.Facts(created is not null, merged), forced);

        return await remover.CarryOutAsync(row, decision, createdHere: created is not null, cancellationToken);
    }

    /// <summary>A leftover row with its folder's facts read again; a live worktree is git's to judge, through git worktree remove.</summary>
    private WorktreeRow AsOnDisc(WorktreeRow row) => row.Health.Kind is WorktreeHealthKind.Orphan or WorktreeHealthKind.Broken or WorktreeHealthKind.Empty
        ? row with { FolderExists = folders.DirectoryExists(row.Path.Value), LeftoverFileCount = LeftoverFiles.Count(folders, row.Path) }
        : row;
}