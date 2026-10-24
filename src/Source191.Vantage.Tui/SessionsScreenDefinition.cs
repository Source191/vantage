using Source191.Vantage.Application;
using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Launch;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Terminal.Gui.Drawing;

namespace Source191.Vantage.Tui;

/// <summary>The sessions view as the shared table screen shows it: its layout, columns, colours and the effects its keys raise.</summary>
internal sealed class SessionsScreenDefinition(StateTransitionNotifier notifier, LaunchWorkflow launches, SessionActions actions, TabOpener tabs, WorktreeBase defaultBase)
    : IScreenDefinition<SessionsViewState, SessionRow>
{
    private static readonly string[] Columns = ["NAME", "STATE", "REPOSITORY", "BRANCH", "WT", "AGE", "TAB", "DESCRIPTION"];

    public TimeSpan RefreshEvery => TimeSpan.FromSeconds(5);

    public string Crumb => " <sessions> ";

    public string WaitingText => "Listing sessions";

    public string LoadFailurePrefix => "listing failed";

    public string HeaderNames => "Machine:\nClaude:\nRoots:\nSessions:\nNeed input:";

    public string Keys => "<enter>\n<o>\n<n>\n<r>\n<x>\n<d>";

    public string KeyNames => "Attach\nOpen in tab\nNew session\nRename\nStop\nDelete";

    public string Keys2 => "<R>\n<C>\n<a>\n<w>\n</>\n<?>\n<q>";

    public string KeyNames2 => "Resume stopped\nClose empty tabs\nAgent view\nWorktrees\nFilter\nHelp\nQuit";

    public string HelpText => """
        Enter  attach (Ctrl+Z/Esc back)    o  open in its own tab, or switch to it when it is open
        n      new session                 r  rename (restarts the session under the new name)
        x      stop                        d  delete (and its worktree, safely); closes the tab of a terminal session with no messages
        C      close every tab whose claude has never been sent a message
        b      move a terminal session under Vantage (its tab becomes a shell)
        R      resume after a reboot       a  native agent view
        w      worktrees view              /  filter    ?  help    q  quit
        """;

    public IReadOnlyList<string> ColumnNames => Columns;

    public string Cell(SessionRow row, int column, DateTimeOffset now)
    {
        return column switch
        {
            0 => row.Session.Name,
            1 => row.Session.State.Label,
            2 => row.Repository,
            3 => row.Branch ?? "-",
            4 => row.IsWorktree ? "yes" : string.Empty,
            5 => Durations.Age(now - row.Session.StartedAt),
            6 => Tab(row),
            _ => row.Description ?? string.Empty,
        };
    }

    public Color RowColour(SessionRow row)
    {
        return Palette.ForState(row.Session.State.Kind);
    }

    /// <summary>The sessions line (48 columns at most, so it never wraps into the "Need input" row) and the needs-input count.</summary>
    public string ContextLines(SessionsViewState state)
    {
        const int width = 48;
        IEnumerable<string> byState = state.Rows
            .Where(row => row.Session.State.Kind != DisplayStateKind.NeedsInput)
            .GroupBy(row => row.Session.State.Label)
            .OrderByDescending(group => group.Count())
            .Select(group => $"{group.Count()} {group.Key}");
        string summary = state.Rows.Count == 0 ? "none" : $"{state.Rows.Count} ({string.Join(", ", byState)})";
        string fitted = summary.Length <= width ? summary : summary[..(width - 1)] + "…";

        return $"{fitted}\n{state.NeedsInputCount}";
    }

    public string FooterText(SessionsViewState state)
    {
        return state.Footer.Length == 0 ? $"{state.NeedsInputCount} need input" : state.Footer;
    }

    public SessionsViewState WithFooter(SessionsViewState state, string footer)
    {
        return state with { Footer = footer };
    }

    public (SessionsViewState State, IReadOnlyList<ViewEffect> Effects) Reduce(SessionsViewState state, ViewInput input)
    {
        ViewUpdate update = SessionsView.Reduce(state, input);

        return (update.State, update.Effects);
    }

    public void OnInput(SessionsViewState before, ViewInput input)
    {
        if (input is RowsLoaded loaded)
        {
            notifier.Observe(before.Rows, loaded.Rows);
        }
    }

    public bool LeavesScreen(ViewEffect effect)
    {
        return effect is AttachToSession or OpenAgentView or Quit or SwitchToWorktrees;
    }

    public void Handle(IEffectContext<SessionsViewState> context, ViewEffect effect)
    {
        switch (effect)
        {
            case OpenLaunchWizard:
                RunWizard(context);
                break;
            case OpenTabForSession open:
                context.RunInBackground($"Opening a tab for {open.Row.Session.Name}...", token => tabs.OpenAsync(open.Row, token));
                break;
            case StopSession stop:
                context.RunInBackground("Stopping...", token => actions.StopAsync(stop.Row, token));
                break;
            case AdoptSession adopt:
                context.RunInBackground($"Moving {adopt.Row.Session.Name} under Vantage...", token => actions.AdoptAsync(adopt.Row, token));
                break;
            case CloseEmptyTerminals close:
                CloseEmptyTerminals(context, close.Rows);
                break;
            case AskForNewName ask:
                string? typed = NameDialog.Show(context.App, "Rename session", $"Rename {ask.Row.Session.Name}. The session restarts under the new name and keeps its conversation.", ask.Row.Session.Name);
                context.Reduce(new RenameConfirmed(ask.Row, typed));
                break;
            case RenameSession rename:
                context.RunInBackground($"Renaming {rename.Row.Session.Name} to {rename.NewName.Text}...", token => actions.RenameAsync(rename.Row, rename.NewName, token));
                break;
            case DeleteSession delete:
                ConfirmAndDelete(context, delete.Row);
                break;
            case ResumeStoppedSessions:
                ResumeSessions(context);
                break;
            default:
                break;
        }
    }

    /// <summary>"terminal" marks a session started outside Vantage: it lives in its own tab and cannot be attached to.</summary>
    private static string Tab(SessionRow row)
    {
        return row switch
        {
            { Session.Kind: SessionKind.Interactive } => "terminal",
            { HasLiveTab: true } => "open",
            _ => string.Empty,
        };
    }

    private void CloseEmptyTerminals(IEffectContext<SessionsViewState> context, IReadOnlyList<SessionRow> rows)
    {
        string names = string.Join(", ", rows.Select(row => row.Session.Name));
        string what = rows.Count == 1 ? $"Close the tab of {names}?" : $"Close {rows.Count} tabs: {names}?";
        int? answer = Confirm.Query(context.App, "Close empty terminal sessions", $"{what}\nNone of them has been sent a message, so nothing is lost.", "Close", "Keep");
        if (answer != 0)
        {
            context.State = context.State with { Footer = "Kept." };
            return;
        }

        context.RunInBackground("Closing...", token => actions.CloseEmptyTerminalsAsync(rows, token));
    }

    private void ConfirmAndDelete(IEffectContext<SessionsViewState> context, SessionRow row)
    {
        int? answer = Confirm.Query(context.App, "Delete session", $"Delete {row.Session.Name}?\nIts worktree is removed only when nothing in it would be lost.", "Delete", "Cancel");
        if (answer != 0)
        {
            return;
        }

        context.RunInBackground(
            $"Deleting {row.Session.Name}...",
            token => actions.DeleteAsync(row, forced: false, token),
            outcome =>
            {
                if (outcome.BranchToConfirm is null)
                {
                    return outcome.Message;
                }

                // Back on the UI thread, so the question is asked before the answer is used.
                int? branchAnswer = Confirm.Query(context.App, "Delete branch", $"Branch {outcome.BranchToConfirm.Branch} is not merged. Delete it anyway?", "Delete branch", "Keep");
                if (branchAnswer != 0)
                {
                    return $"{outcome.Message} Branch kept.";
                }

                context.RunInBackground($"Deleting branch {outcome.BranchToConfirm.Branch}...", async token =>
                {
                    await actions.DeleteBranchAsync(outcome.BranchToConfirm, token);
                    return $"{outcome.Message} Branch deleted.";
                });

                return $"Deleting branch {outcome.BranchToConfirm.Branch}...";
            });
    }

    private void ResumeSessions(IEffectContext<SessionsViewState> context)
    {
        // The checklist lists sessions in the order the table shows them, not the order the listing returned them.
        ResumePlan plan = SessionActions.PlanResume(context.State.VisibleRows);
        IReadOnlyList<SessionRow> chosen = plan.ToChoose.Count == 0 ? [] : ResumeChecklistDialog.Show(context.App, plan.ToChoose);
        List<SessionRow> toResume = [.. plan.Automatic, .. chosen];
        if (toResume.Count == 0)
        {
            context.State = context.State with { Footer = "Nothing to resume." };
            return;
        }

        context.RunInBackground($"Resuming {toResume.Count}...", token => actions.ResumeAsync(toResume, token));
    }

    private void RunWizard(IEffectContext<SessionsViewState> context)
    {
        WizardResult? wizard = LaunchWizardDialog.Show(context.App, launches, defaultBase);
        if (wizard is null)
        {
            return;
        }

        context.RunInBackground($"Starting {wizard.Plan.Name.Text}...", async token =>
        {
            LaunchOutcome outcome = await launches.LaunchAsync(wizard.Plan, token);

            return outcome.Kind switch
            {
                LaunchOutcomeKind.Started => await launches.StartedAsync(wizard.Plan, outcome, wizard.OpenInTab, token),
                LaunchOutcomeKind.WorkspaceNotTrusted => await launches.OpenTrustTabAsync(wizard.Plan.Directory, token),
                _ => $"Could not start: {outcome.Message}",
            };
        });
    }
}