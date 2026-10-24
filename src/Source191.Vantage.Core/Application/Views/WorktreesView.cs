using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

public sealed record WorktreeRowsLoaded(IReadOnlyList<WorktreeRow> Rows) : ViewInput;

/// <summary>What the user typed into the force-remove confirmation; null when they cancelled.</summary>
public sealed record ForceConfirmed(WorktreeRow Row, string ConfirmationText, string? Typed) : ViewInput;

/// <summary>Remove through git, or delete a leftover folder; the action re-checks the facts it alone can settle.</summary>
public sealed record RemoveWorktree(WorktreeRow Row, bool Forced) : ViewEffect, IDialogEffect;

/// <summary>The TUI asks the user to type the confirmation text, then raises RemoveWorktree with Forced.</summary>
public sealed record ConfirmForceRemove(WorktreeRow Row, string ConfirmationText) : ViewEffect, IDialogEffect;

public sealed record PruneRepository(string Repository) : ViewEffect;

public sealed record StartSessionIn(WorktreeRow Row) : ViewEffect, IDialogEffect;

public sealed record OpenShellTab(WorktreeRow Row) : ViewEffect;

/// <summary>Go to the sessions view with this session selected; by id, because names are not unique.</summary>
public sealed record JumpToSession(string SessionId) : ViewEffect, ILeavesScreen;

public sealed record SwitchToSessions : ViewEffect, ILeavesScreen;

public sealed record WorktreesViewUpdate(WorktreesViewState State, IReadOnlyList<ViewEffect> Effects);

public sealed record WorktreesViewState(
    IReadOnlyList<WorktreeRow> Rows,
    string? SelectedPath,
    RowFilter Filter,
    bool FilterEditing,
    string? Banner,
    Footer Footer,
    bool Loaded = false)
    : IListViewState<WorktreeRow>
{
    /// <summary>Nothing scanned yet: the first scan has not arrived, so the screen shows that it is scanning rather than an empty table.</summary>
    public static readonly WorktreesViewState Empty = new([], null, new RowFilter(string.Empty), FilterEditing: false, Banner: null, Footer: Footer.None);

    private static readonly ListPolicy<WorktreeRow> Policy = new(
        KeyOf: row => row.Path,
        FilterFields: row => [row.RepositoryName, row.FolderName, row.Branch, row.Health.Kind.ToString()],
        Order: rows => rows.OrderBy(row => row.Repository, StringComparer.OrdinalIgnoreCase).ThenBy(row => row.FolderName, StringComparer.OrdinalIgnoreCase),
        KeyComparison: StringComparison.OrdinalIgnoreCase);

    /// <summary>The rows, selection, filter and loaded flag as the shared list component, built from this state's fields.</summary>
    public ListSelection<WorktreeRow> List => new(Policy, Rows, SelectedPath, Filter, FilterEditing, Loaded);

    public IReadOnlyList<WorktreeRow> VisibleRows => List.Visible;

    public WorktreeRow? Selected => List.Selected;

    /// <summary>This state with the list component's fields taken from <paramref name="list"/>.</summary>
    public WorktreesViewState With(ListSelection<WorktreeRow> list)
    {
        return this with { Rows = list.Rows, SelectedPath = list.SelectedKey, Filter = list.Filter, FilterEditing = list.FilterEditing, Loaded = list.Loaded };
    }

    /// <summary>The footer to show: the last message, else why the selected row is not healthy, else the standing hint.</summary>
    public Footer Shown => (Footer, Selected) switch
    {
        (not Footer.Blank, _) => Footer,
        (_, { Health.Reason.Length: > 0 } selected) => new Footer.Unhealthy(selected.FolderName, selected.Health.Kind, selected.Health.Reason),
        _ => new Footer.RemoveHint(),
    };

    public string Title => List.Title("worktrees");
}

/// <summary>The worktrees view as a pure function of state and input, mirroring <see cref="SessionsView"/>.</summary>
public static class WorktreesView
{
    public static WorktreesViewUpdate Reduce(WorktreesViewState state, ViewInput input)
    {
        return input switch
        {
            WorktreeRowsLoaded loaded => Unchanged(state.With(state.List.Load(loaded.Rows)) with { Banner = null }),
            LoadFailed failed => Unchanged(state with { Banner = failed.Message }),
            ForceConfirmed confirmed => Force(state, confirmed),
            KeyPressed pressed when state.FilterEditing => Unchanged(state.With(state.List.EditFilter(pressed.Key))),
            KeyPressed pressed => Act(state, pressed.Key),
            _ => Unchanged(state),
        };
    }

    private static WorktreesViewUpdate Act(WorktreesViewState state, Key key)
    {
        return key switch
        {
            // Moving on clears the last message, so the footer can explain the row now under the bar.
            { Name: KeyName.Up } => Unchanged(state.With(state.List.Move(-1)) with { Footer = Footer.None }),
            { Name: KeyName.Down } => Unchanged(state.With(state.List.Move(+1)) with { Footer = Footer.None }),
            { Name: KeyName.Escape } when state.Filter.Text.Length > 0 => Unchanged(state.With(state.List.ClearFilter())),
            { Name: KeyName.Escape } => Effect(state, new SwitchToSessions()),
            { Name: KeyName.Enter } => RowAction(state, JumpToSession),
            { Name: KeyName.Character, Character: '/' } => Unchanged(state.With(state.List.StartFilter()) with { Footer = Footer.None }),
            { Name: KeyName.Character, Character: 'd' } => RowAction(state, row => Remove(row, forced: false)),
            { Name: KeyName.Character, Character: 'D' } => RowAction(state, row => Remove(row, forced: true)),
            { Name: KeyName.Character, Character: 'p' } => RowAction(state, Prune),
            { Name: KeyName.Character, Character: 's' } => RowAction(state, row => InRealWorktree(row, new StartSessionIn(row))),
            { Name: KeyName.Character, Character: 'o' } => RowAction(state, row => InRealWorktree(row, new OpenShellTab(row))),
            { Name: KeyName.Character, Character: 'w' } => Effect(state, new SwitchToSessions()),
            { Name: KeyName.Character, Character: 'q' } => Effect(state, new Quit()),
            { Name: KeyName.Character, Character: '?' } => Effect(state, new ShowHelp()),
            _ => Unchanged(state),
        };
    }

    /// <summary>Either an effect to run or a footer explaining why nothing happens.</summary>
    private static WorktreesViewUpdate RowAction(WorktreesViewState state, Func<WorktreeRow, (ViewEffect? Effect, Footer Footer)> decide)
    {
        WorktreeRow? row = state.Selected;
        if (row is null)
        {
            return Unchanged(state with { Footer = new Footer.NoWorktreeSelected() });
        }

        (ViewEffect? effect, Footer footer) = decide(row);

        return effect is null
            ? Unchanged(state with { Footer = footer })
            : Effect(state with { Footer = footer }, effect);
    }

    private static (ViewEffect?, Footer) Remove(WorktreeRow row, bool forced)
    {
        RemovalDecision decision = RemovalDecision.For(row.Facts(), forced);
        if (decision.Outcome == RemovalOutcome.Refused)
        {
            return (null, new Footer.NotRemoved(decision.Reason, OfferForce: !forced && decision.CanBeForced));
        }

        // The folder name is what the dialog says it deletes, so that is the text to type; a branch name would be a second thing to look up.
        return forced
            ? (new ConfirmForceRemove(row, row.FolderName), Footer.None)
            : (new RemoveWorktree(row, Forced: false), Footer.None);
    }

    /// <summary>The typed text must match exactly; anything else says what was typed so a near miss is obvious.</summary>
    private static WorktreesViewUpdate Force(WorktreesViewState state, ForceConfirmed confirmed)
    {
        return confirmed.Typed switch
        {
            null => Unchanged(state with { Footer = new Footer.ForceCancelled() }),
            "" => Unchanged(state with { Footer = new Footer.NothingTyped() }),
            string typed when !string.Equals(typed, confirmed.ConfirmationText, StringComparison.Ordinal) =>
                Unchanged(state with { Footer = new Footer.TypedWrongName(typed, confirmed.ConfirmationText) }),
            _ => Forced(state, new RemoveWorktree(confirmed.Row, Forced: true)),
        };
    }

    private static WorktreesViewUpdate Forced(WorktreesViewState state, RemoveWorktree remove)
    {
        return Effect(state with { Footer = new Footer.Working(remove) }, remove);
    }

    private static (ViewEffect?, Footer) Prune(WorktreeRow row)
    {
        return row.OwnerExists
            ? (new PruneRepository(row.Repository), Footer.None)
            : (null, new Footer.NothingToPrune(row.RepositoryName));
    }

    private static (ViewEffect?, Footer) InRealWorktree(WorktreeRow row, ViewEffect effect)
    {
        return row.Health.Kind is WorktreeHealthKind.Ok or WorktreeHealthKind.Locked && row.FolderExists
            ? (effect, Footer.None)
            : (null, new Footer.LeftoverFolder(row.FolderName));
    }

    private static (ViewEffect?, Footer) JumpToSession(WorktreeRow row)
    {
        return row.SessionId is null
            ? (null, new Footer.NoSessionIn(row.FolderName))
            : (new JumpToSession(row.SessionId), Footer.None);
    }

    private static WorktreesViewUpdate Unchanged(WorktreesViewState state)
    {
        return new WorktreesViewUpdate(state, []);
    }

    private static WorktreesViewUpdate Effect(WorktreesViewState state, ViewEffect effect)
    {
        return new WorktreesViewUpdate(state, [effect]);
    }
}