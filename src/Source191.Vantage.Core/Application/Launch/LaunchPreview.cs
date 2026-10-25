using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Launch;

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
            return $"Runs in {repository.Name} on its current branch; the name is only the session's label.";
        }

        if (!repository.IsRepository)
        {
            return "A worktree needs a repository; this folder is not one.";
        }

        if (!WorktreeSlug.TryParse(nameText, out WorktreeSlug? slug))
        {
            return $"The name becomes the worktree folder {repository.Name}-<name> under {worktreeRoot} and its branch.";
        }

        PlannedWorktree worktree = PlannedWorktree.For(worktreeRoot, repository.Path, slug);

        return $"Worktree {worktree.Path.Name} under {worktreeRoot}, on branch {worktree.Branch}.";
    }
}