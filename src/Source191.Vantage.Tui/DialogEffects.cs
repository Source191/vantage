using Source191.Vantage.Application;
using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Launch;
using Source191.Vantage.Application.Reporting;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using System.Collections.Frozen;

namespace Source191.Vantage.Tui;

/// <summary>
/// Every effect that needs a dialog, for both views. Each flow asks its question, then hands the answer back through
/// the reducer or dispatches the effect it settles on; the work itself is always a handler's.
/// </summary>
public sealed class DialogEffects(RepositoryCatalogue catalogue, IRecentRepositories recent, IFolders folders, CurrentSettings settings, SolutionFinder solutions, UpdateCheck updates, DeleteSessionHandler deleteSession, RemoveWorktreeHandler removeWorktree, ProblemReports reports)
{
    /// <summary>One flow per dialog effect, keyed by the effect's type; <see cref="Handles"/> lets <c>DialogEffectsShould</c> check the set is complete.</summary>
    private static readonly FrozenDictionary<Type, Action<DialogEffects, IEffectContext, ViewEffect>> Flows = new Dictionary<Type, Action<DialogEffects, IEffectContext, ViewEffect>>
    {
        [typeof(ShowHelp)] = (_, context, _) => HelpDialog.Show(context.App, context.Diagnostics, "Keys", context.HelpText),
        [typeof(OpenLaunchWizard)] = (self, context, _) => self.StartSession(context, fixedDirectory: null),
        [typeof(StartSessionIn)] = (self, context, effect) => self.StartSession(context, ((StartSessionIn)effect).Row.Path),
        [typeof(AskForNewName)] = (_, context, effect) => Rename(context, ((AskForNewName)effect).Session),
        [typeof(CloseEmptyTerminals)] = (_, context, effect) => ConfirmCloseEmpty(context, (CloseEmptyTerminals)effect),
        [typeof(DeleteSession)] = (self, context, effect) => self.ConfirmDelete(context, (DeleteSession)effect),
        [typeof(ResumeStoppedSessions)] = (_, context, effect) => ChooseSessionsToResume(context, ((ResumeStoppedSessions)effect).Rows),
        [typeof(ConfirmForceRemove)] = (_, context, effect) => ConfirmForce(context, (ConfirmForceRemove)effect),
        [typeof(RemoveWorktree)] = (self, context, effect) => self.Remove(context, (RemoveWorktree)effect),
        [typeof(FindSolution)] = (self, context, effect) => self.OpenSolutionOf(context, (FindSolution)effect),
        [typeof(OpenSettings)] = (self, context, _) => self.EditSettings(context),
        [typeof(UpdateVantage)] = (self, context, _) => self.ConfirmUpdate(context),
        [typeof(ShowProblemReport)] = (self, context, effect) => self.ReportProblem(context, (ShowProblemReport)effect),
    }.ToFrozenDictionary();

    /// <summary>Whether an effect of this type has a dialog flow.</summary>
    public static bool Handles(Type effectType) => Flows.ContainsKey(effectType);

    internal void Show(IEffectContext context, ViewEffect effect)
    {
        if (Flows.TryGetValue(effect.GetType(), out Action<DialogEffects, IEffectContext, ViewEffect>? flow))
        {
            flow(this, context, effect);
            return;
        }

        context.ShowFooter(new Footer.NoDialogFor(effect.GetType().Name));
    }

    /// <summary>Back on the UI thread after a removal: ask about the branch it left, and delete it on a yes.</summary>
    private static Footer AskAboutBranch(IEffectContext context, Outcome removal, BranchToConfirm branch, bool createdByVantage)
    {
        int? answer = Confirm.Query(context.App, context.Diagnostics, DialogName.DeleteBranch, "Delete branch", Wording.DeleteBranchQuestion(branch.Branch.Value, createdByVantage), "Delete branch", "Keep");
        if (answer != 0)
        {
            return new Footer.BranchKept(removal);
        }

        DeleteBranch deleteBranch = new(branch);
        context.Dispatch(deleteBranch, deleted => new Footer.Followed(removal, deleted));

        return new Footer.Working(deleteBranch);
    }

    private static void Rename(IEffectContext context, BackgroundSession session)
    {
        string? typed = NameDialog.Show(context.App, context.Diagnostics, DialogName.Rename, "Rename session", Wording.RenamePrompt(session.Name), session.Name);
        context.Reduce(new RenameConfirmed(session, typed));
    }

    private static void ConfirmCloseEmpty(IEffectContext context, CloseEmptyTerminals close)
    {
        string question = Wording.CloseEmptyQuestion([.. close.Sessions.Select(session => session.Name)]);
        int? answer = Confirm.Query(context.App, context.Diagnostics, DialogName.CloseEmpty, "Close empty terminal sessions", question, "Close", "Keep");
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
        IReadOnlyList<SessionRow> chosen = plan.ToChoose.Count == 0 ? [] : ResumeChecklistDialog.Show(context.App, context.Diagnostics, plan.ToChoose);
        List<SessionRow> toResume = [.. plan.Automatic, .. chosen];
        if (toResume.Count == 0)
        {
            context.ShowFooter(new Footer.NothingChosenToResume());
            return;
        }

        context.Dispatch(new ResumeSessions([.. toResume.Select(row => row.Session).OfType<BackgroundSession>()]));
    }

    private static void ConfirmForce(IEffectContext context, ConfirmForceRemove confirm)
    {
        string? typed = TypedConfirmationDialog.Show(
            context.App,
            context.Diagnostics,
            DialogName.ForceRemove,
            "Force remove",
            Wording.ForceRemoveWarning,
            confirm.ConfirmationText,
            "folder name");
        context.Reduce(new ForceConfirmed(confirm.Row, confirm.ConfirmationText, typed));
    }

    /// <summary>
    /// Asks before updating; on a yes the handler opens the update tab, then the screen leaves with Quit so this process
    /// releases its files. A failure to open the tab keeps Vantage open and says why.
    /// </summary>
    private void ConfirmUpdate(IEffectContext context)
    {
        UpdateStatus status = updates.Status;
        if (status is not UpdateStatus.Available available)
        {
            context.ShowFooter(UpdateVantage.Refusal(status) ?? new Footer.UpToDate());
            return;
        }

        int? answer = Confirm.Query(context.App, context.Diagnostics, DialogName.Update, "Update Vantage", Wording.UpdateQuestion(available.Newest), "Update", "Cancel");
        if (answer != 0)
        {
            return;
        }

        context.Dispatch(new StartUpdate(available.Newest), outcome =>
        {
            context.Reduce(new UpdateTabOpened());
            return new Footer.Reported(outcome);
        });
    }

    private void StartSession(IEffectContext context, FolderPath? fixedDirectory)
    {
        WizardResult? wizard = LaunchWizardDialog.Show(context.App, context.Diagnostics, catalogue, recent, folders, settings.Value, fixedDirectory);
        if (wizard is null)
        {
            return;
        }

        context.Dispatch(new StartSession(wizard.Plan, wizard.OpenInTab));
    }

    /// <summary>The settings dialog over the settings saved now; a save is dispatched, so the footer confirms it and the refresh after it uses the new values.</summary>
    private void EditSettings(IEffectContext context)
    {
        Settings? saved = SettingsDialog.Show(context.App, context.Diagnostics, settings.Value, folders.DirectoryExists);
        if (saved is null)
        {
            return;
        }

        context.Dispatch(new SaveSettings(saved));
    }

    private void ConfirmDelete(IEffectContext context, DeleteSession deletion)
    {
        BackgroundSession session = deletion.Session;
        int? answer = Confirm.Query(context.App, context.Diagnostics, DialogName.DeleteSession, "Delete session", Wording.DeleteSessionQuestion(session.Name), "Delete", "Cancel");
        if (answer != 0)
        {
            return;
        }

        context.RunInBackground(
            new Footer.Working(deletion),
            token => deleteSession.DeleteAsync(session, forced: false, token),
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

    /// <summary>Builds the report off the UI thread; back on it, the dialog shows it, and opening or copying is dispatched.</summary>
    private void ReportProblem(IEffectContext context, ShowProblemReport show) => context.RunInBackground(new Footer.Working(show), _ => reports.BuildAsync(), report => AskAboutReport(context, report));

    private Footer AskAboutReport(IEffectContext context, ProblemReport report)
    {
        ProblemReportEffect answer = ProblemReportDialog.Show(context.App, context.Diagnostics, report, reports.DefaultTitle);

        return answer switch
        {
            ProblemReportEffect.Open open => Dispatched(context, new OpenProblemReport(open.Title, report)),
            ProblemReportEffect.Copy copy => Dispatched(context, new CopyProblemReport(copy.Title, report)),
            _ => Footer.None,
        };
    }

    private static Footer.Working Dispatched(IEffectContext context, ViewEffect effect)
    {
        context.Dispatch(effect);

        return new Footer.Working(effect);
    }
    /// <summary>Looks off the UI thread; back on it, one solution opens, none says so, and several are asked about.</summary>
    private void OpenSolutionOf(IEffectContext context, FindSolution find)
    {
        context.RunInBackground(
            new Footer.Working(find),
            _ => Task.FromResult(solutions.Find(find.Folder, find.RepositoryName)),
            search => search switch
            {
                SolutionSearch.One one => Open(context, one.File),
                SolutionSearch.Several several => ChooseSolution(context, find, several.Files),
                _ => new Footer.NoSolution(find.Folder.Name),
            });
    }

    private static Footer ChooseSolution(IEffectContext context, FindSolution find, IReadOnlyList<string> files)
    {
        string[] buttons = [.. files.Select(Path.GetFileName)!, "Cancel"];
        int? answer = Confirm.Query(context.App, context.Diagnostics, DialogName.OpenSolution, Wording.ChooseSolutionTitle, Wording.ChooseSolutionQuestion(find.Folder.Name), buttons);

        return answer is int chosen && chosen < files.Count
            ? Open(context, files[chosen])
            : Footer.None;
    }

    private static Footer.Working Open(IEffectContext context, string file)
    {
        OpenSolution open = new(file);
        context.Dispatch(open);

        return new Footer.Working(open);
    }
}