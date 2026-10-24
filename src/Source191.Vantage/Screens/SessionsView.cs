using Source191.Vantage.Domain;

namespace Source191.Vantage.Screens;

/// <summary>One line of the sessions table: the session plus what Vantage adds to it.</summary>
/// <summary>A listed session with what git, the state store and the transcript folder add to it. HasTranscript is false for a session never sent a message: Claude writes nothing to resume from until then.</summary>
public sealed record SessionRow(Session Session, string Repository, string? Branch, bool IsWorktree, string? Description, bool HasLiveTab, bool HasTranscript = true);

/// <summary>A key as the reducer sees it, independent of any terminal library.</summary>
public sealed record Key(KeyName Name, char Character)
{
    public static readonly Key Enter = new(KeyName.Enter, '\0');
    public static readonly Key Escape = new(KeyName.Escape, '\0');
    public static readonly Key Up = new(KeyName.Up, '\0');
    public static readonly Key Down = new(KeyName.Down, '\0');
    public static readonly Key Backspace = new(KeyName.Backspace, '\0');
    public static readonly Key Slash = new(KeyName.Character, '/');

    public static Key Of(char character)
    {
        return new Key(KeyName.Character, character);
    }
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
public sealed record SelectSession(string SessionId) : ViewInput;

public abstract record ViewEffect;

public sealed record AttachToSession(string JobId) : ViewEffect;

/// <summary>The TUI asks for a new name, then raises <see cref="RenameConfirmed"/> with what was typed (null when cancelled).</summary>
public sealed record AskForNewName(SessionRow Row) : ViewEffect;

public sealed record RenameConfirmed(SessionRow Row, string? Name) : ViewInput;

/// <summary>Rename by swapping the job for one resumed under the new name; the session id and conversation stay.</summary>
public sealed record RenameSession(SessionRow Row, SessionName NewName) : ViewEffect;

public sealed record OpenTabForSession(SessionRow Row) : ViewEffect;

public sealed record StopSession(SessionRow Row) : ViewEffect;

public sealed record DeleteSession(SessionRow Row) : ViewEffect;

/// <summary>Move a session started as plain claude in another tab under the supervisor, so Vantage can manage it.</summary>
public sealed record AdoptSession(SessionRow Row) : ViewEffect;

/// <summary>Close the tabs of plain claude sessions that were never sent a message: nothing to keep.</summary>
public sealed record CloseEmptyTerminals(IReadOnlyList<SessionRow> Rows) : ViewEffect;

public sealed record OpenLaunchWizard : ViewEffect;

public sealed record ResumeStoppedSessions : ViewEffect;

public sealed record OpenAgentView : ViewEffect;

public sealed record SwitchToWorktrees : ViewEffect;

public sealed record Quit : ViewEffect;

public sealed record ShowHelp : ViewEffect;

public sealed record ViewUpdate(SessionsViewState State, IReadOnlyList<ViewEffect> Effects);

public sealed record SessionsViewState(
    IReadOnlyList<SessionRow> Rows,
    string? SelectedSessionId,
    RowFilter Filter,
    bool FilterEditing,
    string? Banner,
    string Footer,
    bool Loaded = false)
{
    /// <summary>Nothing listed yet: the first listing has not arrived, so the screen shows that it is waiting rather than an empty table.</summary>
    public static readonly SessionsViewState Empty = new([], null, new RowFilter(string.Empty), FilterEditing: false, Banner: null, Footer: string.Empty);

    public IReadOnlyList<SessionRow> VisibleRows
    {
        get
        {
            Sessions ordered = new([.. Rows.Where(Matches).Select(row => row.Session)]);
            Dictionary<string, SessionRow> bySession = Rows.ToDictionary(row => row.Session.SessionId);

            return [.. ordered.OrderedByAttention().Select(session => bySession[session.SessionId])];
        }
    }

    public SessionRow? Selected => VisibleRows.FirstOrDefault(row => row.Session.SessionId == SelectedSessionId);

    public int NeedsInputCount => Rows.Count(row => row.Session.State.Kind == DisplayStateKind.NeedsInput);

    public string Title
    {
        get
        {
            string scope = string.IsNullOrWhiteSpace(Filter.Text) ? "all" : Filter.Text;

            return $"sessions({scope})[{VisibleRows.Count}]";
        }
    }

    private bool Matches(SessionRow row)
    {
        return Filter.Matches([row.Session.Name, row.Repository, row.Branch]);
    }
}

public static class SessionsView
{
    public static ViewUpdate Reduce(SessionsViewState state, ViewInput input)
    {
        return input switch
        {
            RowsLoaded loaded => Unchanged(Load(state, loaded.Rows)),
            LoadFailed failed => Unchanged(state with { Banner = failed.Message }),
            RenameConfirmed confirmed => RenameWith(state, confirmed),
            SelectSession select => Unchanged(Select(state, select.SessionId)),
            KeyPressed pressed when state.FilterEditing => Unchanged(EditFilter(state, pressed.Key)),
            KeyPressed pressed => Act(state, pressed.Key),
            _ => Unchanged(state),
        };
    }

    private static SessionsViewState Load(SessionsViewState state, IReadOnlyList<SessionRow> rows)
    {
        int previousIndex = IndexOfSelected(state);
        SessionsViewState loaded = state with { Rows = rows, Banner = null, Loaded = true };
        if (loaded.Selected is not null)
        {
            return loaded;
        }

        IReadOnlyList<SessionRow> visible = loaded.VisibleRows;
        if (visible.Count == 0)
        {
            return loaded with { SelectedSessionId = null };
        }

        int nearest = Math.Clamp(previousIndex, 0, visible.Count - 1);

        return loaded with { SelectedSessionId = visible[nearest].Session.SessionId };
    }

    private static SessionsViewState Select(SessionsViewState state, string sessionId)
    {
        if (!state.Rows.Any(row => row.Session.SessionId == sessionId))
        {
            return state;
        }

        SessionsViewState selected = state with { SelectedSessionId = sessionId };

        return selected.Selected is null
            ? selected with { Filter = new RowFilter(string.Empty), FilterEditing = false }
            : selected;
    }

    private static SessionsViewState EditFilter(SessionsViewState state, Key key)
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

    private static ViewUpdate Act(SessionsViewState state, Key key)
    {
        return key switch
        {
            { Name: KeyName.Up } => Unchanged(MoveSelection(state, -1)),
            { Name: KeyName.Down } => Unchanged(MoveSelection(state, +1)),
            { Name: KeyName.Escape } => Unchanged(Reselect(state with { Filter = new RowFilter(string.Empty) })),
            { Name: KeyName.Enter } => SessionAction(state, row => new AttachToSession(row.Session.JobId!), Attachment.Here),
            { Name: KeyName.Character, Character: '/' } => Unchanged(state with { FilterEditing = true, Filter = new RowFilter(string.Empty) }),
            { Name: KeyName.Character, Character: 'o' } => SessionAction(state, row => new OpenTabForSession(row), Attachment.InOwnTab),
            { Name: KeyName.Character, Character: 'r' } => Rename(state),
            { Name: KeyName.Character, Character: 'x' } => SessionAction(state, row => new StopSession(row), Attachment.None),
            { Name: KeyName.Character, Character: 'd' } when state.Selected is { Session.Kind: SessionKind.Interactive, HasTranscript: false } row =>
                Effect(state with { Footer = string.Empty }, new CloseEmptyTerminals([row])),
            { Name: KeyName.Character, Character: 'C' } => CloseEmpty(state),
            { Name: KeyName.Character, Character: 'd' } => SessionAction(state, row => new DeleteSession(row), Attachment.None),
            { Name: KeyName.Character, Character: 'b' } => Adopt(state),
            { Name: KeyName.Character, Character: 'n' } => Effect(state, new OpenLaunchWizard()),
            { Name: KeyName.Character, Character: 'R' } => Effect(state, new ResumeStoppedSessions()),
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
            return Unchanged(state with { Footer = "No session selected." });
        }

        if (row.Session.Kind == SessionKind.Interactive || row.Session.JobId is null)
        {
            string footer = row.HasTranscript
                ? $"{row.Session.Name} runs in its own terminal. Press b to move it under Vantage (its tab becomes a plain shell), or type /bg there."
                : $"{row.Session.Name} runs in its own terminal and has no messages yet. d closes its tab, C closes every such tab, or send it something there and press b.";

            return Unchanged(state with { Footer = footer });
        }

        // The open-in-tab key switches to the tab that already shows the session; attaching here as well would be a second attach.
        if (attachment == Attachment.Here && row.HasLiveTab)
        {
            return Unchanged(state with { Footer = $"{row.Session.Name} is open in another tab; press o to switch to it." });
        }

        // Attaching to a session with no process resumes it from its transcript; a session never sent a message has none.
        if (attachment != Attachment.None && row.Session.ProcessId is null && !row.HasTranscript)
        {
            return Unchanged(state with { Footer = $"{row.Session.Name} has nothing to resume: it was never sent a message. d deletes it." });
        }

        return Effect(state with { Footer = string.Empty }, effect(row));
    }

    /// <summary>Every plain claude session that was never sent a message, in one go; the TUI confirms before closing.</summary>
    private static ViewUpdate CloseEmpty(SessionsViewState state)
    {
        IReadOnlyList<SessionRow> empty = [.. state.Rows.Where(row => row.Session.Kind == SessionKind.Interactive && !row.HasTranscript && row.Session.ProcessId is not null)];

        return empty.Count == 0
            ? Unchanged(state with { Footer = "No terminal sessions without messages." })
            : Effect(state with { Footer = string.Empty }, new CloseEmptyTerminals(empty));
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
            { HasTranscript: false, Session.ProcessId: null } => Unchanged(state with { Footer = $"{row.Session.Name} has nothing to resume: it was never sent a message. d deletes it." }),
            { HasTranscript: false } => Unchanged(state with { Footer = $"{row.Session.Name} has no messages yet, so it cannot be renamed; send it something first." }),
            { Session.State.Kind: DisplayStateKind.Working } => Unchanged(state with { Footer = $"{row.Session.Name} is working; wait until it is idle or needs input, because renaming restarts it." }),
            _ => Effect(state with { Footer = string.Empty }, new AskForNewName(row)),
        };
    }

    private static ViewUpdate RenameWith(SessionsViewState state, RenameConfirmed confirmed)
    {
        if (confirmed.Name is null)
        {
            return Unchanged(state with { Footer = "Rename cancelled." });
        }

        try
        {
            SessionName name = SessionName.Parse(confirmed.Name.Trim());

            return Effect(state with { Footer = $"Renaming {confirmed.Row.Session.Name} to {name.Text}..." }, new RenameSession(confirmed.Row, name));
        }
        catch (ArgumentException invalid)
        {
            // ArgumentException appends " (Parameter 'text')" to the message; the footer wants only the sentence.
            return Unchanged(state with { Footer = invalid.Message.Split(" (Parameter")[0] });
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
            null => Unchanged(state with { Footer = "No session selected." }),
            { Session.Kind: SessionKind.Background } => Unchanged(state with { Footer = $"{row.Session.Name} already runs in the background under Vantage." }),
            { Session.State.Kind: DisplayStateKind.Working } => Unchanged(state with { Footer = $"{row.Session.Name} is working; wait until it is idle or needs input, so nothing in flight is lost." }),
            _ => Effect(state with { Footer = string.Empty }, new AdoptSession(row)),
        };
    }

    private static SessionsViewState MoveSelection(SessionsViewState state, int delta)
    {
        IReadOnlyList<SessionRow> visible = state.VisibleRows;
        if (visible.Count == 0)
        {
            return state;
        }

        int index = Math.Clamp(IndexOfSelected(state) + delta, 0, visible.Count - 1);

        return state with { SelectedSessionId = visible[index].Session.SessionId };
    }

    private static SessionsViewState Reselect(SessionsViewState state)
    {
        return state.Selected is not null ? state : Load(state, state.Rows);
    }

    private static int IndexOfSelected(SessionsViewState state)
    {
        IReadOnlyList<SessionRow> visible = state.VisibleRows;
        int index = visible.ToList().FindIndex(row => row.Session.SessionId == state.SelectedSessionId);

        return Math.Max(index, 0);
    }

    private static ViewUpdate Unchanged(SessionsViewState state)
    {
        return new ViewUpdate(state, []);
    }

    private static ViewUpdate Effect(SessionsViewState state, ViewEffect effect)
    {
        return new ViewUpdate(state, [effect]);
    }
}