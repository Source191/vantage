using Source191.Vantage.Application.Launch;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using System.Diagnostics;

namespace Source191.Vantage.Application.Views;

/// <summary>
/// Everything the new-session wizard shows. The repositories are read once when it opens; the choices, the chosen
/// repository and the preview follow from the filter, the selection, the name and the mode, so they are never stale.
/// </summary>
public sealed record WizardState(
    IReadOnlyList<RepositoryChoice> AllChoices,
    WizardOrigin Origin,
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
    /// <summary>The wizard as it opens, with the default place, the worktree root and the default base from the settings saved now. Fixed on a folder (the worktrees view's s key) the session starts there on its current branch: no repository list, no worktree choice.</summary>
    public static WizardState Open(RepositoryCatalogue catalogue, IReadOnlyList<string> recent, IFolders folders, Settings settings, WizardOrigin origin)
    {
        bool anywhere = origin is WizardOrigin.Anywhere;
        IReadOnlyList<RepositoryChoice> all = anywhere ? catalogue.Choices(recent, string.Empty) : [];

        return new WizardState(
            all,
            origin,
            folders,
            Filter: string.Empty,
            SelectedIndex: all.Count > 0 ? 0 : null,
            Name: string.Empty,
            anywhere ? settings.DefaultPlace : LaunchMode.CurrentBranch,
            settings.DefaultBase,
            Prompt: string.Empty,
            Problem: new WizardProblem.None(),
            settings.WorktreeRoot,
            anywhere ? WizardField.RepositoryFilter : WizardField.Name);
    }

    /// <summary>False for a fixed directory: the filter, the list and the worktree choices are hidden.</summary>
    public bool ShowsChooser => Origin is WizardOrigin.Anywhere;

    public IReadOnlyList<RepositoryChoice> Choices => [.. AllChoices.Where(choice => choice.Matches(Filter))];

    public RepositoryPick Chosen => Origin switch
    {
        WizardOrigin.Fixed pinned => new RepositoryPick.Picked(pinned.Choice),
        WizardOrigin.Anywhere => SelectedIndex is int index && Choices.ElementAtOrDefault(index) is RepositoryChoice selected ? new RepositoryPick.Picked(selected) : new RepositoryPick.None(),
        _ => throw new UnreachableException(),
    };

    /// <summary>What the name will become, or nothing while no repository is chosen.</summary>
    public string Preview => Chosen is RepositoryPick.Picked picked ? LaunchPreview.Describe(picked.Choice, Name, Mode, WorktreeRoot) : string.Empty;
}