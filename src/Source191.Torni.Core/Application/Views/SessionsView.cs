using Source191.Torni.Domain;
using System.Diagnostics;
using static Source191.Torni.Application.Views.ViewUpdate<Source191.Torni.Application.Views.SessionsViewState>;

namespace Source191.Torni.Application.Views;

/// <summary>The Sessions tab as a pure function of state and input; every key that acts on the selected row goes through <see cref="RowAction"/>, and <see cref="Opening"/> is shared with the console.</summary>
public static class SessionsView
{
    public static ViewUpdate<SessionsViewState> Reduce(SessionsViewState state, ViewInput input)
    {
        return input switch
        {
            RowsLoaded loaded => Unchanged(state with { List = state.List.Load(loaded.Rows), Banner = new LoadBanner.None(), AutomatedRuns = loaded.AutomatedRuns }),
            LoadFailed failed => Unchanged(state with { Banner = new LoadBanner.Failed(failed.Message) }),
            MessageDismissed dismissed when ReferenceEquals(state.Message, dismissed.Message) => Unchanged(state with { Message = StatusMessage.None }),
            MessageDismissed => Unchanged(state),
            RenameConfirmed confirmed => RenameWith(state, confirmed),
            RenameCancelled => Unchanged(state with { Message = new StatusMessage.RenameCancelled() }),
            UpdateTabOpened => Effect(state, new Quit()),
            KeyPressed pressed when state.List.FilterEditing => Unchanged(state with { List = state.List.EditFilter(pressed.Key), Message = StatusMessage.None }),
            KeyPressed pressed => Act(state, pressed.Key),
            _ => Unchanged(state),
        };
    }

    private static ViewUpdate<SessionsViewState> Act(SessionsViewState state, Key key)
    {
        return key switch
        {
            // Moving on or filtering clears the last message, so the status line goes until there is something new to say.
            { Name: KeyName.Up } => Unchanged(state with { List = state.List.Move(-1), Message = StatusMessage.None }),
            { Name: KeyName.Down } => Unchanged(state with { List = state.List.Move(+1), Message = StatusMessage.None }),
            { Name: KeyName.Escape } when state.List.Filter.Text.Length > 0 => Unchanged(state with { List = state.List.ClearFilter(), Message = StatusMessage.None }),
            { Name: KeyName.Enter } => RowAction(state, Opening),
            { Name: KeyName.Character, Character: '/' } => Unchanged(state with { List = state.List.StartFilter(), Message = StatusMessage.None }),
            { Name: KeyName.Character, Character: 'r' } => RowAction(state, Rename),
            { Name: KeyName.Character, Character: 'x' } => RowAction(state, row => OnJob(row, job => new StopSession(job))),
            { Name: KeyName.Character, Character: 'd' } => RowAction(state, Delete),
            { Name: KeyName.Character, Character: 'C' } => CloseEmpty(state),
            { Name: KeyName.Character, Character: 'b' } => RowAction(state, Adopt),
            { Name: KeyName.Character, Character: 'c' } => RowAction(state, row => new RowDecision.Act(new CopyFolderPath(row.Session.WorkingDirectory))),
            { Name: KeyName.Character, Character: 'f' } => RowAction(state, row => new RowDecision.Act(new OpenFolderInEditor(row.Session.WorkingDirectory))),
            { Name: KeyName.Character, Character: 's' } => RowAction(state, row => new RowDecision.Act(new FindSolution(row.Session.WorkingDirectory, row.Repository))),
            { Name: KeyName.Character, Character: 'p' } => RowAction(state, PullRequestOf),
            { Name: KeyName.Character, Character: 'm' } => RowAction(state, Memory), { Name: KeyName.Character, Character: 'n' } => Effect(state, new OpenLaunchWizard()),
            { Name: KeyName.Character, Character: 'R' } => ResumeAll(state),
            _ when SharedKeys.TryFor(key, out ViewEffect? shared) => Effect(state, shared),
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
        return row.Session switch
        {
            not Session.Background => new RowDecision.Explain(new StatusMessage.RunsInOwnTerminal(row.Session.Name, row.HasTranscript)),
            // Attaching to a session with no process resumes it from its transcript; a session never sent a message has none.
            Session.Background { Process: JobProcess.None } when !row.HasTranscript => new RowDecision.Explain(new StatusMessage.NeverSentAMessage(row.Session.Name)),
            Session.Background session => new RowDecision.Act(new OpenTabForSession(session, row.Tab)),
        };
    }

    /// <summary>Either an effect to run or a status message explaining why nothing happens.</summary>
    private static ViewUpdate<SessionsViewState> RowAction(SessionsViewState state, Func<SessionRow, RowDecision> decide)
    {
        SessionRow? row = state.Selected;
        if (row is null)
        {
            return Unchanged(state with { Message = new StatusMessage.NoSessionSelected() });
        }

        return decide(row) switch
        {
            RowDecision.Act act => Effect(state with { Message = StatusMessage.None }, act.Effect),
            RowDecision.Explain explain => Unchanged(state with { Message = explain.Message }),
            _ => throw new UnreachableException(),
        };
    }
    /// <summary>
    /// Every stopped or failed job the table shows, in its order, so a filter narrows what R brings back; a session
    /// stopped on purpose and not wanted is deleted with d instead.
    /// </summary>
    private static ViewUpdate<SessionsViewState> ResumeAll(SessionsViewState state)
    {
        Session.Background[] sessions = [.. state.VisibleRows
            .Select(row => row.Session)
            .OfType<Session.Background>()
            .Where(session => session.State.Kind is DisplayStateKind.Stopped or DisplayStateKind.Failed)];

        return sessions.Length == 0
            ? Unchanged(state with { Message = new StatusMessage.NothingToResume() })
            : Effect(state with { Message = StatusMessage.None }, new ResumeSessions(sessions));
    }

    /// <summary>A key that acts on a supervised job; a terminal session gets the status message that says why it cannot.</summary>
    private static RowDecision OnJob(SessionRow row, Func<Session.Background, ViewEffect> effect)
    {
        return row.Session switch
        {
            Session.Background job => new RowDecision.Act(effect(job)),
            Session.Interactive => new RowDecision.Explain(new StatusMessage.RunsInOwnTerminal(row.Session.Name, row.HasTranscript)),
            _ => throw new UnreachableException(),
        };
    }

    /// <summary>A terminal session never sent a message is closed; any other is deleted as a job, or refused when it is a terminal with a conversation.</summary>
    private static RowDecision Delete(SessionRow row)
    {
        return row is { Session: Session.Interactive terminal, HasTranscript: false }
            ? new RowDecision.Act(new CloseEmptyTerminals([terminal]))
            : OnJob(row, job => new DeleteSession(job));
    }

    /// <summary>The row's pull request in the browser, or a status message saying why it has none.</summary>
    private static RowDecision PullRequestOf(SessionRow row)
    {
        return row.PullRequest switch
        {
            PullRequestLookup.Found found => new RowDecision.Act(new OpenPullRequest(found.PullRequest)),
            PullRequestLookup.Missing missing => new RowDecision.Explain(new StatusMessage.NoPullRequest(Wording.Name(row.Session.Name), missing.Gap, missing.Host)),
            _ => throw new UnreachableException(),
        };
    }

    /// <summary>The memory breakdown of the selected session; a background session with no process is stopped, so there is nothing to measure.</summary>
    private static RowDecision Memory(SessionRow row)
    {
        return row.Session switch
        {
            Session.Background { Process: JobProcess.Running running } session => new RowDecision.Act(new ShowMemory(session, running.Id)),
            Session.Interactive session => new RowDecision.Act(new ShowMemory(session, session.ProcessId)),
            Session.Background => new RowDecision.Explain(new StatusMessage.NothingToMeasure(row.Session.Name)),
            _ => throw new UnreachableException(),
        };
    }
    /// <summary>Every plain claude session that was never sent a message, in one go; the TUI confirms before closing.</summary>
    private static ViewUpdate<SessionsViewState> CloseEmpty(SessionsViewState state)
    {
        IReadOnlyList<Session.Interactive> empty = [.. state.List.Rows.Where(row => !row.HasTranscript).Select(row => row.Session).OfType<Session.Interactive>()];

        return empty.Count == 0
            ? Unchanged(state with { Message = new StatusMessage.NoEmptyTerminals() })
            : Effect(state with { Message = StatusMessage.None }, new CloseEmptyTerminals(empty));
    }

    /// <summary>Renaming restarts the session under the new name, so it waits until nothing is in flight.</summary>
    private static RowDecision Rename(SessionRow row)
    {
        if (row.Session is not Session.Background session)
        {
            return new RowDecision.Explain(new StatusMessage.RunsInOwnTerminal(row.Session.Name, row.HasTranscript));
        }

        return (row.HasTranscript, session) switch
        {
            (false, { Process: JobProcess.None }) => new RowDecision.Explain(new StatusMessage.NeverSentAMessage(session.Name)),
            (false, _) => new RowDecision.Explain(new StatusMessage.NoMessagesToRename(session.Name)),
            (_, { State.Kind: DisplayStateKind.Working }) => new RowDecision.Explain(new StatusMessage.WorkingSoRenameWaits(session.Name)),
            _ => new RowDecision.Act(new AskForNewName(session)),
        };
    }
    private static ViewUpdate<SessionsViewState> RenameWith(SessionsViewState state, RenameConfirmed confirmed)
    {
        return SessionName.Validate(confirmed.Name.Trim()) switch
        {
            SessionNameValidation.Valid valid => Renamed(state, new RenameSession(confirmed.Session, valid.Name)),
            SessionNameValidation.Refused refused => Unchanged(state with { Message = new StatusMessage.InvalidName(refused.Problem) }),
            _ => throw new UnreachableException(),
        };
    }

    private static ViewUpdate<SessionsViewState> Renamed(SessionsViewState state, RenameSession rename) => Effect(state with { Message = new StatusMessage.Working(rename) }, rename);

    /// <summary>
    /// Adoption ends the interactive process and resumes the conversation under the supervisor, so it is refused
    /// while the session is mid-turn: whatever that turn was doing would be lost.
    /// </summary>
    private static RowDecision Adopt(SessionRow row)
    {
        return row.Session switch
        {
            Session.Background => new RowDecision.Explain(new StatusMessage.AlreadyInBackground(row.Session.Name)),
            Session.Interactive { State.Kind: DisplayStateKind.Working } => new RowDecision.Explain(new StatusMessage.WorkingSoAdoptWaits(row.Session.Name)),
            Session.Interactive terminal => new RowDecision.Act(new AdoptSession(terminal)),
            _ => throw new UnreachableException(),
        };
    }
}