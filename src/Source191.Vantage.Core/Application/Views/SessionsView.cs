using Source191.Vantage.Domain;
using System.Diagnostics;

namespace Source191.Vantage.Application.Views;

public static class SessionsView
{
    public static ViewUpdate Reduce(SessionsViewState state, ViewInput input)
    {
        return input switch
        {
            RowsLoaded loaded => Unchanged(state.With(state.List.Load(loaded.Rows)) with { Banner = new LoadBanner.None(), AutomatedRuns = loaded.AutomatedRuns }),
            LoadFailed failed => Unchanged(state with { Banner = new LoadBanner.Failed(failed.Message) }),
            MessageDismissed dismissed when ReferenceEquals(state.Message, dismissed.Message) => Unchanged(state with { Message = StatusMessage.None }),
            MessageDismissed => Unchanged(state),
            RenameConfirmed confirmed => RenameWith(state, confirmed),
            RenameCancelled => Unchanged(state with { Message = new StatusMessage.RenameCancelled() }),
            UpdateTabOpened => Effect(state, new Quit()),
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
            { Name: KeyName.Escape } when state.Filter.Text.Length > 0 => Unchanged(state.With(state.List.ClearFilter()) with { Message = StatusMessage.None }),
            { Name: KeyName.Enter } => Open(state),
            { Name: KeyName.Character, Character: '/' } => Unchanged(state.With(state.List.StartFilter()) with { Message = StatusMessage.None }),
            { Name: KeyName.Character, Character: 'r' } => Rename(state),
            { Name: KeyName.Character, Character: 'x' } => SessionAction(state, (_, session) => new StopSession(session)),
            { Name: KeyName.Character, Character: 'd' } when state.Selected is { Session: InteractiveSession terminal, HasTranscript: false } =>
                Effect(state with { Message = StatusMessage.None }, new CloseEmptyTerminals([terminal])),
            { Name: KeyName.Character, Character: 'C' } => CloseEmpty(state),
            { Name: KeyName.Character, Character: 'd' } => SessionAction(state, (_, session) => new DeleteSession(session)),
            { Name: KeyName.Character, Character: 'b' } => Adopt(state),
            { Name: KeyName.Character, Character: 'c' } => FolderAction(state, (folder, _) => new CopyFolderPath(folder)),
            { Name: KeyName.Character, Character: 'f' } => FolderAction(state, (folder, _) => new OpenFolderInEditor(folder)),
            { Name: KeyName.Character, Character: 's' } => FolderAction(state, (folder, repository) => new FindSolution(folder, repository)),
            { Name: KeyName.Character, Character: 'p' } => OpenPullRequest(state),
            { Name: KeyName.Character, Character: 'm' } => MeasureMemory(state),
            { Name: KeyName.Character, Character: 'n' } => Effect(state, new OpenLaunchWizard()),
            { Name: KeyName.Character, Character: 'R' } => ResumeAll(state),
            { Name: KeyName.Character, Character: 'q' } => Effect(state, new Quit()),
            { Name: KeyName.Character, Character: '?' } => Effect(state, new ShowHelp()),
            { Name: KeyName.Character, Character: 'U' } => Effect(state, new UpdateVantage()),
            { Name: KeyName.Character, Character: '!' } => Effect(state, new ShowProblemReport()),
            _ when TabKeys.TryFor(key, out ViewEffect? tab) => Effect(state, tab),
            _ => Unchanged(state),
        };
    }

    /// <summary>
    /// What Enter does to a session: open it in its own tab or switch to that tab, or say why it cannot. The Sessions tab
    /// asks it for the selected row and the console for a worktree's session, so both tabs' Enter behave the same.
    /// </summary>
    public static RowDecision Opening(SessionRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        return row.Session switch
        {
            not BackgroundSession => new RowDecision.Explain(new StatusMessage.RunsInOwnTerminal(row.Session.Name, row.HasTranscript)),
            // Attaching to a session with no process resumes it from its transcript; a session never sent a message has none.
            BackgroundSession { Process: JobProcess.None } when !row.HasTranscript => new RowDecision.Explain(new StatusMessage.NeverSentAMessage(row.Session.Name)),
            BackgroundSession session => new RowDecision.Act(new OpenTabForSession(session, row.Tab)),
        };
    }

    private static ViewUpdate Open(SessionsViewState state)
    {
        return state.Selected switch
        {
            null => Unchanged(state with { Message = new StatusMessage.NoSessionSelected() }),
            SessionRow row => Opening(row) switch
            {
                RowDecision.Act act => Effect(state with { Message = StatusMessage.None }, act.Effect),
                RowDecision.Explain explain => Unchanged(state with { Message = explain.Message }),
                _ => throw new UnreachableException(),
            },
        };
    }

    /// <summary>
    /// Every stopped or failed job the table shows, in its order, so a filter narrows what R brings back; a session
    /// stopped on purpose and not wanted is deleted with d instead.
    /// </summary>
    private static ViewUpdate ResumeAll(SessionsViewState state)
    {
        BackgroundSession[] sessions = [.. state.VisibleRows
            .Select(row => row.Session)
            .OfType<BackgroundSession>()
            .Where(session => session.State.Kind is DisplayStateKind.Stopped or DisplayStateKind.Failed)];

        return sessions.Length == 0
            ? Unchanged(state with { Message = new StatusMessage.NothingToResume() })
            : Effect(state with { Message = StatusMessage.None }, new ResumeSessions(sessions));
    }

    /// <summary>A key that acts on a supervised job; a terminal session gets the status message that says why it cannot.</summary>
    private static ViewUpdate SessionAction(SessionsViewState state, Func<SessionRow, BackgroundSession, ViewEffect> effect)
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
            _ => throw new UnreachableException(),
        };
    }

    /// <summary>The memory breakdown of the selected session; a background session with no process is stopped, so there is nothing to measure.</summary>
    private static ViewUpdate MeasureMemory(SessionsViewState state)
    {
        return state.Selected switch
        {
            null => Unchanged(state with { Message = new StatusMessage.NoSessionSelected() }),
            { Session: BackgroundSession { Process: JobProcess.Running running } session } => Effect(state with { Message = StatusMessage.None }, new ShowMemory(session, running.Id)),
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
            return SessionAction(state, (_, background) => new AskForNewName(background));
        }

        return (row.HasTranscript, session) switch
        {
            (false, { Process: JobProcess.None }) => Unchanged(state with { Message = new StatusMessage.NeverSentAMessage(session.Name) }),
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
            _ => throw new UnreachableException(),
        };
    }

    private static ViewUpdate Unchanged(SessionsViewState state) => new(state, []);

    private static ViewUpdate Effect(SessionsViewState state, ViewEffect effect) => new(state, [effect]);
}