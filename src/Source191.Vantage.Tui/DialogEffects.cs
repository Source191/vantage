using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Launch;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Tui;

/// <summary>
/// Every effect that needs a dialog, for both views. Each flow asks its question, then hands the answer back through
/// the reducer or dispatches the effect it settles on; the work itself is always a handler's.
/// </summary>
public sealed class DialogEffects(LaunchWorkflow launches, WorktreeBase defaultBase, DeleteSessionHandler deleteSession, RemoveWorktreeHandler removeWorktree)
{
    internal void Show(IEffectContext context, ViewEffect effect)
    {
        switch (effect)
        {
            case ShowHelp:
                Confirm.Query(context.App, "Keys", context.HelpText, "Close");
                break;
            case OpenLaunchWizard:
                StartSession(context, fixedDirectory: null);
                break;
            case StartSessionIn start:
                StartSession(context, start.Row.Path);
                break;
            case AskForNewName ask:
                Rename(context, ask.Row);
                break;
            case CloseEmptyTerminals close:
                ConfirmCloseEmpty(context, close);
                break;
            case DeleteSession deletion:
                ConfirmDelete(context, deletion);
                break;
            case ResumeStoppedSessions resume:
                ChooseSessionsToResume(context, resume.Rows);
                break;
            case ConfirmForceRemove confirm:
                ConfirmForce(context, confirm);
                break;
            case RemoveWorktree remove:
                Remove(context, remove);
                break;
            default:
                context.ShowFooter($"No dialog is defined for {effect.GetType().Name}.");
                break;
        }
    }

    /// <summary>Back on the UI thread after a removal: ask about the branch it left, and delete it on a yes.</summary>
    private static string AskAboutBranch(IEffectContext context, string message, BranchToConfirm? branch, string question)
    {
        if (branch is null)
        {
            return message;
        }

        int? answer = Confirm.Query(context.App, "Delete branch", question, "Delete branch", "Keep");
        if (answer != 0)
        {
            return $"{message} Branch kept.";
        }

        DeleteBranch deleteBranch = new(branch);
        context.Dispatch(deleteBranch, deleted => $"{message} {deleted}");

        return WorkingText.For(deleteBranch);
    }

    private static void Rename(IEffectContext context, SessionRow row)
    {
        string? typed = NameDialog.Show(context.App, "Rename session", $"Rename {row.Session.Name}. The session restarts under the new name and keeps its conversation.", row.Session.Name);
        context.Reduce(new RenameConfirmed(row, typed));
    }

    private static void ConfirmCloseEmpty(IEffectContext context, CloseEmptyTerminals close)
    {
        string names = string.Join(", ", close.Rows.Select(row => row.Session.Name));
        string what = close.Rows.Count == 1 ? $"Close the tab of {names}?" : $"Close {close.Rows.Count} tabs: {names}?";
        int? answer = Confirm.Query(context.App, "Close empty terminal sessions", $"{what}\nNone of them has been sent a message, so nothing is lost.", "Close", "Keep");
        if (answer != 0)
        {
            context.ShowFooter("Kept.");
            return;
        }

        context.Dispatch(close);
    }

    private static void ChooseSessionsToResume(IEffectContext context, IReadOnlyList<SessionRow> rows)
    {
        // The checklist lists sessions in the order the table shows them, not the order the listing returned them.
        ResumePlan plan = ResumeSessionsHandler.Plan(rows);
        IReadOnlyList<SessionRow> chosen = plan.ToChoose.Count == 0 ? [] : ResumeChecklistDialog.Show(context.App, plan.ToChoose);
        List<SessionRow> toResume = [.. plan.Automatic, .. chosen];
        if (toResume.Count == 0)
        {
            context.ShowFooter("Nothing to resume.");
            return;
        }

        context.Dispatch(new ResumeSessions(toResume));
    }

    private static void ConfirmForce(IEffectContext context, ConfirmForceRemove confirm)
    {
        string? typed = TypedConfirmationDialog.Show(
            context.App,
            "Force remove",
            "This deletes the folder below and everything in it that is not committed and pushed.\nTo confirm, give its name:",
            confirm.ConfirmationText,
            "folder name");
        context.Reduce(new ForceConfirmed(confirm.Row, confirm.ConfirmationText, typed));
    }

    private void StartSession(IEffectContext context, string? fixedDirectory)
    {
        WizardResult? wizard = LaunchWizardDialog.Show(context.App, launches, defaultBase, fixedDirectory);
        if (wizard is null)
        {
            return;
        }

        context.Dispatch(new StartSession(wizard.Plan, wizard.OpenInTab));
    }

    private void ConfirmDelete(IEffectContext context, DeleteSession deletion)
    {
        SessionRow row = deletion.Row;
        int? answer = Confirm.Query(context.App, "Delete session", $"Delete {row.Session.Name}?\nIts worktree is removed only when nothing in it would be lost.", "Delete", "Cancel");
        if (answer != 0)
        {
            return;
        }

        context.RunInBackground(
            WorkingText.For(deletion),
            token => deleteSession.DeleteAsync(row, forced: false, token),
            outcome => AskAboutBranch(context, outcome.Message, outcome.BranchToConfirm, $"Branch {outcome.BranchToConfirm?.Branch} is not merged. Delete it anyway?"));
    }

    private void Remove(IEffectContext context, RemoveWorktree remove)
    {
        context.RunInBackground(
            WorkingText.For(remove),
            token => removeWorktree.RemoveAsync(remove.Row, remove.Forced, token),
            outcome => AskAboutBranch(context, outcome.Message, outcome.BranchToConfirm, $"Branch {outcome.BranchToConfirm?.Branch} was not created by Vantage. Delete it anyway?"));
    }
}