using Source191.Vantage.Application;
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
                context.ShowFooter(new Footer.NoDialogFor(effect.GetType().Name));
                break;
        }
    }

    /// <summary>Back on the UI thread after a removal: ask about the branch it left, and delete it on a yes.</summary>
    private static Footer AskAboutBranch(IEffectContext context, Outcome removal, BranchToConfirm branch, bool createdByVantage)
    {
        int? answer = Confirm.Query(context.App, "Delete branch", Wording.DeleteBranchQuestion(branch.Branch, createdByVantage), "Delete branch", "Keep");
        if (answer != 0)
        {
            return new Footer.BranchKept(removal);
        }

        DeleteBranch deleteBranch = new(branch);
        context.Dispatch(deleteBranch, deleted => new Footer.Followed(removal, deleted));

        return new Footer.Working(deleteBranch);
    }

    private static void Rename(IEffectContext context, SessionRow row)
    {
        string? typed = NameDialog.Show(context.App, "Rename session", Wording.RenamePrompt(row.Session.Name), row.Session.Name);
        context.Reduce(new RenameConfirmed(row, typed));
    }

    private static void ConfirmCloseEmpty(IEffectContext context, CloseEmptyTerminals close)
    {
        string question = Wording.CloseEmptyQuestion([.. close.Rows.Select(row => row.Session.Name)]);
        int? answer = Confirm.Query(context.App, "Close empty terminal sessions", question, "Close", "Keep");
        if (answer != 0)
        {
            context.ShowFooter(new Footer.Kept());
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
            context.ShowFooter(new Footer.NothingChosenToResume());
            return;
        }

        context.Dispatch(new ResumeSessions(toResume));
    }

    private static void ConfirmForce(IEffectContext context, ConfirmForceRemove confirm)
    {
        string? typed = TypedConfirmationDialog.Show(
            context.App,
            "Force remove",
            Wording.ForceRemoveWarning,
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
        int? answer = Confirm.Query(context.App, "Delete session", Wording.DeleteSessionQuestion(row.Session.Name), "Delete", "Cancel");
        if (answer != 0)
        {
            return;
        }

        context.RunInBackground(
            new Footer.Working(deletion),
            token => deleteSession.DeleteAsync(row, forced: false, token),
            outcome => outcome is DeleteSessionOutcome.BranchNotMerged unmerged
                ? AskAboutBranch(context, outcome, unmerged.Branch, createdByVantage: true)
                : new Footer.Reported(outcome));
    }

    private void Remove(IEffectContext context, RemoveWorktree remove)
    {
        context.RunInBackground(
            new Footer.Working(remove),
            token => removeWorktree.RemoveAsync(remove.Row, remove.Forced, token),
            outcome => outcome is RemoveWorktreeOutcome.BranchKept kept
                ? AskAboutBranch(context, outcome, kept.Branch, kept.CreatedByVantage)
                : new Footer.Reported(outcome));
    }
}