using Source191.Torni.Domain;

namespace Source191.Torni.Application.Launch;

/// <summary>
/// One sentence telling the wizard's user what the name they are typing will become, so "session name" and
/// "branch name" are never confused: the name is the label, and with a worktree it also names the folder and branch.
/// </summary>
public static class LaunchPreview
{
    public static string Describe(RepositoryChoice repository, string nameText, LaunchMode mode, FolderPath worktreeRoot)
    {
        if (mode == LaunchMode.CurrentBranch)
        {
            return Wording.PreviewCurrentBranch(repository.Name);
        }

        if (!repository.IsRepository)
        {
            return Wording.PreviewNeedsRepository;
        }

        if (!WorktreeSlug.TryParse(nameText, out WorktreeSlug? slug))
        {
            return Wording.PreviewBeforeAName(repository.Name, worktreeRoot);
        }

        PlannedWorktree worktree = PlannedWorktree.For(worktreeRoot, repository.Path, slug);

        return Wording.PreviewWorktree(worktree.Path.Name, worktreeRoot, worktree.Branch);
    }
}