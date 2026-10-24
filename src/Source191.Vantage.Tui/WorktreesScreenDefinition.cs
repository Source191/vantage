using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Launch;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Terminal.Gui.Drawing;

namespace Source191.Vantage.Tui;

/// <summary>The worktrees view as the shared table screen shows it: its layout, columns, colours and the effects its keys raise.</summary>
internal sealed class WorktreesScreenDefinition(WorktreeActions actions, LaunchWorkflow launches, WorktreeBase defaultBase)
    : IScreenDefinition<WorktreesViewState, WorktreeRow>
{
    private static readonly string[] Columns = ["REPOSITORY", "FOLDER", "BRANCH", "DIRTY", "UNPUSHED", "LAST COMMIT", "SESSION", "HEALTH"];

    public TimeSpan RefreshEvery => TimeSpan.FromSeconds(15);

    public string Crumb => " <worktrees> ";

    public string WaitingText => "Scanning worktrees";

    public string LoadFailurePrefix => "scan failed";

    public string HeaderNames => "Machine:\nClaude:\nRoots:\nWorktrees:\nAt risk:";

    public string Keys => "<d>\n<D>\n<p>\n<s>\n<o>\n<enter>";

    public string KeyNames => "Remove\nForce remove\nPrune\nStart session\nOpen shell\nGo to session";

    public string Keys2 => "<w>\n<esc>\n</>\n<?>\n<q>";

    public string KeyNames2 => "Sessions\nSessions\nFilter\nHelp\nQuit";

    public string HelpText => """
        d      remove (refused when work would be lost)   D  force remove after typing the branch or folder name
        p      prune stale registrations in its repository
        s      start a session in this worktree            o  open a PowerShell tab there
        Enter  go to the session running in it             w or Esc  back to sessions
        /      filter    ?  help    q  quit
        Health: ok, locked (by another tool), orphan (folder git forgot), broken (registration or owner gone), empty (leftover folder)
        """;

    public IReadOnlyList<string> ColumnNames => Columns;

    public string Cell(WorktreeRow row, int column, DateTimeOffset now)
    {
        return column switch
        {
            0 => row.RepositoryName,
            1 => row.FolderName,
            2 => row.Branch ?? "-",
            3 => Count(row.DirtyFiles),
            4 => Count(row.UnpushedCommits),
            5 => row.LastCommitAt is null ? "-" : Durations.Age(now - row.LastCommitAt.Value),
            6 => row.SessionName ?? string.Empty,
            // The kind only: the reason goes in the footer for the selected row, so a long one is never cut off.
            _ => row.Health.Kind.ToString().ToLowerInvariant(),
        };
    }

    public Color RowColour(WorktreeRow row)
    {
        return row.Health.Kind switch
        {
            WorktreeHealthKind.Ok => Palette.White,
            WorktreeHealthKind.Locked => Palette.Amber,
            WorktreeHealthKind.Empty => Palette.Grey,
            _ => Palette.Red,
        };
    }

    public string ContextLines(WorktreesViewState state)
    {
        IEnumerable<string> byHealth = state.Rows
            .GroupBy(row => row.Health.Kind)
            .OrderByDescending(group => group.Count())
            .Select(group => $"{group.Count()} {group.Key.ToString().ToLowerInvariant()}");
        string summary = state.Rows.Count == 0 ? "none" : $"{state.Rows.Count} ({string.Join(", ", byHealth)})";
        int dirty = state.Rows.Count(row => row.DirtyFiles > 0);
        int unpushed = state.Rows.Count(row => row.UnpushedCommits > 0);

        return $"{summary}\n{dirty} with changes, {unpushed} with unpushed commits";
    }

    public string FooterText(WorktreesViewState state)
    {
        return state.FooterText;
    }

    public WorktreesViewState WithFooter(WorktreesViewState state, string footer)
    {
        return state with { Footer = footer };
    }

    public (WorktreesViewState State, IReadOnlyList<ViewEffect> Effects) Reduce(WorktreesViewState state, ViewInput input)
    {
        WorktreesViewUpdate update = WorktreesView.Reduce(state, input);

        return (update.State, update.Effects);
    }

    public void OnInput(WorktreesViewState before, ViewInput input)
    {
        // Nothing listens to the worktrees view's inputs.
    }

    public bool LeavesScreen(ViewEffect effect)
    {
        return effect is SwitchToSessions or JumpToSession or Quit or AttachToSession;
    }

    public void Handle(IEffectContext<WorktreesViewState> context, ViewEffect effect)
    {
        switch (effect)
        {
            case RemoveWorktree remove:
                Remove(context, remove.Row, remove.Forced);
                break;
            case ConfirmForceRemove confirm:
                ConfirmThenForce(context, confirm);
                break;
            case PruneRepository prune:
                context.RunInBackground("Pruning...", token => actions.PruneAsync(prune.Repository, token));
                break;
            case OpenShellTab shell:
                context.RunInBackground($"Opening a shell in {shell.Row.FolderName}...", token => actions.OpenShellAsync(shell.Row, token));
                break;
            case StartSessionIn start:
                RunWizard(context, start.Row);
                break;
            default:
                break;
        }
    }

    private static string Count(int number)
    {
        return number == 0 ? string.Empty : number.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static void ConfirmThenForce(IEffectContext<WorktreesViewState> context, ConfirmForceRemove confirm)
    {
        string? typed = TypedConfirmationDialog.Show(
            context.App,
            "Force remove",
            "This deletes the folder below and everything in it that is not committed and pushed.\nTo confirm, give its name:",
            confirm.ConfirmationText,
            "folder name");
        context.Reduce(new ForceConfirmed(confirm.Row, confirm.ConfirmationText, typed));
    }

    private void Remove(IEffectContext<WorktreesViewState> context, WorktreeRow row, bool forced)
    {
        context.RunInBackground(
            $"Removing {row.FolderName}...",
            token => actions.RemoveAsync(row, forced, token),
            outcome =>
            {
                if (outcome.BranchToConfirm is null)
                {
                    return outcome.Message;
                }

                int? answer = Confirm.Query(context.App, "Delete branch", $"Branch {outcome.BranchToConfirm.Branch} was not created by Vantage. Delete it anyway?", "Delete branch", "Keep");
                if (answer != 0)
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

    private void RunWizard(IEffectContext<WorktreesViewState> context, WorktreeRow row)
    {
        WizardResult? wizard = LaunchWizardDialog.Show(context.App, launches, defaultBase, fixedDirectory: row.Path);
        if (wizard is null)
        {
            return;
        }

        context.RunInBackground(
            $"Starting {wizard.Plan.Name.Text}...",
            async token =>
            {
                LaunchOutcome outcome = await launches.LaunchAsync(wizard.Plan, token);

                return outcome.Kind switch
                {
                    LaunchOutcomeKind.Started => await launches.StartedAsync(wizard.Plan, outcome, wizard.OpenInTab, token),
                    LaunchOutcomeKind.WorkspaceNotTrusted => $"Could not start: {wizard.Plan.Directory} is not trusted yet. Run claude there once and accept the prompt.",
                    _ => $"Could not start: {outcome.Message}",
                };
            });
    }
}