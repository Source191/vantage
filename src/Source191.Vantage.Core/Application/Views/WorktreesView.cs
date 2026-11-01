using Source191.Vantage.Domain;
using System.Diagnostics;

namespace Source191.Vantage.Application.Views;

public sealed record WorktreeRowsLoaded(IReadOnlyList<WorktreeRow> Rows) : ViewInput;

/// <summary>What the user typed into the force-remove confirmation.</summary>
public sealed record ForceConfirmed(WorktreeRow Row, string ConfirmationText, string Typed) : ViewInput;

/// <summary>The force-remove confirmation was closed without an answer.</summary>
public sealed record ForceCancelled(WorktreeRow Row) : ViewInput;

/// <summary>Remove through git, or delete a leftover folder; the action re-checks the facts it alone can settle.</summary>
public sealed record RemoveWorktree(WorktreeRow Row, bool Forced) : ViewEffect, IDialogEffect;

/// <summary>The TUI asks the user to type the confirmation text, then raises RemoveWorktree with Forced.</summary>
public sealed record ConfirmForceRemove(WorktreeRow Row, string ConfirmationText) : ViewEffect, IDialogEffect;

public sealed record PruneRepository(FolderPath Repository) : ViewEffect;

public sealed record StartSessionIn(WorktreeRow Row) : ViewEffect, IDialogEffect;

public sealed record OpenShellTab(WorktreeRow Row) : ViewEffect;

/// <summary>Enter on a worktree: open its session's tab as the Sessions tab's Enter does; by id, because names are not unique, and the name for when it is not listed.</summary>
public sealed record OpenWorktreeSession(SessionId SessionId, string Name) : ViewEffect, IConsultsSessions;


public sealed record WorktreesViewUpdate(WorktreesViewState State, IReadOnlyList<ViewEffect> Effects);

public sealed record WorktreesViewState(
    IReadOnlyList<WorktreeRow> Rows,
    SelectionKey SelectedPath,
    RowFilter Filter,
    bool FilterEditing,
    LoadBanner Banner,
    StatusMessage Message,
    bool Loaded = false)
    : IListViewState<WorktreeRow>
{
    /// <summary>Nothing scanned yet: the first scan has not arrived, so the screen shows that it is scanning rather than an empty table.</summary>
    public static readonly WorktreesViewState Empty = new([], new SelectionKey.None(), new RowFilter(string.Empty), FilterEditing: false, Banner: new LoadBanner.None(), Message: StatusMessage.None);

    private static readonly ListPolicy<WorktreeRow> Policy = new(
        KeyOf: row => row.Path.Key,
        FilterFields: row => [row.RepositoryName, row.FolderName, .. FilterText.Of(row.Head), row.Health.Kind.ToString()],
        Order: rows => rows.OrderBy(row => row.Repository.Key, StringComparer.Ordinal).ThenBy(row => row.FolderName, StringComparer.OrdinalIgnoreCase),
        KeyComparison: StringComparison.Ordinal,
        Selectable: _ => true);

    /// <summary>The rows, selection, filter and loaded flag as the shared list component, built from this state's fields.</summary>
    public ListSelection<WorktreeRow> List => Policy.List(Rows, SelectedPath, Filter, FilterEditing, Loaded);

    public IReadOnlyList<WorktreeRow> VisibleRows => List.Visible;

    public WorktreeRow? Selected => List.Selected;

    /// <summary>This state with the list component's fields taken from <paramref name="list"/>.</summary>
    public WorktreesViewState With(ListSelection<WorktreeRow> list) => this with { Rows = list.Rows, SelectedPath = list.Selection, Filter = list.Filter, FilterEditing = list.FilterEditing, Loaded = list.Loaded };

    /// <summary>The last message only: the selected row's health is the detail pane's.</summary>
    public StatusMessage Shown => Message;

    public TableTitle Title => List.Title("worktree", row => row.FolderName);
}

/// <summary>The worktrees view as a pure function of state and input, mirroring <see cref="SessionsView"/>.</summary>
public static class WorktreesView
{
    public static WorktreesViewUpdate Reduce(WorktreesViewState state, ViewInput input)
    {
        return input switch
        {
            WorktreeRowsLoaded loaded => Unchanged(state.With(state.List.Load(loaded.Rows)) with { Banner = new LoadBanner.None() }),
            LoadFailed failed => Unchanged(state with { Banner = new LoadBanner.Failed(failed.Message) }),
            MessageDismissed dismissed when ReferenceEquals(state.Message, dismissed.Message) => Unchanged(state with { Message = StatusMessage.None }),
            MessageDismissed => Unchanged(state),
            ForceConfirmed confirmed => Force(state, confirmed),
            ForceCancelled => Unchanged(state with { Message = new StatusMessage.ForceCancelled() }),
            UpdateTabOpened => Effect(state, new Quit()),
            KeyPressed pressed when state.FilterEditing => Unchanged(state.With(state.List.EditFilter(pressed.Key)) with { Message = StatusMessage.None }),
            KeyPressed pressed => Act(state, pressed.Key),
            _ => Unchanged(state),
        };
    }

    private static WorktreesViewUpdate Act(WorktreesViewState state, Key key)
    {
        return key switch
        {
            // Moving on or filtering clears the last message, so the status line goes until there is something new to say.
            { Name: KeyName.Up } => Unchanged(state.With(state.List.Move(-1)) with { Message = StatusMessage.None }),
            { Name: KeyName.Down } => Unchanged(state.With(state.List.Move(+1)) with { Message = StatusMessage.None }),
            { Name: KeyName.Escape } when state.Filter.Text.Length > 0 => Unchanged(state.With(state.List.ClearFilter())),
            { Name: KeyName.Escape } => Effect(state, new SelectTab(ConsoleTab.Sessions)),
            { Name: KeyName.Enter } => RowAction(state, OpenSession),
            { Name: KeyName.Character, Character: '/' } => Unchanged(state.With(state.List.StartFilter()) with { Message = StatusMessage.None }),
            { Name: KeyName.Character, Character: 'd' } => RowAction(state, row => Remove(row, forced: false)),
            { Name: KeyName.Character, Character: 'D' } => RowAction(state, row => Remove(row, forced: true)),
            { Name: KeyName.Character, Character: 'P' } => RowAction(state, Prune),
            { Name: KeyName.Character, Character: 'p' } => RowAction(state, PullRequestOf),
            { Name: KeyName.Character, Character: 'n' } => RowAction(state, row => InRealWorktree(row, new StartSessionIn(row))),
            { Name: KeyName.Character, Character: 'o' } => RowAction(state, row => InRealWorktree(row, new OpenShellTab(row))),
            { Name: KeyName.Character, Character: 'c' } => RowAction(state, row => WithFolder(row, new CopyFolderPath(row.Path))),
            { Name: KeyName.Character, Character: 'f' } => RowAction(state, row => WithFolder(row, new OpenFolderInEditor(row.Path))),
            { Name: KeyName.Character, Character: 's' } => RowAction(state, row => WithFolder(row, new FindSolution(row.Path, row.RepositoryName))),
            { Name: KeyName.Character, Character: 'q' } => Effect(state, new Quit()),
            { Name: KeyName.Character, Character: '?' } => Effect(state, new ShowHelp()),
            { Name: KeyName.Character, Character: 'U' } => Effect(state, new UpdateVantage()),
            { Name: KeyName.Character, Character: '!' } => Effect(state, new ShowProblemReport()),
            _ when TabKeys.TryFor(key, out ViewEffect? tab) => Effect(state, tab),
            _ => Unchanged(state),
        };
    }

    /// <summary>Either an effect to run or a status message explaining why nothing happens.</summary>
    private static WorktreesViewUpdate RowAction(WorktreesViewState state, Func<WorktreeRow, RowDecision> decide)
    {
        WorktreeRow? row = state.Selected;
        if (row is null)
        {
            return Unchanged(state with { Message = new StatusMessage.NoWorktreeSelected() });
        }

        return decide(row) switch
        {
            RowDecision.Act act => Effect(state with { Message = StatusMessage.None }, act.Effect),
            RowDecision.Explain explain => Unchanged(state with { Message = explain.Message }),
            _ => throw new UnreachableException(),
        };
    }

    private static RowDecision Remove(WorktreeRow row, bool forced)
    {
        RemovalDecision decision = RemovalDecision.For(row.Facts(), forced);
        if (decision.Outcome == RemovalOutcome.Refused)
        {
            return new RowDecision.Explain(new StatusMessage.NotRemoved(decision.Reason, OfferForce: !forced && decision.CanBeForced));
        }

        // The folder name is what the dialog says it deletes, so that is the text to type; a branch name would be a second thing to look up.
        return forced
            ? new RowDecision.Act(new ConfirmForceRemove(row, row.FolderName))
            : new RowDecision.Act(new RemoveWorktree(row, Forced: false));
    }

    /// <summary>The typed text must match exactly; anything else says what was typed so a near miss is obvious.</summary>
    private static WorktreesViewUpdate Force(WorktreesViewState state, ForceConfirmed confirmed)
    {
        return confirmed.Typed switch
        {
            "" => Unchanged(state with { Message = new StatusMessage.NothingTyped() }),
            string typed when !string.Equals(typed, confirmed.ConfirmationText, StringComparison.Ordinal) =>
                Unchanged(state with { Message = new StatusMessage.TypedWrongName(typed, confirmed.ConfirmationText) }),
            _ => Forced(state, new RemoveWorktree(confirmed.Row, Forced: true)),
        };
    }

    private static WorktreesViewUpdate Forced(WorktreesViewState state, RemoveWorktree remove) => Effect(state with { Message = new StatusMessage.Working(remove) }, remove);

    private static RowDecision PullRequestOf(WorktreeRow row) => row.PullRequest switch
    {
        PullRequestLookup.Found found => new RowDecision.Act(new OpenPullRequest(found.PullRequest)),
        PullRequestLookup.Missing missing => new RowDecision.Explain(new StatusMessage.NoPullRequest(row.FolderName, missing.Gap, missing.Host)),
        _ => new RowDecision.Explain(StatusMessage.None),
    };

    private static RowDecision Prune(WorktreeRow row)
    {
        return row.OwnerExists
            ? new RowDecision.Act(new PruneRepository(row.Repository))
            : new RowDecision.Explain(new StatusMessage.NothingToPrune(row.RepositoryName));
    }

    /// <summary>A real worktree is one git still has: ok, locked, or elsewhere (registered with a repository outside the roots).</summary>
    private static RowDecision InRealWorktree(WorktreeRow row, ViewEffect effect)
    {
        return row.Health.Kind is WorktreeHealthKind.Ok or WorktreeHealthKind.Locked or WorktreeHealthKind.Elsewhere && row.FolderExists
            ? new RowDecision.Act(effect)
            : new RowDecision.Explain(new StatusMessage.LeftoverFolder(row.FolderName));
    }

    /// <summary>The folder keys need a folder on disc; a leftover whose folder is gone is refused as the start and shell keys refuse it.</summary>
    private static RowDecision WithFolder(WorktreeRow row, ViewEffect effect)
    {
        return row.FolderExists
            ? new RowDecision.Act(effect)
            : new RowDecision.Explain(new StatusMessage.LeftoverFolder(row.FolderName));
    }

    private static RowDecision OpenSession(WorktreeRow row)
    {
        return row.Session is WorktreeSession.Running running
            ? new RowDecision.Act(new OpenWorktreeSession(running.Id, running.Name))
            : new RowDecision.Explain(new StatusMessage.NoSessionIn(row.FolderName));
    }

    private static WorktreesViewUpdate Unchanged(WorktreesViewState state) => new(state, []);

    private static WorktreesViewUpdate Effect(WorktreesViewState state, ViewEffect effect) => new(state, [effect]);
}