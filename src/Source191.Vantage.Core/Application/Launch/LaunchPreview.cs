using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Launch;

/// <summary>
/// One sentence telling the wizard's user what the name they are typing will become, so "session name" and
/// "branch name" are never confused: the name is the label, and with a worktree it also names the folder and branch.
/// </summary>
public static class LaunchPreview
{
    public static string Describe(RepositoryChoice repository, string nameText, LaunchMode mode)
    {
        if (mode == LaunchMode.CurrentBranch)
        {
            return $"Runs in {repository.Name} on its current branch; the name is only the session's label.";
        }

        if (!repository.IsRepository)
        {
            return "A worktree needs a repository; this folder is not one.";
        }

        WorktreeSlug? slug = WorktreeSlug.From(nameText);
        if (slug is null)
        {
            return $"The name becomes the worktree folder {repository.Name}-<name> and its branch.";
        }

        SiblingWorktree worktree = SiblingWorktree.For(new FolderPath(repository.Path), slug);

        return $"Worktree {Path.GetFileName(worktree.Path)} beside the repository, on branch {worktree.Branch}.";
    }
}