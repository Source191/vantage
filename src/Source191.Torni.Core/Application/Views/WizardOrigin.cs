using Source191.Torni.Application.Launch;
using Source191.Torni.Domain;

namespace Source191.Torni.Application.Views;

/// <summary>
/// How the new-session wizard was opened: anywhere (n, with the repository list and the worktree choice), or fixed on
/// one folder (n on a Worktrees row), where the session starts on that folder's current branch.
/// </summary>
public abstract record WizardOrigin
{
    /// <summary>Fixed on a folder, the choice named after the folder itself.</summary>
    public static WizardOrigin In(FolderPath folder) => new Fixed(new RepositoryChoice(folder.Name, folder, IsRepository: true));

    public sealed record Anywhere : WizardOrigin;

    public sealed record Fixed(RepositoryChoice Choice) : WizardOrigin;
}