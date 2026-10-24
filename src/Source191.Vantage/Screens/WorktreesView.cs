using Source191.Vantage.Domain;

namespace Source191.Vantage.Screens;

public sealed record WorktreeRowsLoaded(IReadOnlyList<WorktreeRow> Rows) : ViewInput;

/// <summary>What the user typed into the force-remove confirmation; null when they cancelled.</summary>
public sealed record ForceConfirmed(WorktreeRow Row, string ConfirmationText, string? Typed) : ViewInput;

/// <summary>Remove through git, or delete a leftover folder; the action re-checks the facts it alone can settle.</summary>
public sealed record RemoveWorktree(WorktreeRow Row, bool Forced) : ViewEffect;

/// <summary>The TUI asks the user to type the confirmation text, then raises RemoveWorktree with Forced.</summary>
public sealed record ConfirmForceRemove(WorktreeRow Row, string ConfirmationText) : ViewEffect;

public sealed record PruneRepository(string Repository) : ViewEffect;

public sealed record StartSessionIn(WorktreeRow Row) : ViewEffect;

public sealed record OpenShellTab(WorktreeRow Row) : ViewEffect;

public sealed record JumpToSession(string SessionName) : ViewEffect;

public sealed record SwitchToSessions : ViewEffect;

public sealed record WorktreesViewUpdate(WorktreesViewState State, IReadOnlyList<ViewEffect> Effects);

public sealed record WorktreesViewState(
    IReadOnlyList<WorktreeRow> Rows,
    string? SelectedPath,
    RowFilter Filter,
    bool FilterEditing,
    string? Banner,
    string Footer,
    bool Loaded = false)
{
    /// <summary>Nothing scanned yet: the first scan has not arrived, so the screen shows that it is scanning rather than an empty table.</summary>
    public static readonly WorktreesViewState Empty = new([], null, new RowFilter(string.Empty), FilterEditing: false, Banner: null, Footer: string.Empty);

    public IReadOnlyList<WorktreeRow> VisibleRows =>
    [
        .. Rows
            .Where(Matches)
            .OrderBy(row => row.Repository, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.FolderName, StringComparer.OrdinalIgnoreCase),
    ];

    public WorktreeRow? Selected => VisibleRows.FirstOrDefault(row => string.Equals(row.Path, SelectedPath, StringComparison.OrdinalIgnoreCase));

    /// <summary>The footer to show: the last message, else why the selected row is not healthy, else the standing hint.</summary>
    public string FooterText
    {
        get
        {
            if (Footer.Length > 0)
            {
                return Footer;
            }

            WorktreeRow? selected = Selected;

            return selected is null || selected.Health.Reason.Length == 0
                ? "d removes only what is safe to remove"
                : $"{selected.FolderName} is {selected.Health.Kind.ToString().ToLowerInvariant()}: {selected.Health.Reason}.";
        }
    }

    public string Title
    {
        get
        {
            string scope = string.IsNullOrWhiteSpace(Filter.Text) ? "all" : Filter.Text;

            return $"worktrees({scope})[{VisibleRows.Count}]";
        }
    }

    private bool Matches(WorktreeRow row)
    {
        return Filter.Matches([row.RepositoryName, row.FolderName, row.Branch, row.Health.Kind.ToString()]);
    }
}

/// <summary>The worktrees view as a pure function of state and input, mirroring <see cref="SessionsView"/>.</summary>
public static class WorktreesView
{
    private const string ForceHint = " D forces after a typed confirmation.";

    public static WorktreesViewUpdate Reduce(WorktreesViewState state, ViewInput input)
    {
        return input switch
        {
            WorktreeRowsLoaded loaded => Unchanged(Load(state, loaded.Rows)),
            LoadFailed failed => Unchanged(state with { Banner = failed.Message }),
            ForceConfirmed confirmed => Force(state, confirmed),
            KeyPressed pressed when state.FilterEditing => Unchanged(EditFilter(state, pressed.Key)),
            KeyPressed pressed => Act(state, pressed.Key),
            _ => Unchanged(state),
        };
    }

    private static WorktreesViewState Load(WorktreesViewState state, IReadOnlyList<WorktreeRow> rows)
    {
        int previousIndex = IndexOfSelected(state);
        WorktreesViewState loaded = state with { Rows = rows, Banner = null, Loaded = true };
        if (loaded.Selected is not null)
        {
            return loaded;
        }

        IReadOnlyList<WorktreeRow> visible = loaded.VisibleRows;
        if (visible.Count == 0)
        {
            return loaded with { SelectedPath = null };
        }

        int nearest = Math.Clamp(previousIndex, 0, visible.Count - 1);

        return loaded with { SelectedPath = visible[nearest].Path };
    }

    private static WorktreesViewState EditFilter(WorktreesViewState state, Key key)
    {
        return key.Name switch
        {
            KeyName.Enter => state with { FilterEditing = false },
            KeyName.Escape => Reselect(state with { FilterEditing = false, Filter = new RowFilter(string.Empty) }),
            KeyName.Backspace => Reselect(state with { Filter = new RowFilter(state.Filter.Text[..Math.Max(0, state.Filter.Text.Length - 1)]) }),
            KeyName.Character => Reselect(state with { Filter = new RowFilter(state.Filter.Text + key.Character) }),
            _ => state,
        };
    }

    private static WorktreesViewUpdate Act(WorktreesViewState state, Key key)
    {
        return key switch
        {
            { Name: KeyName.Up } => Unchanged(MoveSelection(state, -1)),
            { Name: KeyName.Down } => Unchanged(MoveSelection(state, +1)),
            { Name: KeyName.Escape } when state.Filter.Text.Length > 0 => Unchanged(Reselect(state with { Filter = new RowFilter(string.Empty) })),
            { Name: KeyName.Escape } => Effect(state, new SwitchToSessions()),
            { Name: KeyName.Enter } => RowAction(state, JumpToSession),
            { Name: KeyName.Character, Character: '/' } => Unchanged(state with { FilterEditing = true, Filter = new RowFilter(string.Empty), Footer = string.Empty }),
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
    private static WorktreesViewUpdate RowAction(WorktreesViewState state, Func<WorktreeRow, (ViewEffect? Effect, string Footer)> decide)
    {
        WorktreeRow? row = state.Selected;
        if (row is null)
        {
            return Unchanged(state with { Footer = "No worktree selected." });
        }

        (ViewEffect? effect, string footer) = decide(row);

        return effect is null
            ? Unchanged(state with { Footer = footer })
            : Effect(state with { Footer = footer }, effect);
    }

    private static (ViewEffect?, string) Remove(WorktreeRow row, bool forced)
    {
        RemovalDecision decision = RemovalDecision.For(row.Facts(), forced);
        if (decision.Outcome == RemovalOutcome.Refused)
        {
            string hint = forced || !decision.CanBeForced ? string.Empty : ForceHint;

            return (null, $"Not removed: {decision.Reason}.{hint}");
        }

        // The folder name is what the dialog says it deletes, so that is the text to type; a branch name would be a second thing to look up.
        return forced
            ? (new ConfirmForceRemove(row, row.FolderName), string.Empty)
            : (new RemoveWorktree(row, Forced: false), string.Empty);
    }

    /// <summary>The typed text must match exactly; anything else says what was typed so a near miss is obvious.</summary>
    private static WorktreesViewUpdate Force(WorktreesViewState state, ForceConfirmed confirmed)
    {
        return confirmed.Typed switch
        {
            null => Unchanged(state with { Footer = "Cancelled; nothing removed." }),
            "" => Unchanged(state with { Footer = "Nothing was typed; nothing removed." }),
            string typed when !string.Equals(typed, confirmed.ConfirmationText, StringComparison.Ordinal) =>
                Unchanged(state with { Footer = $"You typed {typed}, not {confirmed.ConfirmationText}; nothing removed." }),
            _ => Effect(state with { Footer = $"Removing {confirmed.Row.FolderName}..." }, new RemoveWorktree(confirmed.Row, Forced: true)),
        };
    }

    private static (ViewEffect?, string) Prune(WorktreeRow row)
    {
        return row.OwnerExists
            ? (new PruneRepository(row.Repository), string.Empty)
            : (null, $"Nothing to prune: the repository {row.RepositoryName} no longer exists.");
    }

    private static (ViewEffect?, string) InRealWorktree(WorktreeRow row, ViewEffect effect)
    {
        return row.Health.Kind is WorktreeHealthKind.Ok or WorktreeHealthKind.Locked && row.FolderExists
            ? (effect, string.Empty)
            : (null, $"{row.FolderName} is a leftover folder, not a worktree.");
    }

    private static (ViewEffect?, string) JumpToSession(WorktreeRow row)
    {
        return row.SessionName is null
            ? (null, $"No session is running in {row.FolderName}.")
            : (new JumpToSession(row.SessionName), string.Empty);
    }

    private static WorktreesViewState MoveSelection(WorktreesViewState state, int delta)
    {
        IReadOnlyList<WorktreeRow> visible = state.VisibleRows;
        if (visible.Count == 0)
        {
            return state;
        }

        int index = Math.Clamp(IndexOfSelected(state) + delta, 0, visible.Count - 1);

        // Moving on clears the last message, so the footer can explain the row now under the bar.
        return state with { SelectedPath = visible[index].Path, Footer = string.Empty };
    }

    private static WorktreesViewState Reselect(WorktreesViewState state)
    {
        return state.Selected is not null ? state : Load(state, state.Rows);
    }

    private static int IndexOfSelected(WorktreesViewState state)
    {
        int index = state.VisibleRows.ToList().FindIndex(row => string.Equals(row.Path, state.SelectedPath, StringComparison.OrdinalIgnoreCase));

        return Math.Max(index, 0);
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