using Source191.Vantage.Application.Launch;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>
/// How the new-session wizard was opened: anywhere (n, with the repository list and the worktree choice), or fixed on
/// one folder (the worktrees view's s), where the session starts on that folder's current branch.
/// </summary>
public abstract record WizardOrigin
{
    /// <summary>Fixed on a folder, the choice named after the folder itself.</summary>
    public static WizardOrigin In(FolderPath folder)
    {
        ArgumentNullException.ThrowIfNull(folder);

        return new Fixed(new RepositoryChoice(folder.Name, folder, IsRepository: true));
    }

    public sealed record Anywhere : WizardOrigin;

    public sealed record Fixed(RepositoryChoice Choice) : WizardOrigin;
}

/// <summary>The repository the wizard would start in: the fixed one, the selected choice, or none while the list is empty.</summary>
public abstract record RepositoryPick
{
    public sealed record None : RepositoryPick;

    public sealed record Picked(RepositoryChoice Choice) : RepositoryPick;
}