using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>A listed session with what git, the state store and the transcript folder add to it. HasTranscript is false for a session never sent a message: Claude writes nothing to resume from until then, so every caller must say which it is.</summary>
public sealed record SessionRow(Session Session, string Repository, BranchName? Branch, bool IsWorktree, string? Description, bool HasLiveTab, bool HasTranscript);

/// <summary>A key as the reducer sees it, independent of any terminal library.</summary>
public sealed record Key(KeyName Name, char Character)
{
    public static readonly Key Enter = new(KeyName.Enter, '\0');
    public static readonly Key Escape = new(KeyName.Escape, '\0');
    public static readonly Key Up = new(KeyName.Up, '\0');
    public static readonly Key Down = new(KeyName.Down, '\0');
    public static readonly Key Backspace = new(KeyName.Backspace, '\0');
    public static readonly Key Slash = new(KeyName.Character, '/');

    public static Key Of(char character) => new Key(KeyName.Character, character);
}

public enum KeyName
{
    Character,
    Enter,
    Escape,
    Up,
    Down,
    Backspace,
}

public abstract record ViewInput;

public sealed record RowsLoaded(IReadOnlyList<SessionRow> Rows) : ViewInput;

public sealed record LoadFailed(string Message) : ViewInput;

public sealed record KeyPressed(Key Key) : ViewInput;

/// <summary>Select a session by its id, as the worktrees view's Enter asks; a filter hiding it is cleared so it shows.</summary>
public sealed record SelectSession(SessionId SessionId) : ViewInput;

public abstract record ViewEffect;

public sealed record AttachToSession(JobId JobId) : ViewEffect, ILeavesScreen;

/// <summary>The TUI asks for a new name, then raises <see cref="RenameConfirmed"/> with what was typed (null when cancelled).</summary>
public sealed record AskForNewName(SessionRow Row) : ViewEffect, IDialogEffect;

public sealed record RenameConfirmed(SessionRow Row, string? Name) : ViewInput;

/// <summary>Rename by swapping the job for one resumed under the new name; the session id and conversation stay.</summary>
public sealed record RenameSession(SessionRow Row, SessionName NewName) : ViewEffect;

public sealed record OpenTabForSession(SessionRow Row) : ViewEffect;

public sealed record StopSession(SessionRow Row) : ViewEffect;

public sealed record DeleteSession(SessionRow Row) : ViewEffect, IDialogEffect;

/// <summary>Move a session started as plain claude in another tab under the supervisor, so Vantage can manage it.</summary>
public sealed record AdoptSession(SessionRow Row) : ViewEffect;

/// <summary>Close the tabs of plain claude sessions that were never sent a message: nothing to keep.</summary>
public sealed record CloseEmptyTerminals(IReadOnlyList<SessionRow> Rows) : ViewEffect, IDialogEffect;

public sealed record OpenLaunchWizard : ViewEffect, IDialogEffect;

public sealed record ResumeStoppedSessions(IReadOnlyList<SessionRow> Rows) : ViewEffect, IDialogEffect;

public sealed record OpenAgentView : ViewEffect, ILeavesScreen;

public sealed record SwitchToWorktrees : ViewEffect, ILeavesScreen;

public sealed record Quit : ViewEffect, ILeavesScreen;

public sealed record ShowHelp : ViewEffect, IDialogEffect;

public sealed record ViewUpdate(SessionsViewState State, IReadOnlyList<ViewEffect> Effects);

public sealed record SessionsViewState(
    IReadOnlyList<SessionRow> Rows,
    string? SelectedSessionId,
    RowFilter Filter,
    bool FilterEditing,
    string? Banner,
    Footer Footer,
    bool Loaded = false)
    : IListViewState<SessionRow>
{
    /// <summary>Nothing listed yet: the first listing has not arrived, so the screen shows that it is waiting rather than an empty table.</summary>
    public static readonly SessionsViewState Empty = new([], null, new RowFilter(string.Empty), FilterEditing: false, Banner: null, Footer: Footer.None);

    private static readonly ListPolicy<SessionRow> Policy = new(
        KeyOf: row => row.Session.SessionId.Value,
        FilterFields: row => [row.Session.Name, row.Repository, row.Branch?.Value],
        Order: ByAttention,
        KeyComparison: StringComparison.Ordinal);

    /// <summary>The rows, selection, filter and loaded flag as the shared list component, built from this state's fields.</summary>
    public ListSelection<SessionRow> List => new(Policy, Rows, SelectedSessionId, Filter, FilterEditing, Loaded);

    public IReadOnlyList<SessionRow> VisibleRows => List.Visible;

    public SessionRow? Selected => List.Selected;

    public int NeedsInputCount => Rows.Count(row => row.Session.State.Kind == DisplayStateKind.NeedsInput);

    public string Title => List.Title("sessions");

    /// <summary>The footer to show: the last message, else how many sessions need input.</summary>
    public Footer Shown => Footer is Footer.Blank ? new Footer.NeedInput(NeedsInputCount) : Footer;

    /// <summary>This state with the list component's fields taken from <paramref name="list"/>.</summary>
    public SessionsViewState With(ListSelection<SessionRow> list) => this with { Rows = list.Rows, SelectedSessionId = list.SelectedKey, Filter = list.Filter, FilterEditing = list.FilterEditing, Loaded = list.Loaded };

    private static IEnumerable<SessionRow> ByAttention(IEnumerable<SessionRow> rows)
    {
        List<SessionRow> matching = [.. rows];
        Sessions ordered = new([.. matching.Select(row => row.Session)]);
        Dictionary<SessionId, SessionRow> bySession = matching.ToDictionary(row => row.Session.SessionId);

        return ordered.OrderedByAttention().Select(session => bySession[session.SessionId]);
    }
}

public static class SessionsView
{
    public static ViewUpdate Reduce(SessionsViewState state, ViewInput input)
    {
        return input switch
        {
            RowsLoaded loaded => Unchanged(state.With(state.List.Load(loaded.Rows)) with { Banner = null }),
            LoadFailed failed => Unchanged(state with { Banner = failed.Message }),
            RenameConfirmed confirmed => RenameWith(state, confirmed),
            SelectSession select => Unchanged(state.With(state.List.Select(select.SessionId.Value))),
            KeyPressed pressed when state.FilterEditing => Unchanged(state.With(state.List.EditFilter(pressed.Key))),
            KeyPressed pressed => Act(state, pressed.Key),
            _ => Unchanged(state),
        };
    }

    private static ViewUpdate Act(SessionsViewState state, Key key)
    {
        return key switch
        {
            { Name: KeyName.Up } => Unchanged(state.With(state.List.Move(-1))),
            { Name: KeyName.Down } => Unchanged(state.With(state.List.Move(+1))),
            { Name: KeyName.Escape } => Unchanged(state.With(state.List.ClearFilter())),
            { Name: KeyName.Enter } => SessionAction(state, row => new AttachToSession(row.Session.JobId!), Attachment.Here),
            { Name: KeyName.Character, Character: '/' } => Unchanged(state.With(state.List.StartFilter())),
            { Name: KeyName.Character, Character: 'o' } => SessionAction(state, row => new OpenTabForSession(row), Attachment.InOwnTab),
            { Name: KeyName.Character, Character: 'r' } => Rename(state),
            { Name: KeyName.Character, Character: 'x' } => SessionAction(state, row => new StopSession(row), Attachment.None),
            { Name: KeyName.Character, Character: 'd' } when state.Selected is { Session.Kind: SessionKind.Interactive, HasTranscript: false } row =>
                Effect(state with { Footer = Footer.None }, new CloseEmptyTerminals([row])),
            { Name: KeyName.Character, Character: 'C' } => CloseEmpty(state),
            { Name: KeyName.Character, Character: 'd' } => SessionAction(state, row => new DeleteSession(row), Attachment.None),
            { Name: KeyName.Character, Character: 'b' } => Adopt(state),
            { Name: KeyName.Character, Character: 'n' } => Effect(state, new OpenLaunchWizard()),
            { Name: KeyName.Character, Character: 'R' } => Effect(state, new ResumeStoppedSessions(state.VisibleRows)),
            { Name: KeyName.Character, Character: 'a' } => Effect(state, new OpenAgentView()),
            { Name: KeyName.Character, Character: 'w' } => Effect(state, new SwitchToWorktrees()),
            { Name: KeyName.Character, Character: 'q' } => Effect(state, new Quit()),
            { Name: KeyName.Character, Character: '?' } => Effect(state, new ShowHelp()),
            _ => Unchanged(state),
        };
    }

    /// <summary>Whether a key attaches to the session: here (so not while a tab already shows it), in its own tab, or not at all.</summary>
    private enum Attachment
    {
        None,
        InOwnTab,
        Here,
    }

    private static ViewUpdate SessionAction(SessionsViewState state, Func<SessionRow, ViewEffect> effect, Attachment attachment)
    {
        SessionRow? row = state.Selected;
        if (row is null)
        {
            return Unchanged(state with { Footer = new Footer.NoSessionSelected() });
        }

        if (row.Session.Kind == SessionKind.Interactive || row.Session.JobId is null)
        {
            return Unchanged(state with { Footer = new Footer.RunsInOwnTerminal(row.Session.Name, row.HasTranscript) });
        }

        // The open-in-tab key switches to the tab that already shows the session; attaching here as well would be a second attach.
        if (attachment == Attachment.Here && row.HasLiveTab)
        {
            return Unchanged(state with { Footer = new Footer.OpenInAnotherTab(row.Session.Name) });
        }

        // Attaching to a session with no process resumes it from its transcript; a session never sent a message has none.
        if (attachment != Attachment.None && row.Session.ProcessId is null && !row.HasTranscript)
        {
            return Unchanged(state with { Footer = new Footer.NeverSentAMessage(row.Session.Name) });
        }

        return Effect(state with { Footer = Footer.None }, effect(row));
    }

    /// <summary>Every plain claude session that was never sent a message, in one go; the TUI confirms before closing.</summary>
    private static ViewUpdate CloseEmpty(SessionsViewState state)
    {
        IReadOnlyList<SessionRow> empty = [.. state.Rows.Where(row => row.Session.Kind == SessionKind.Interactive && !row.HasTranscript && row.Session.ProcessId is not null)];

        return empty.Count == 0
            ? Unchanged(state with { Footer = new Footer.NoEmptyTerminals() })
            : Effect(state with { Footer = Footer.None }, new CloseEmptyTerminals(empty));
    }

    /// <summary>Renaming restarts the session under the new name, so it waits until nothing is in flight.</summary>
    private static ViewUpdate Rename(SessionsViewState state)
    {
        SessionRow? row = state.Selected;
        if (row is null || row.Session.Kind == SessionKind.Interactive || row.Session.JobId is null)
        {
            return SessionAction(state, _ => new AskForNewName(row!), Attachment.None);
        }

        return row switch
        {
            { HasTranscript: false, Session.ProcessId: null } => Unchanged(state with { Footer = new Footer.NeverSentAMessage(row.Session.Name) }),
            { HasTranscript: false } => Unchanged(state with { Footer = new Footer.NoMessagesToRename(row.Session.Name) }),
            { Session.State.Kind: DisplayStateKind.Working } => Unchanged(state with { Footer = new Footer.WorkingSoRenameWaits(row.Session.Name) }),
            _ => Effect(state with { Footer = Footer.None }, new AskForNewName(row)),
        };
    }

    private static ViewUpdate RenameWith(SessionsViewState state, RenameConfirmed confirmed)
    {
        if (confirmed.Name is null)
        {
            return Unchanged(state with { Footer = new Footer.RenameCancelled() });
        }

        try
        {
            SessionName name = SessionName.Parse(confirmed.Name.Trim());

            RenameSession rename = new(confirmed.Row, name);

            return Effect(state with { Footer = new Footer.Working(rename) }, rename);
        }
        catch (ArgumentException invalid)
        {
            // ArgumentException appends " (Parameter 'text')" to the message; the footer wants only the sentence.
            return Unchanged(state with { Footer = new Footer.InvalidName(invalid.Message.Split(" (Parameter")[0]) });
        }
    }

    /// <summary>
    /// Adoption ends the interactive process and resumes the conversation under the supervisor, so it is refused
    /// while the session is mid-turn: whatever that turn was doing would be lost.
    /// </summary>
    private static ViewUpdate Adopt(SessionsViewState state)
    {
        SessionRow? row = state.Selected;

        return row switch
        {
            null => Unchanged(state with { Footer = new Footer.NoSessionSelected() }),
            { Session.Kind: SessionKind.Background } => Unchanged(state with { Footer = new Footer.AlreadyUnderVantage(row.Session.Name) }),
            { Session.State.Kind: DisplayStateKind.Working } => Unchanged(state with { Footer = new Footer.WorkingSoAdoptWaits(row.Session.Name) }),
            _ => Effect(state with { Footer = Footer.None }, new AdoptSession(row)),
        };
    }

    private static ViewUpdate Unchanged(SessionsViewState state) => new ViewUpdate(state, []);

    private static ViewUpdate Effect(SessionsViewState state, ViewEffect effect) => new ViewUpdate(state, [effect]);
}