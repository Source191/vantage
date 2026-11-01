using Source191.Vantage.Domain;
using System.Diagnostics;

namespace Source191.Vantage.Application.Views;

/// <summary>
/// A listed session with what git, the state store and the transcript folder add to it. Head is Unknown outside a
/// repository or when git did not name a branch. Tab says whether a live Vantage tab shows the session, and in which
/// colour. HasTranscript is false for a session never sent a message: Claude writes nothing to resume from until then,
/// so every caller must say which it is.
/// </summary>
public sealed record SessionRow(Session Session, string Repository, Head Head, LiveTab Tab, bool HasTranscript)
{

    /// <summary>The branch's pull request, or why there is none; a row outside a repository has no branch to look up.</summary>
    public PullRequestLookup PullRequest { get; init; } = new PullRequestLookup.Missing(PullRequestGap.NoBranch);

    /// <summary>What the session's process and everything it started use; null for a session with no process, or whose process has gone.</summary>
    public MemorySize? Memory { get; init; }

    /// <summary>The main checkout the session's folder belongs to; null outside a repository, which is how REPOSITORY knows to show the folder's label.</summary>
    public FolderPath? Checkout { get; init; }
}

/// <summary>A key as the reducer sees it, independent of any terminal library.</summary>
public sealed record Key(KeyName Name, char Character)
{
    public static readonly Key Enter = new(KeyName.Enter, '\0');
    public static readonly Key Escape = new(KeyName.Escape, '\0');
    public static readonly Key Up = new(KeyName.Up, '\0');
    public static readonly Key Down = new(KeyName.Down, '\0');
    public static readonly Key Left = new(KeyName.Left, '\0');
    public static readonly Key Right = new(KeyName.Right, '\0');
    public static readonly Key Backspace = new(KeyName.Backspace, '\0');
    public static readonly Key Slash = new(KeyName.Character, '/');

    public static Key Of(char character) => new(KeyName.Character, character);
}

public enum KeyName
{
    Character,
    Enter,
    Escape,
    Up,
    Down,
    Left,
    Right,
    Backspace,
}

public abstract record ViewInput;

public sealed record RowsLoaded(IReadOnlyList<SessionRow> Rows) : ViewInput
{
    /// <summary>The automated runs the same listing held, which are not rows.</summary>
    public AutomatedRuns AutomatedRuns { get; init; } = AutomatedRuns.None;
}

public sealed record LoadFailed(string Message) : ViewInput;

/// <summary>The status line's close mark was clicked, or a success's time ran out: clears that message, and never a newer one.</summary>
public sealed record MessageDismissed(StatusMessage Message) : ViewInput;

public sealed record KeyPressed(Key Key) : ViewInput;

/// <summary>Select a session by its id, as the worktrees view's Enter asks; a filter hiding it is cleared so it shows.</summary>
public sealed record SelectSession(SessionId SessionId) : ViewInput;

public abstract record ViewEffect;

public sealed record AttachToSession(JobId JobId) : ViewEffect, ILeavesScreen;

/// <summary>The TUI asks for a new name, then raises <see cref="RenameConfirmed"/> with what was typed, or <see cref="RenameCancelled"/>.</summary>
public sealed record AskForNewName(BackgroundSession Session) : ViewEffect, IDialogEffect;

public sealed record RenameConfirmed(BackgroundSession Session, string Name) : ViewInput;

/// <summary>The new name dialog was closed without an answer.</summary>
public sealed record RenameCancelled(BackgroundSession Session) : ViewInput;

/// <summary>Rename by swapping the job for one resumed under the new name; the session id and conversation stay.</summary>
public sealed record RenameSession(BackgroundSession Session, SessionName NewName) : ViewEffect;

/// <summary>Open the job in its own tab, or switch to the tab that already shows it.</summary>
public sealed record OpenTabForSession(BackgroundSession Session, LiveTab Tab) : ViewEffect;

public sealed record StopSession(BackgroundSession Session) : ViewEffect;

public sealed record DeleteSession(BackgroundSession Session) : ViewEffect, IDialogEffect;

/// <summary>Move a session started as plain claude in another tab under the supervisor, so Vantage can manage it.</summary>
public sealed record AdoptSession(InteractiveSession Session) : ViewEffect;

/// <summary>Close the tabs of plain claude sessions that were never sent a message: nothing to keep.</summary>
public sealed record CloseEmptyTerminals(IReadOnlyList<InteractiveSession> Sessions) : ViewEffect, IDialogEffect;

public sealed record OpenLaunchWizard : ViewEffect, IDialogEffect;

public sealed record ResumeStoppedSessions(IReadOnlyList<SessionRow> Rows) : ViewEffect, IDialogEffect;


public sealed record Quit : ViewEffect, ILeavesScreen;

public sealed record ShowHelp : ViewEffect, IDialogEffect;

/// <summary>Measure the session's process tree and show what its memory is made of; the process id is the tree's root.</summary>
public sealed record ShowMemory(Session Session, int ProcessId) : ViewEffect, IDialogEffect;

public sealed record ViewUpdate(SessionsViewState State, IReadOnlyList<ViewEffect> Effects);

public sealed record SessionsViewState(
    IReadOnlyList<SessionRow> Rows,
    string? SelectedSessionId,
    RowFilter Filter,
    bool FilterEditing,
    string? Banner,
    StatusMessage Message,
    bool Loaded = false)
    : IListViewState<SessionRow>
{
    /// <summary>Nothing listed yet: the first listing has not arrived, so the screen shows that it is waiting rather than an empty table.</summary>
    public static readonly SessionsViewState Empty = new([], null, new RowFilter(string.Empty), FilterEditing: false, Banner: null, Message: StatusMessage.None);

    /// <summary>The automated runs the last listing held, for the counts; kept when a listing fails, as the rows are.</summary>
    public AutomatedRuns AutomatedRuns { get; init; } = AutomatedRuns.None;

    private static readonly ListPolicy<SessionRow> Policy = new(
        KeyOf: row => row.Session.SessionId.Value,
        FilterFields: row => [row.Session.Name, row.Repository, .. FilterText.Of(row.Head)],
        Order: ByAttention,
        KeyComparison: StringComparison.Ordinal);

    /// <summary>The rows, selection, filter and loaded flag as the shared list component, built from this state's fields.</summary>
    public ListSelection<SessionRow> List => new(Policy, Rows, SelectedSessionId, Filter, FilterEditing, Loaded);

    public IReadOnlyList<SessionRow> VisibleRows => List.Visible;

    public SessionRow? Selected => List.Selected;


    public TableTitle Title => List.Title("session", row => row.Session.Name);

    /// <summary>The status message to show: the last message, else how many sessions need input.</summary>
    public StatusMessage Shown => Message;

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
            RowsLoaded loaded => Unchanged(state.With(state.List.Load(loaded.Rows)) with { Banner = null, AutomatedRuns = loaded.AutomatedRuns }),
            LoadFailed failed => Unchanged(state with { Banner = failed.Message }),
            MessageDismissed dismissed when ReferenceEquals(state.Message, dismissed.Message) => Unchanged(state with { Message = StatusMessage.None }),
            MessageDismissed => Unchanged(state),
            RenameConfirmed confirmed => RenameWith(state, confirmed),
            RenameCancelled => Unchanged(state with { Message = new StatusMessage.RenameCancelled() }),
            UpdateTabOpened => Effect(state, new Quit()),
            SelectSession select => Unchanged(state.With(state.List.Select(select.SessionId.Value))),
            KeyPressed pressed when state.FilterEditing => Unchanged(state.With(state.List.EditFilter(pressed.Key)) with { Message = StatusMessage.None }),
            KeyPressed pressed => Act(state, pressed.Key),
            _ => Unchanged(state),
        };
    }

    private static ViewUpdate Act(SessionsViewState state, Key key)
    {
        return key switch
        {
            // Moving on or filtering clears the last message, so the status line goes until there is something new to say.
            { Name: KeyName.Up } => Unchanged(state.With(state.List.Move(-1)) with { Message = StatusMessage.None }),
            { Name: KeyName.Down } => Unchanged(state.With(state.List.Move(+1)) with { Message = StatusMessage.None }),
            { Name: KeyName.Escape } => Unchanged(state.With(state.List.ClearFilter())),
            { Name: KeyName.Enter } => SessionAction(state, (row, session) => new OpenTabForSession(session, row.Tab), Attachment.InOwnTab),
            { Name: KeyName.Character, Character: '/' } => Unchanged(state.With(state.List.StartFilter()) with { Message = StatusMessage.None }),
            { Name: KeyName.Character, Character: 'r' } => Rename(state),
            { Name: KeyName.Character, Character: 'x' } => SessionAction(state, (_, session) => new StopSession(session), Attachment.None),
            { Name: KeyName.Character, Character: 'd' } when state.Selected is { Session: InteractiveSession terminal, HasTranscript: false } =>
                Effect(state with { Message = StatusMessage.None }, new CloseEmptyTerminals([terminal])),
            { Name: KeyName.Character, Character: 'C' } => CloseEmpty(state),
            { Name: KeyName.Character, Character: 'd' } => SessionAction(state, (_, session) => new DeleteSession(session), Attachment.None),
            { Name: KeyName.Character, Character: 'b' } => Adopt(state),
            { Name: KeyName.Character, Character: 'c' } => FolderAction(state, (folder, _) => new CopyFolderPath(folder)),
            { Name: KeyName.Character, Character: 'e' } => FolderAction(state, (folder, _) => new OpenFolderInEditor(folder)),
            { Name: KeyName.Character, Character: 'v' } => FolderAction(state, (folder, repository) => new FindSolution(folder, repository)),
            { Name: KeyName.Character, Character: 'p' } => OpenPullRequest(state),
            { Name: KeyName.Character, Character: 'm' } => MeasureMemory(state),
            { Name: KeyName.Character, Character: 'n' } => Effect(state, new OpenLaunchWizard()),
            { Name: KeyName.Character, Character: 'R' } => Effect(state, new ResumeStoppedSessions(state.VisibleRows)),
            { Name: KeyName.Character, Character: 'q' } => Effect(state, new Quit()),
            { Name: KeyName.Character, Character: '?' } => Effect(state, new ShowHelp()),
            { Name: KeyName.Character, Character: 'U' } => Effect(state, new UpdateVantage()),
            { Name: KeyName.Character, Character: '!' } => Effect(state, new ShowProblemReport()),
            _ when TabKeys.For(key) is ViewEffect tab => Effect(state, tab),
            _ => Unchanged(state),
        };
    }

    /// <summary>Whether a key attaches to the session, in its own tab, or not at all.</summary>
    private enum Attachment
    {
        None,
        InOwnTab,
    }

    /// <summary>A key that acts on a supervised job; a terminal session gets the status message that says why it cannot.</summary>
    private static ViewUpdate SessionAction(SessionsViewState state, Func<SessionRow, BackgroundSession, ViewEffect> effect, Attachment attachment)
    {
        SessionRow? row = state.Selected;
        if (row is null)
        {
            return Unchanged(state with { Message = new StatusMessage.NoSessionSelected() });
        }

        if (row.Session is not BackgroundSession session)
        {
            return Unchanged(state with { Message = new StatusMessage.RunsInOwnTerminal(row.Session.Name, row.HasTranscript) });
        }

        // Attaching to a session with no process resumes it from its transcript; a session never sent a message has none.
        if (attachment != Attachment.None && session.ProcessId is null && !row.HasTranscript)
        {
            return Unchanged(state with { Message = new StatusMessage.NeverSentAMessage(row.Session.Name) });
        }

        return Effect(state with { Message = StatusMessage.None }, effect(row, session));
    }

    /// <summary>A key that takes the row's folder to another tool; every row has one, whatever kind of session it is.</summary>
    private static ViewUpdate FolderAction(SessionsViewState state, Func<FolderPath, string, ViewEffect> effect)
    {
        return state.Selected switch
        {
            null => Unchanged(state with { Message = new StatusMessage.NoSessionSelected() }),
            SessionRow row => Effect(state with { Message = StatusMessage.None }, effect(row.Session.WorkingDirectory, row.Repository)),
        };
    }
    /// <summary>The row's pull request in the browser, or a status message saying why it has none.</summary>
    private static ViewUpdate OpenPullRequest(SessionsViewState state)
    {
        return state.Selected switch
        {
            null => Unchanged(state with { Message = new StatusMessage.NoSessionSelected() }),
            { PullRequest: PullRequestLookup.Found found } => Effect(state with { Message = StatusMessage.None }, new OpenPullRequest(found.PullRequest)),
            { PullRequest: PullRequestLookup.Missing missing } row => Unchanged(state with { Message = new StatusMessage.NoPullRequest(row.Session.Name, missing.Gap, missing.Host) }),
            _ => Unchanged(state),
        };
    }

    /// <summary>The memory breakdown of the selected session; a background session with no process is stopped, so there is nothing to measure.</summary>
    private static ViewUpdate MeasureMemory(SessionsViewState state)
    {
        return state.Selected switch
        {
            null => Unchanged(state with { Message = new StatusMessage.NoSessionSelected() }),
            { Session: BackgroundSession { ProcessId: int processId } session } => Effect(state with { Message = StatusMessage.None }, new ShowMemory(session, processId)),
            { Session: InteractiveSession session } => Effect(state with { Message = StatusMessage.None }, new ShowMemory(session, session.ProcessId)),
            SessionRow row => Unchanged(state with { Message = new StatusMessage.NothingToMeasure(row.Session.Name) }),
        };
    }

    /// <summary>Every plain claude session that was never sent a message, in one go; the TUI confirms before closing.</summary>
    private static ViewUpdate CloseEmpty(SessionsViewState state)
    {
        IReadOnlyList<InteractiveSession> empty = [.. state.Rows.Where(row => !row.HasTranscript).Select(row => row.Session).OfType<InteractiveSession>()];

        return empty.Count == 0
            ? Unchanged(state with { Message = new StatusMessage.NoEmptyTerminals() })
            : Effect(state with { Message = StatusMessage.None }, new CloseEmptyTerminals(empty));
    }

    /// <summary>Renaming restarts the session under the new name, so it waits until nothing is in flight.</summary>
    private static ViewUpdate Rename(SessionsViewState state)
    {
        if (state.Selected is not { Session: BackgroundSession session } row)
        {
            return SessionAction(state, (_, background) => new AskForNewName(background), Attachment.None);
        }

        return (row.HasTranscript, session) switch
        {
            (false, { ProcessId: null }) => Unchanged(state with { Message = new StatusMessage.NeverSentAMessage(session.Name) }),
            (false, _) => Unchanged(state with { Message = new StatusMessage.NoMessagesToRename(session.Name) }),
            (_, { State.Kind: DisplayStateKind.Working }) => Unchanged(state with { Message = new StatusMessage.WorkingSoRenameWaits(session.Name) }),
            _ => Effect(state with { Message = StatusMessage.None }, new AskForNewName(session)),
        };
    }

    private static ViewUpdate RenameWith(SessionsViewState state, RenameConfirmed confirmed)
    {
        return SessionName.Validate(confirmed.Name.Trim()) switch
        {
            SessionNameValidation.Valid valid => Renamed(state, new RenameSession(confirmed.Session, valid.Name)),
            SessionNameValidation.Refused refused => Unchanged(state with { Message = new StatusMessage.InvalidName(refused.Problem) }),
            _ => throw new UnreachableException(),
        };
    }

    private static ViewUpdate Renamed(SessionsViewState state, RenameSession rename) => Effect(state with { Message = new StatusMessage.Working(rename) }, rename);

    /// <summary>
    /// Adoption ends the interactive process and resumes the conversation under the supervisor, so it is refused
    /// while the session is mid-turn: whatever that turn was doing would be lost.
    /// </summary>
    private static ViewUpdate Adopt(SessionsViewState state)
    {
        SessionRow? row = state.Selected;

        return row switch
        {
            null => Unchanged(state with { Message = new StatusMessage.NoSessionSelected() }),
            { Session: BackgroundSession } => Unchanged(state with { Message = new StatusMessage.AlreadyUnderVantage(row.Session.Name) }),
            { Session.State.Kind: DisplayStateKind.Working } => Unchanged(state with { Message = new StatusMessage.WorkingSoAdoptWaits(row.Session.Name) }),
            { Session: InteractiveSession terminal } => Effect(state with { Message = StatusMessage.None }, new AdoptSession(terminal)),
            _ => Unchanged(state),
        };
    }

    private static ViewUpdate Unchanged(SessionsViewState state) => new(state, []);

    private static ViewUpdate Effect(SessionsViewState state, ViewEffect effect) => new(state, [effect]);
}