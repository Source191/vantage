using Source191.Vantage.Application.Launch;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Application;

/// <summary>
/// Every sentence Vantage shows, in one place: footers, dialog questions, the text beside the busy spinner and the
/// key help. Reducers, dialogs and the screen hand over facts; this class chooses the words. <c>WordingShould</c>
/// holds each sentence verbatim.
/// </summary>
public static partial class Wording
{
    /// <summary>Printed before an attach. Attach lands on Claude's agent list when the session has no messages yet; that screen leaves with Esc, not Ctrl+Z.</summary>
    public static string ReturnHint => "Ctrl+Z returns to Vantage. If you see the agent list instead, the session has no messages yet: Space sends it a prompt, Esc returns to Vantage.";

    public static string HereUsage => "Usage: vantage here [name] [-p \"opening prompt\"]";

    public static string ClaimTabUsage => "Usage: vantage claim-tab <registration id> <process id>";

    public static string ForceRemoveWarning => "This deletes the folder below and everything in it that is not committed and pushed.\nTo confirm, give its name:";

    public static string Describe(Footer footer)
    {
        return footer switch
        {
            Footer.Blank => string.Empty,
            Footer.NeedInput need => $"{need.Count} need input",
            Footer.RemoveHint => "d removes only what is safe to remove",
            Footer.Unhealthy unhealthy => $"{unhealthy.FolderName} is {unhealthy.Kind.ToString().ToLowerInvariant()}: {unhealthy.Reason}.",
            Footer.NoSessionSelected => "No session selected.",
            Footer.RunsInOwnTerminal { HasTranscript: true } terminal =>
                $"{terminal.Name} runs in its own terminal. Press b to move it under Vantage (its tab becomes a plain shell), or type /bg there.",
            Footer.RunsInOwnTerminal terminal =>
                $"{terminal.Name} runs in its own terminal and has no messages yet. d closes its tab, C closes every such tab, or send it something there and press b.",
            Footer.OpenInAnotherTab open => $"{open.Name} is open in another tab; press o to switch to it.",
            Footer.NeverSentAMessage never => $"{never.Name} has nothing to resume: it was never sent a message. d deletes it.",
            Footer.NoEmptyTerminals => "No terminal sessions without messages.",
            Footer.NoMessagesToRename rename => $"{rename.Name} has no messages yet, so it cannot be renamed; send it something first.",
            Footer.WorkingSoRenameWaits rename => $"{rename.Name} is working; wait until it is idle or needs input, because renaming restarts it.",
            Footer.RenameCancelled => "Rename cancelled.",
            Footer.InvalidName invalid => Describe(invalid.Problem),
            Footer.AlreadyUnderVantage adopted => $"{adopted.Name} already runs in the background under Vantage.",
            Footer.WorkingSoAdoptWaits adopt => $"{adopt.Name} is working; wait until it is idle or needs input, so nothing in flight is lost.",
            _ => DescribeWorktreesAndScreen(footer),
        };
    }

    /// <summary>Why a typed session name is refused, in the rename footer, the wizard and vantage here alike.</summary>
    public static string Describe(NameProblem problem)
    {
        return problem switch
        {
            NameProblem.Missing => "A session name is required.",
            NameProblem.UnsafeCharacters => "A session name cannot contain a semicolon or a double quote, because Windows Terminal cannot pass them safely.",
        };
    }

    /// <summary>Why a launch is refused, in the wizard and from vantage here; the command line's refusals end with its usage line.</summary>
    public static string Describe(LaunchProblem problem)
    {
        return problem switch
        {
            LaunchProblem.InvalidName invalid => Describe(invalid.Problem),
            LaunchProblem.WorktreeNeedsRepository => "A worktree needs a repository; choose one or start on the current branch.",
            LaunchProblem.WorktreeNeedsLetters => "A worktree needs a name with some letters or digits in it.",
            LaunchProblem.OptionNeedsValue missing => $"{missing.Option} needs a value. {HereUsage}",
            LaunchProblem.UnknownOption unknown => $"Unknown option {unknown.Option}. {HereUsage}",
            LaunchProblem.UnexpectedArgument unexpected => $"Unexpected argument {unexpected.Argument}. {HereUsage}",
            _ => problem.ToString(),
        };
    }

    /// <summary>Why the wizard will not go on.</summary>
    public static string Describe(WizardProblem problem)
    {
        return problem switch
        {
            WizardProblem.NoRepository => "Choose a repository: type part of its name, then ↑↓.",
            WizardProblem.NoName => "Session name is required: a short, human-readable label for the session.",
            WizardProblem.Refused refused => Describe(refused.Problem),
            _ => problem.ToString(),
        };
    }

    /// <summary>What the footer says, beside the busy spinner, while an effect runs off the UI thread.</summary>
    public static string Working(ViewEffect effect)
    {
        return effect switch
        {
            OpenTabForSession open => $"Opening a tab for {open.Session.Name}...",
            StopSession => "Stopping...",
            AdoptSession adopt => $"Moving {adopt.Session.Name} under Vantage...",
            RenameSession rename => $"Renaming {rename.Session.Name} to {rename.NewName.Text}...",
            CloseEmptyTerminals => "Closing...",
            ResumeSessions resume => $"Resuming {resume.Sessions.Count}...",
            DeleteSession deletion => $"Deleting {deletion.Session.Name}...",
            DeleteBranch branch => $"Deleting branch {branch.Branch.Branch}...",
            RemoveWorktree remove => $"Removing {remove.Row.FolderName}...",
            PruneRepository => "Pruning...",
            OpenShellTab shell => $"Opening a shell in {shell.Row.FolderName}...",
            StartSession start => $"Starting {start.Plan.Name.Text}...",
            _ => "Working...",
        };
    }

    public static string WizardTitle(WizardState wizard) => wizard.FixedChoice is null ? "New session" : $"New session in {wizard.FixedChoice.Name}";

    public static string WizardChosenCaption(WizardState wizard) => wizard.ShowsChooser ? "Repository" : "Directory";

    public static string WizardChosen(WizardState wizard) => wizard.Chosen?.Name ?? "(none)";

    public static string WizardHint(WizardState wizard)
    {
        const string Fields = "Enter or Tab next field   F5 start   F6 start and open in tab   Esc cancel";

        return wizard.ShowsChooser ? $"Type to filter, ↑↓ to choose, Enter confirms the repository   {Fields}" : Fields;
    }

    public static string RenamePrompt(string name) => $"Rename {name}. The session restarts under the new name and keeps its conversation.";

    public static string DeleteSessionQuestion(string name) => $"Delete {name}?\nIts worktree is removed only when nothing in it would be lost.";

    public static string CloseEmptyQuestion(IReadOnlyList<string> names)
    {
        string listed = string.Join(", ", names);
        string what = names.Count == 1 ? $"Close the tab of {listed}?" : $"Close {names.Count} tabs: {listed}?";

        return $"{what}\nNone of them has been sent a message, so nothing is lost.";
    }

    public static string DeleteBranchQuestion(string branch, bool createdByVantage)
    {
        return createdByVantage
            ? $"Branch {branch} is not merged. Delete it anyway?"
            : $"Branch {branch} was not created by Vantage. Delete it anyway?";
    }

    private static string DescribeWorktreesAndScreen(Footer footer)
    {
        return footer switch
        {
            Footer.NoWorktreeSelected => "No worktree selected.",
            Footer.NotRemoved { OfferForce: true } refused => $"Not removed: {refused.Reason}. D forces after a typed confirmation.",
            Footer.NotRemoved refused => $"Not removed: {refused.Reason}.",
            Footer.ForceCancelled => "Cancelled; nothing removed.",
            Footer.NothingTyped => "Nothing was typed; nothing removed.",
            Footer.TypedWrongName wrong => $"You typed {wrong.Typed}, not {wrong.Expected}; nothing removed.",
            Footer.NothingToPrune prune => $"Nothing to prune: the repository {prune.RepositoryName} no longer exists.",
            Footer.LeftoverFolder leftover => $"{leftover.FolderName} is a leftover folder, not a worktree.",
            Footer.NoSessionIn none => $"No session is running in {none.FolderName}.",
            Footer.Kept => "Kept.",
            Footer.NothingChosenToResume => "Nothing to resume.",
            Footer.NoDialogFor missing => $"No dialog is defined for {missing.EffectName}.",
            Footer.Working working => Working(working.Effect),
            Footer.Reported reported => Describe(reported.Outcome),
            Footer.BranchKept kept => $"{Describe(kept.Removal)} Branch kept.",
            Footer.Followed followed => $"{Describe(followed.First)} {Describe(followed.Then)}",
            Footer.Failed failed => $"Failed: {failed.Message}",
            _ => footer.ToString(),
        };
    }
}