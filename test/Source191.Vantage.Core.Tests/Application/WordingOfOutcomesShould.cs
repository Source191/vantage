using Source191.Vantage.Application;
using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Launch;
using Source191.Vantage.Core.Tests.Application.Actions;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Application;

/// <summary>
/// Every sentence a handler or <c>vantage here</c> reports, verbatim, with the outcome that produces it: one fact per
/// outcome case and per pluralisation. A new outcome case needs a fact here.
/// </summary>
public sealed class WordingOfOutcomesShould
{
    private static readonly BranchToConfirm Branch = new(FolderPath.From(@"C:\repos\Northwind.Orders"), BranchName.From("fix-123"));

    public static TheoryData<Type> OutcomeCases =>
        [.. typeof(Outcome).Assembly.GetTypes().Where(type => !type.IsAbstract && type.IsAssignableTo(typeof(Outcome)))];

    [Theory]
    [MemberData(nameof(OutcomeCases))]
    public void HaveWordsForEveryOutcomeCase(Type type)
    {
        System.Reflection.ConstructorInfo constructor = type.GetConstructors().Single(candidate => candidate.GetParameters().All(parameter => parameter.ParameterType != type));
        object[] arguments = [.. constructor.GetParameters().Select(parameter => Placeholder(parameter.ParameterType))];
        Outcome outcome = (Outcome)constructor.Invoke(arguments);

        Wording.Describe(outcome).ShouldNotBe(outcome.ToString(), $"{type.Name} has no arm in Wording.Describe");
    }

    [Fact]
    public void SayTheUpdateStarted()
    {
        Wording.Describe(new UpdateStartedOutcome("0.10.0")).ShouldBe("Updating to 0.10.0 in a new tab; Vantage starts again there when it is done.");
    }

    [Fact]
    public void SaySettingsSaved()
    {
        Wording.Describe(new SaveSettingsOutcome()).ShouldBe("Settings saved.");
    }

    [Fact]
    public void SayWhetherTheUpdateCheckIsOn()
    {
        Wording.Describe(new ToggleUpdateCheckOutcome(On: true)).ShouldBe("The update check is on: Vantage asks nuget.org at most once a day.");
        Wording.Describe(new ToggleUpdateCheckOutcome(On: false)).ShouldBe("The update check is off.");
    }

    // Sessions.

    [Fact]
    public void SayASessionStopped()
    {
        Wording.Describe(new StopSessionOutcome("fix-123", TabClosed: false)).ShouldBe("Stopped fix-123.");
        Wording.Describe(new StopSessionOutcome("fix-123", TabClosed: true)).ShouldBe("Stopped fix-123 and closed its tab.");
    }


    [Fact]
    public void SayThereIsNothingToMoveWithoutMessages()
    {
        Wording.Describe(new AdoptSessionOutcome.NoMessages("repos-f6"))
            .ShouldBe("repos-f6 has no messages yet, so there is nothing to move. Send it something in its tab first, or close that tab and start a session from Vantage.");
    }

    [Fact]
    public void SayASessionMovedUnderVantage()
    {
        Wording.Describe(new AdoptSessionOutcome.Moved("repos-31", TabClosed: false)).ShouldBe("Moved repos-31 under Vantage.");
        Wording.Describe(new AdoptSessionOutcome.Moved("repos-31", TabClosed: true)).ShouldBe("Moved repos-31 under Vantage and closed its old tab.");
    }

    [Fact]
    public void SayWhyAMoveFailed()
    {
        Wording.Describe(new AdoptSessionOutcome.Failed("repos-31", "it started a copy")).ShouldBe("Could not move repos-31 under Vantage: it started a copy");
    }

    [Fact]
    public void RefuseARenameWithoutMessages()
    {
        Wording.Describe(new RenameSessionOutcome.NoMessages("fix-123")).ShouldBe("fix-123 has no messages yet, so it cannot be renamed; send it something first.");
    }

    [Fact]
    public void SayASessionWasRenamed()
    {
        Wording.Describe(new RenameSessionOutcome.Renamed("fix-123", "fix-124", TabMoved: false)).ShouldBe("Renamed fix-123 to fix-124; it restarts idle and keeps its conversation.");
        Wording.Describe(new RenameSessionOutcome.Renamed("fix-123", "fix-124", TabMoved: true)).ShouldBe("Renamed fix-123 to fix-124 and moved its tab; it restarts idle and keeps its conversation.");
    }

    [Fact]
    public void SayAFailedRenameWasPutBack()
    {
        Wording.Describe(new RenameSessionOutcome.Restored("fix-123", "boom", TabClosed: false)).ShouldBe("Could not rename fix-123: boom; it is back under its old name.");
        Wording.Describe(new RenameSessionOutcome.Restored("fix-123", "boom", TabClosed: true)).ShouldBe("Could not rename fix-123: boom; it is back under its old name and its tab is closed: Enter opens a new one.");
    }

    [Fact]
    public void SayHowToResumeAfterARenameAndItsRestoreBothFailed()
    {
        Wording.Describe(new RenameSessionOutcome.Unsupervised("fix-123", "boom", SessionId.From("sess-1"), "bang"))
            .ShouldBe("Could not rename fix-123: boom; resume it with claude --bg --resume sess-1 (bang).");
    }

    [Fact]
    public void NameTheOneTabClosed()
    {
        Wording.Describe(new CloseEmptyTerminalsOutcome(["repos-f6"], [], [], [])).ShouldBe("Closed 1 tab: repos-f6.");
    }

    [Fact]
    public void NameTheTabsClosed()
    {
        Wording.Describe(new CloseEmptyTerminalsOutcome(["repos-f6", "repos-a1"], [], [], [])).ShouldBe("Closed 2 tabs: repos-f6, repos-a1.");
    }

    [Fact]
    public void NameTheOneSessionEndedWithoutATab()
    {
        Wording.Describe(new CloseEmptyTerminalsOutcome([], ["repos-f6"], [], []))
            .ShouldBe("Ended 1 session: repos-f6 (not in a Windows Terminal tab, so nothing to close).");
    }

    [Fact]
    public void NameTheSessionsEndedWithoutATab()
    {
        Wording.Describe(new CloseEmptyTerminalsOutcome([], ["repos-f6", "repos-a1"], [], []))
            .ShouldBe("Ended 2 sessions: repos-f6, repos-a1 (not in a Windows Terminal tab, so nothing to close).");
    }

    [Fact]
    public void SayThereWasNothingToClose()
    {
        Wording.Describe(new CloseEmptyTerminalsOutcome([], [], [], [])).ShouldBe("Nothing to close.");
    }

    [Fact]
    public void SayWhyNothingWasClosed()
    {
        Wording.Describe(new CloseEmptyTerminalsOutcome([], [], [new SkippedSession("repos-31")], []))
            .ShouldBe("Nothing closed: repos-31 has messages (press b to move it under Vantage).");
    }

    [Fact]
    public void NameWhatWasClosedEndedAndSkipped()
    {
        Wording.Describe(new CloseEmptyTerminalsOutcome(["repos-f6"], ["repos-a1"], [new SkippedSession("repos-93")], []))
            .ShouldBe("Closed 1 tab: repos-f6. Ended 1 session: repos-a1 (not in a Windows Terminal tab, so nothing to close). Skipped: repos-93 has messages (press b to move it under Vantage).");
    }

    [Fact]
    public void SayWhyNothingWasClosedWhenAProcessIdWasReused()
    {
        Wording.Describe(new CloseEmptyTerminalsOutcome([], [], [], ["repos-f6"]))
            .ShouldBe("Nothing closed: repos-f6 no longer runs under its listed process (refresh and try again).");
    }

    [Fact]
    public void NameASessionLeftAloneBesideTheOnesClosedAndSkipped()
    {
        Wording.Describe(new CloseEmptyTerminalsOutcome(["repos-f6"], [], [new SkippedSession("repos-93")], ["repos-a1"]))
            .ShouldBe("Closed 1 tab: repos-f6. Skipped: repos-93 has messages (press b to move it under Vantage); repos-a1 no longer runs under its listed process (refresh and try again).");
    }

    [Fact]
    public void SayAMoveWasRefusedBecauseTheProcessIdWasReused()
    {
        Wording.Describe(new AdoptSessionOutcome.ProcessGone("repos-31")).ShouldBe("Could not move repos-31: it no longer runs under its listed process. Refresh and try again.");
    }

    [Fact]
    public void CountTheSessionsResumed()
    {
        Wording.Describe(new ResumeSessionsOutcome(3, 3, [])).ShouldBe("Resumed 3 of 3, each in its own tab.");
    }

    [Fact]
    public void SayNoTabOpenedWhenNoSessionResumed()
    {
        Wording.Describe(new ResumeSessionsOutcome(0, 1, [new ResumeNote.NeverSentAMessage("repos-f6")]))
            .ShouldBe("Resumed 0 of 1. Notes: repos-f6: never sent a message, so there is nothing to resume; d deletes it");
    }

    [Fact]
    public void AddANoteForEachSessionThatDidNotResume()
    {
        Wording.Describe(new ResumeSessionsOutcome(1, 3, [new ResumeNote.NeverSentAMessage("repos-f6"), new ResumeNote.Failed("fix-123", "boom", SessionId.From("sess-1"))]))
            .ShouldBe("Resumed 1 of 3, each in its own tab. Notes: repos-f6: never sent a message, so there is nothing to resume; d deletes it; fix-123: boom; resume it with claude --bg --resume sess-1");
    }

    [Fact]
    public void GiveTheCommandThatBringsBackASessionWhoseResumeFailed()
    {
        Wording.Describe(new ResumeSessionsOutcome(0, 1, [new ResumeNote.Failed("fix-123", "Workspace not trusted", SessionId.From("sess-1"))]))
            .ShouldBe("Resumed 0 of 1. Notes: fix-123: Workspace not trusted; resume it with claude --bg --resume sess-1");
    }

    [Fact]
    public void SayASessionWasDeleted()
    {
        Wording.Describe(new DeleteSessionOutcome.Deleted("fix-123")).ShouldBe("Deleted fix-123.");
        Wording.Describe(new DeleteSessionOutcome.Deleted("fix-123") { TabClosed = true }).ShouldBe("Deleted fix-123 and closed its tab.");
        Wording.Describe(new DeleteSessionOutcome.RemovedWorktreeAndBranch("fix-123", BranchName.From("fix-123")) { TabClosed = true }).ShouldBe("Deleted fix-123 and closed its tab, and removed the worktree and branch fix-123.");
    }

    [Fact]
    public void SayWhyADeletedSessionsWorktreeWasKept()
    {
        Wording.Describe(new DeleteSessionOutcome.KeptWorktree("fix-123", new RemovalReason.ChangesWouldBeLost(1)))
            .ShouldBe("Deleted fix-123; kept its worktree because 1 changed or untracked files would be lost.");
    }

    [Fact]
    public void SayHowToDeleteAKeptWorktreeThatStillHoldsFiles()
    {
        Wording.Describe(new DeleteSessionOutcome.KeptWorktree("fix-123", new RemovalReason.LeftoverFilesWouldBeLost(12, AtLeast: false)))
            .ShouldBe("Deleted fix-123; kept its worktree because 12 files that git no longer tracks would be lost; force remove (D) on the Worktrees tab deletes it.");
    }

    [Fact]
    public void SayALeftoverOutsideTheRootsWasNotRemoved()
    {
        Wording.Describe(new RemoveWorktreeOutcome.Refused(new RemovalReason.OutsideTheRoots(FolderPath.From(@"D:\Northwind.Archive"))))
            .ShouldBe(@"Not removed: D:\Northwind.Archive is outside the repository roots and the worktree root, so it is not deleted.");
    }

    [Fact]
    public void SayADeletedSessionsWorktreeAndBranchWentToo()
    {
        Wording.Describe(new DeleteSessionOutcome.RemovedWorktreeAndBranch("fix-123", BranchName.From("fix-123"))).ShouldBe("Deleted fix-123 and removed the worktree and branch fix-123.");
    }

    [Fact]
    public void SayADeletedSessionsBranchIsNotMerged()
    {
        Wording.Describe(new DeleteSessionOutcome.BranchNotMerged("fix-123", Branch)).ShouldBe("Deleted fix-123 and removed the worktree; branch fix-123 is not merged.");
    }

    [Fact]
    public void SayABranchWasDeleted()
    {
        Wording.Describe(new DeleteBranchOutcome()).ShouldBe("Branch deleted.");
    }

    // Worktrees.

    [Fact]
    public void GiveTheReasonARemovalWasRefused()
    {
        Wording.Describe(new RemoveWorktreeOutcome.Refused(new RemovalReason.ChangesWouldBeLost(3))).ShouldBe("Not removed: 3 changed or untracked files would be lost.");
    }

    [Fact]
    public void SayAWorktreeIsStillInUse()
    {
        Wording.Describe(new RemoveWorktreeOutcome.InUse("Northwind.Orders-fix-123")).ShouldBe("Not removed: the folder Northwind.Orders-fix-123 is still in use by a tab or program; close it and remove again.");
        Wording.Describe(new DeleteSessionOutcome.WorktreeInUse("fix-123", "Northwind.Orders-fix-123"))
            .ShouldBe("Deleted fix-123; its worktree folder Northwind.Orders-fix-123 is still in use by a tab or program: close it and remove the folder from the worktrees view.");
    }

    [Fact]
    public void SayABrokenWorktreeWasDeletedWithTheSession()
    {
        Wording.Describe(new DeleteSessionOutcome.RemovedWorktree("fix-123", new RemoveWorktreeOutcome.DeletedFolderAndPruned("Northwind.Orders-fix-123", 1, "Northwind.Orders")))
            .ShouldBe("Deleted fix-123; deleted the folder Northwind.Orders-fix-123 and pruned 1 stale registration from Northwind.Orders.");
    }

    [Fact]
    public void SayAWorktreeWasRemoved()
    {
        Wording.Describe(new RemoveWorktreeOutcome.Removed("Northwind.Orders-fix-123")).ShouldBe("Removed Northwind.Orders-fix-123.");
    }

    [Fact]
    public void SayAWorktreeAndItsMergedBranchWereRemoved()
    {
        Wording.Describe(new RemoveWorktreeOutcome.RemovedAndDeletedBranch("Northwind.Orders-fix-123", BranchName.From("fix-123")))
            .ShouldBe("Removed Northwind.Orders-fix-123 and deleted its merged branch fix-123.");
    }

    [Fact]
    public void SayAHandMadeBranchWasKept()
    {
        Wording.Describe(new RemoveWorktreeOutcome.BranchKept("Northwind.Orders-fix-123", Branch, CreatedByVantage: false))
            .ShouldBe("Removed Northwind.Orders-fix-123; branch fix-123 was not created by Vantage, so it is kept.");
    }

    [Fact]
    public void SayAnUnmergedVantageBranchWasKept()
    {
        Wording.Describe(new RemoveWorktreeOutcome.BranchKept("Northwind.Orders-fix-123", Branch, CreatedByVantage: true))
            .ShouldBe("Removed Northwind.Orders-fix-123; branch fix-123 is not merged.");
    }

    [Fact]
    public void SayALeftoverFolderWasDeleted()
    {
        Wording.Describe(new RemoveWorktreeOutcome.DeletedFolder("fix-service-tests")).ShouldBe("Deleted the folder fix-service-tests.");
    }

    [Fact]
    public void SayALeftoverFolderWasDeletedAndItsRegistrationPruned()
    {
        Wording.Describe(new RemoveWorktreeOutcome.DeletedFolderAndPruned("Northwind.Orders-stale", 1, "Northwind.Orders"))
            .ShouldBe("Deleted the folder Northwind.Orders-stale and pruned 1 stale registration from Northwind.Orders.");
    }

    [Fact]
    public void CountOnePrunedRegistration()
    {
        Wording.Describe(new PruneRepositoryOutcome(1, "Northwind.Orders")).ShouldBe("Pruned 1 stale registration from Northwind.Orders.");
    }

    [Fact]
    public void CountThePrunedRegistrations()
    {
        Wording.Describe(new PruneRepositoryOutcome(2, "Northwind.Orders")).ShouldBe("Pruned 2 stale registrations from Northwind.Orders.");
    }

    [Fact]
    public void SayAShellWasOpened()
    {
        Wording.Describe(new OpenShellOutcome("Northwind.Orders-fix-123")).ShouldBe("Opened a shell in Northwind.Orders-fix-123.");
    }

    // Folder keys.

    [Fact]
    public void SayAFolderPathWasCopied()
    {
        Wording.Describe(new FolderPathCopied(@"C:\Users\me\source\worktrees\Northwind.Orders-fix-123"))
            .ShouldBe(@"Copied C:\Users\me\source\worktrees\Northwind.Orders-fix-123 to the clipboard.");
    }

    [Fact]
    public void SayTheClipboardWasBusy()
    {
        Wording.Describe(new ClipboardBusy()).ShouldBe("The clipboard is in use by another program; try again.");
    }

    [Fact]
    public void SayWhichEditorOpenedWhichFolder()
    {
        Wording.Describe(new OpenInEditorOutcome.Opened("Northwind.Orders", EditorChoice.VsCode)).ShouldBe("Opened Northwind.Orders in VS Code.");
        Wording.Describe(new OpenInEditorOutcome.Opened("Northwind.Orders", EditorChoice.Of(EditorPreset.Rider))).ShouldBe("Opened Northwind.Orders in Rider.");
        Wording.Describe(new OpenInEditorOutcome.Opened("Northwind.Orders", EditorChoice.Custom(@"C:\Tools\subl.exe", ["{folder}"]))).ShouldBe("Opened Northwind.Orders in subl.");
    }

    [Fact]
    public void SayWhichEditorCouldNotBeFound()
    {
        Wording.Describe(new OpenInEditorOutcome.EditorNotFound(EditorChoice.Of(EditorPreset.Rider)))
            .ShouldBe("Could not find Rider; choose another in Open folder with on the Config tab (3).");
        Wording.Describe(new OpenInEditorOutcome.EditorNotFound(EditorChoice.Custom(@"C:\Tools\subl.exe", ["{folder}"])))
            .ShouldBe("Could not find subl; choose another in Open folder with on the Config tab (3).");
    }

    [Fact]
    public void SayWhichEditorWouldNotStartAndWhy()
    {
        Wording.Describe(new OpenInEditorOutcome.EditorNotStarted(EditorChoice.Of(EditorPreset.Rider), "Access is denied."))
            .ShouldBe("Rider would not start: Access is denied. Choose another in Open folder with on the Config tab (3).");
        Wording.Describe(new OpenInEditorOutcome.EditorNotStarted(EditorChoice.Custom(@"C:\Tools\subl.exe", ["{folder}"]), "The specified executable is not a valid application for this OS platform"))
            .ShouldBe("subl would not start: The specified executable is not a valid application for this OS platform. Choose another in Open folder with on the Config tab (3).");
    }

    [Fact]
    public void SayTheFolderIsGone()
    {
        Wording.Describe(new OpenInEditorOutcome.FolderMissing("Northwind.Orders-fix-123")).ShouldBe("Northwind.Orders-fix-123 no longer exists, so nothing was opened.");
    }
    [Fact]
    public void SayASolutionWasOpened()
    {
        Wording.Describe(new OpenedSolution("Northwind.Orders.slnx")).ShouldBe("Opened Northwind.Orders.slnx.");
    }

    [Fact]
    public void SayThePullRequestWasOpened()
    {
        Wording.Describe(new OpenPullRequestOutcome(PullRequestHost.AzureDevOps, 4521)).ShouldBe("Opened !4521 in the browser.");
        Wording.Describe(new OpenPullRequestOutcome(PullRequestHost.GitHub, 123)).ShouldBe("Opened #123 in the browser.");
    }
    // Tabs and starting.


    [Fact]
    public void NameTheColourOfTheNewTab()
    {
        Wording.Describe(new OpenTabOutcome.Opened("Fix flaky tests", "Red", ColourReused: false)).ShouldBe("Opened Fix flaky tests in a red tab.");
    }

    [Fact]
    public void WarnWhenATabColourWasReused()
    {
        Wording.Describe(new OpenTabOutcome.Opened("Fix flaky tests", "Red", ColourReused: true))
            .ShouldBe("Opened Fix flaky tests in a red tab (every palette colour is in use, so one was reused).");
    }

    [Fact]
    public void SayTheOpenTabWasSelected()
    {
        Wording.Describe(new OpenTabOutcome.SwitchedTo("Refund retry policy")).ShouldBe("Switched to the tab of Refund retry policy.");
    }

    [Fact]
    public void SayTheOpenTabCouldNotBeFound()
    {
        Wording.Describe(new OpenTabOutcome.NotFound("Refund retry policy"))
            .ShouldBe("Refund retry policy is open in another tab, but Vantage could not find it. Is it in another window?");
    }


    [Fact]
    public void SayASessionStartedAndWhereItsTabOpened()
    {
        Wording.Describe(new StartSessionOutcome.StartedAndOpened("Fix flaky tests", JobId.From("81cdb0f9"), new OpenTabOutcome.Opened("Fix flaky tests", "Red", ColourReused: false)))
            .ShouldBe("Started Fix flaky tests (81cdb0f9). Opened Fix flaky tests in a red tab.");
    }

    [Fact]
    public void SayHowToOpenATabForASessionNotListedYet()
    {
        Wording.Describe(new StartSessionOutcome.NotListedYet("Fix flaky tests", JobId.From("81cdb0f9")))
            .ShouldBe("Started Fix flaky tests (81cdb0f9). It is not listed yet: press Enter on it to open its tab.");
    }

    [Fact]
    public void ExplainTheTrustTab()
    {
        Wording.Describe(new StartSessionOutcome.NotTrusted(FolderPath.From(@"C:\repos\Northwind.Orders")))
            .ShouldBe("Northwind.Orders is not a trusted workspace yet. A Claude tab was opened there: accept the trust prompt, exit, then retry.");
    }

    [Fact]
    public void SayWhyAStartFailed()
    {
        Wording.Describe(new StartSessionOutcome.Failed("supervisor not running")).ShouldBe("Could not start: supervisor not running");
    }

    // vantage here.

    [Fact]
    public void SayWhereVantageHereStartedTheSession()
    {
        Wording.Here(new LaunchOutcome.Listed(JobId.From("81cdb0f9"), SessionTestData.Job(@"C:\repos\Northwind.Orders")), "fix-tests", @"C:\repos\Northwind.Orders")
            .ShouldBe(@"Started fix-tests (81cdb0f9) in C:\repos\Northwind.Orders.");
    }

    [Fact]
    public void SayWhereVantageHereStartedTheSessionBeforeItIsListed()
    {
        Wording.Here(new LaunchOutcome.NotListedYet(JobId.From("81cdb0f9")), "fix-tests", @"C:\repos\Northwind.Orders")
            .ShouldBe(@"Started fix-tests (81cdb0f9) in C:\repos\Northwind.Orders.");
    }

    [Fact]
    public void TellVantageHereToTrustTheFolderFirst()
    {
        Wording.Here(new LaunchOutcome.WorkspaceNotTrusted(), "fix-tests", @"C:\repos\Northwind.Orders")
            .ShouldBe(@"Could not start: C:\repos\Northwind.Orders is not trusted yet. Run claude here once and accept the prompt.");
    }

    [Fact]
    public void SayWhyVantageHereCouldNotStart()
    {
        Wording.Here(new LaunchOutcome.Failed("something broke"), "fix-tests", @"C:\repos\Northwind.Orders")
            .ShouldBe("Could not start: something broke");
    }

    [Fact]
    public void SayWhatBecameOfAProblemReport()
    {
        Wording.Describe(new ProblemReportOutcome.Opened()).ShouldBe("Opened a problem report in your browser; describe the problem there and submit it.");
        Wording.Describe(new ProblemReportOutcome.Copied()).ShouldBe("Copied the problem report to the clipboard.");
        Wording.Describe(new ProblemReportOutcome.NotOpened("No application is associated with the address."))
            .ShouldBe("Could not open the browser: No application is associated with the address. Press ! and choose Copy text instead.");
    }

    private static object Placeholder(Type type)
    {
        return type switch
        {
            _ when type == typeof(string) => "fix-123",
            _ when type == typeof(RemovalReason) => new RemovalReason.Locked(),
            _ when type == typeof(int) => 2,
            _ when type == typeof(bool) => true,
            _ when type == typeof(BranchToConfirm) => Branch,
            _ when type == typeof(EditorChoice) => EditorChoice.VsCode,
            _ when type == typeof(FolderPath) => FolderPath.From(@"C:\repos\Northwind.Orders"),
            _ when type == typeof(JobId) => JobId.From("81cdb0f9"),
            _ when type == typeof(SessionId) => SessionId.From("sess-1"),
            _ when type == typeof(BranchName) => BranchName.From("fix-123"),
            _ when type == typeof(OpenTabOutcome) => new OpenTabOutcome.SwitchedTo("fix-123"),
            _ when type == typeof(RemoveWorktreeOutcome) => new RemoveWorktreeOutcome.DeletedFolder("Northwind.Orders-fix-123"),
            _ when type == typeof(IReadOnlyList<string>) => new List<string> { "fix-123" },
            _ when type == typeof(IReadOnlyList<SkippedSession>) => new List<SkippedSession>(),
            _ when type == typeof(IReadOnlyList<ResumeNote>) => new List<ResumeNote>(),
            _ when type == typeof(PullRequestHost) => PullRequestHost.AzureDevOps,
            _ => throw new InvalidOperationException($"An outcome field of type {type.Name} needs a placeholder."),
        };
    }
}