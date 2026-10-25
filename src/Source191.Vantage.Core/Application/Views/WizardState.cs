using Source191.Vantage.Application.Launch;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Views;

/// <summary>
/// Everything the new-session wizard shows. The repositories are read once when it opens; the choices, the chosen
/// repository and the preview follow from the filter, the selection, the name and the mode, so they are never stale.
/// </summary>
public sealed record WizardState(
    IReadOnlyList<RepositoryChoice> AllChoices,
    RepositoryChoice? FixedChoice,
    IFolders Folders,
    string Filter,
    int? SelectedIndex,
    string Name,
    LaunchMode Mode,
    WorktreeBase Base,
    string Prompt,
    WizardProblem Problem,
    FolderPath WorktreeRoot,
    WizardField Focus)
{
    /// <summary>The wizard as it opens, with the worktree root and default base from the settings saved now. With a fixed directory (the worktrees view's s key) the session starts there: no repository list, no worktree choice.</summary>
    public static WizardState Open(RepositoryCatalogue catalogue, IReadOnlyList<string> recent, IFolders folders, Settings settings, FolderPath? fixedDirectory = null)
    {
        RepositoryChoice? fixedChoice = fixedDirectory is null ? null : new RepositoryChoice(fixedDirectory.Name, fixedDirectory, IsRepository: true);
        IReadOnlyList<RepositoryChoice> all = fixedChoice is null ? catalogue.Choices(recent, string.Empty) : [];

        return new WizardState(
            all,
            fixedChoice,
            folders,
            Filter: string.Empty,
            SelectedIndex: all.Count > 0 ? 0 : null,
            Name: string.Empty,
            LaunchMode.CurrentBranch,
            settings.DefaultBase,
            Prompt: string.Empty,
            Problem: new WizardProblem.None(),
            settings.WorktreeRoot,
            fixedChoice is null ? WizardField.RepositoryFilter : WizardField.Name);
    }

    /// <summary>False for a fixed directory: the filter, the list and the worktree choices are hidden.</summary>
    public bool ShowsChooser => FixedChoice is null;

    public IReadOnlyList<RepositoryChoice> Choices => [.. AllChoices.Where(choice => choice.Matches(Filter))];

    public RepositoryChoice? Chosen => FixedChoice ?? (SelectedIndex is int index ? Choices.ElementAtOrDefault(index) : null);

    /// <summary>What the name will become, or nothing while no repository is chosen.</summary>
    public string Preview => Chosen is null ? string.Empty : LaunchPreview.Describe(Chosen, Name, Mode, WorktreeRoot);
}