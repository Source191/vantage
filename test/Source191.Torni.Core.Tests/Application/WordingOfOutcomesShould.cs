using Source191.Torni.Application;
using Source191.Torni.Application.Actions;
using Source191.Torni.Application.Effects;
using Source191.Torni.Application.Launch;
using Source191.Torni.Core.Tests.Application.Actions;
using Source191.Torni.Domain;
using Source191.Torni.TestDoubles;

namespace Source191.Torni.Core.Tests.Application;

/// <summary>
/// Every sentence a handler or <c>torni here</c> reports, verbatim, with the outcome that produces it: one fact per
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
        Outcome outcome = (Outcome)Fixtures.Build(type);

        Wording.Describe(outcome).ShouldNotBe(outcome.ToString(), $"{type.Name} has no arm in Wording.Describe");
    }

    /// <summary>The comma key opened the settings dialog until the Config tab replaced it; no outcome may still send the user there.</summary>
    [Theory]
    [MemberData(nameof(OutcomeCases))]
    public void NeverOfferTheCommaKey(Type type)
    {
        Outcome outcome = (Outcome)Fixtures.Build(type);

        string words = Wording.Describe(outcome);

        words.ShouldNotContain("press ,", Case.Insensitive);
        words.ShouldNotContain(" , ");
        words.ShouldNotContain("in settings", Case.Insensitive);
    }

    [Fact]
    public void SayTheReleasePageOpened()
    {
        Wording.Describe(new OpenReleasePageOutcome.Opened("0.21.0")).ShouldBe("Opened the 0.21.0 release page in the browser.");
    }

    [Fact]
    public void SayWhyTheReleasePageDidNotOpen()
    {
        Wording.Describe(new OpenReleasePageOutcome.NotOpened("0.21.0", "PowerShell 7 (pwsh.exe) was not found on the PATH, so nothing can be opened."))
            .ShouldBe("The 0.21.0 release page did not open: PowerShell 7 (pwsh.exe) was not found on the PATH, so nothing can be opened.");
    }

    [Fact]
    public void SayTheReleasePageIsOpening()
    {
        Wording.Working(new Source191.Torni.Application.Views.OpenReleasePage(ReleaseVersions.Version("0.21.0"))).ShouldBe("Opening the 0.21.0 release page...");
    }

    [Fact]
    public void SayTheUpdateStarted()
    {
        Wording.Describe(new UpdateStartedOutcome("0.10.0")).ShouldBe("Updating to 0.10.0 in a new tab; Torni starts again there when it is done.");
    }

    [Fact]
    public void SayWingetIsMissingAndWhereToGetTheRelease()
    {
        Wording.Describe(new WingetMissingOutcome("0.21.0")).ShouldBe("winget was not found on the PATH, so Torni cannot install 0.21.0 itself: press U and choose Release page to download it.");
    }

    [Fact]
    public void SaySettingsSaved()
    {
        Wording.Describe(new SaveSettingsOutcome()).ShouldBe("Settings saved.");
    }

    [Fact]
    public void SayWhetherTheUpdateCheckIsOn()
    {
        Wording.Describe(new ToggleUpdateCheckOutcome(On: true)).ShouldBe("The update check is on: Torni asks the winget community repository at most once a day.");
        Wording.Describe(new ToggleUpdateCheckOutcome(On: false)).ShouldBe("The update check is off.");
    }

    // Sessions.

    [Fact]
    public void SayASessionStopped()
    {
        Wording.Describe(new StopSessionOutcome.Stopped(Listed.As("fix-123"), TabClosed: false)).ShouldBe("Stopped fix-123.");
        Wording.Describe(new StopSessionOutcome.Stopped(Listed.As("fix-123"), TabClosed: true)).ShouldBe("Stopped fix-123 and closed its tab.");
    }

    [Fact]
    public void SayThereIsNothingToMoveWithoutMessages()
    {
        Wording.Describe(new AdoptSessionOutcome.NoMessages(Listed.As("repos-f6")))
            .ShouldBe("repos-f6 has no messages yet, so there is nothing to move. Send it something in its tab first, or close that tab and start a session from Torni.");
    }

    [Fact]
    public void SayASessionMovedIntoTheBackground()
    {
        Wording.Describe(new AdoptSessionOutcome.Moved(Listed.As("repos-31"), TabClosed: false)).ShouldBe("Moved repos-31 under Torni.");
        Wording.Describe(new AdoptSessionOutcome.Moved(Listed.As("repos-31"), TabClosed: true)).ShouldBe("Moved repos-31 under Torni and closed its old tab.");
    }

    [Fact]
    public void SayWhyAMoveFailed()
    {
        Wording.Describe(new AdoptSessionOutcome.Failed(Listed.As("repos-31"), "it started a copy")).ShouldBe("Could not move repos-31 under Torni: it started a copy");
    }

    [Fact]
    public void RefuseARenameWithoutMessages()
    {
        Wording.Describe(new RenameSessionOutcome.NoMessages(Listed.As("fix-123"))).ShouldBe("fix-123 has no messages yet, so it cannot be renamed; send it something first.");
    }

    [Fact]
    public void SayASessionWasRenamed()
    {
        Wording.Describe(new RenameSessionOutcome.Renamed(Listed.As("fix-123"), SessionName.From("fix-124"), TabMoved: false)).ShouldBe("Renamed fix-123 to fix-124; it restarts idle and keeps its conversation.");
        Wording.Describe(new RenameSessionOutcome.Renamed(Listed.As("fix-123"), SessionName.From("fix-124"), TabMoved: true)).ShouldBe("Renamed fix-123 to fix-124 and moved its tab; it restarts idle and keeps its conversation.");
    }

    [Fact]
    public void SayAFailedRenameWasPutBack()
    {
        Wording.Describe(new RenameSessionOutcome.Restored(Listed.As("fix-123"), "boom", TabClosed: false)).ShouldBe("Could not rename fix-123: boom; it is back under its old name.");
        Wording.Describe(new RenameSessionOutcome.Restored(Listed.As("fix-123"), "boom", TabClosed: true)).ShouldBe("Could not rename fix-123: boom; it is back under its old name and its tab is closed: Enter opens a new one.");
    }

    [Fact]
    public void SayHowToResumeAfterARenameAndItsRestoreBothFailed()
    {
        Wording.Describe(new RenameSessionOutcome.Unsupervised(Listed.As("fix-123"), "boom", SessionId.From("sess-1"), "bang"))
            .ShouldBe("Could not rename fix-123: boom; resume it with claude --bg --resume sess-1 (bang).");
    }

    [Fact]
    public void NameTheOneTabClosed()
    {
        Wording.Describe(new CloseEmptyTerminalsOutcome([Listed.As("repos-f6")], [], [], [])).ShouldBe("Closed 1 tab: repos-f6.");
    }

    [Fact]
    public void NameTheTabsClosed()
    {
        Wording.Describe(new CloseEmptyTerminalsOutcome([Listed.As("repos-f6"), Listed.As("repos-a1")], [], [], [])).ShouldBe("Closed 2 tabs: repos-f6, repos-a1.");
    }

    [Fact]
    public void NameTheOneSessionEndedWithoutATab()
    {
        Wording.Describe(new CloseEmptyTerminalsOutcome([], [Listed.As("repos-f6")], [], []))
            .ShouldBe("Ended 1 session: repos-f6 (not in a Windows Terminal tab, so nothing to close).");
    }

    [Fact]
    public void NameTheSessionsEndedWithoutATab()
    {
        Wording.Describe(new CloseEmptyTerminalsOutcome([], [Listed.As("repos-f6"), Listed.As("repos-a1")], [], []))
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
        Wording.Describe(new CloseEmptyTerminalsOutcome([], [], [new SkippedSession(Listed.As("repos-31"))], []))
            .ShouldBe("Nothing closed: repos-31 has messages (press b to move it under Torni).");
    }

    [Fact]
    public void NameWhatWasClosedEndedAndSkipped()
    {
        Wording.Describe(new CloseEmptyTerminalsOutcome([Listed.As("repos-f6")], [Listed.As("repos-a1")], [new SkippedSession(Listed.As("repos-93"))], []))
            .ShouldBe("Closed 1 tab: repos-f6. Ended 1 session: repos-a1 (not in a Windows Terminal tab, so nothing to close). Skipped: repos-93 has messages (press b to move it under Torni).");
    }

    [Fact]
    public void SayWhyNothingWasClosedWhenAProcessIdWasReused()
    {
        Wording.Describe(new CloseEmptyTerminalsOutcome([], [], [], [Listed.As("repos-f6")]))
            .ShouldBe("Nothing closed: repos-f6 no longer runs under its listed process (refresh and try again).");
    }

    [Fact]
    public void NameASessionLeftAloneBesideTheOnesClosedAndSkipped()
    {
        Wording.Describe(new CloseEmptyTerminalsOutcome([Listed.As("repos-f6")], [], [new SkippedSession(Listed.As("repos-93"))], [Listed.As("repos-a1")]))
            .ShouldBe("Closed 1 tab: repos-f6. Skipped: repos-93 has messages (press b to move it under Torni); repos-a1 no longer runs under its listed process (refresh and try again).");
    }

    [Fact]
    public void SayAMoveWasRefusedBecauseTheProcessIdWasReused()
    {
        Wording.Describe(new AdoptSessionOutcome.ProcessGone(Listed.As("repos-31"))).ShouldBe("Could not move repos-31: it no longer runs under its listed process. Refresh and try again.");
    }

    [Fact]
    public void CountTheSessionsResumed()
    {
        Wording.Describe(new ResumeSessionsOutcome(3, 3, [])).ShouldBe("Resumed 3 of 3, each in its own tab.");
    }

    [Fact]
    public void SayNoTabOpenedWhenNoSessionResumed()
    {
        Wording.Describe(new ResumeSessionsOutcome(0, 1, [new ResumeNote.NeverSentAMessage(Listed.As("repos-f6"))]))
            .ShouldBe("Resumed 0 of 1. Notes: repos-f6: never sent a message, so there is nothing to resume; d deletes it");
    }

    [Fact]
    public void AddANoteForEachSessionThatDidNotResume()
    {
        Wording.Describe(new ResumeSessionsOutcome(1, 3, [new ResumeNote.NeverSentAMessage(Listed.As("repos-f6")), new ResumeNote.Failed(Listed.As("fix-123"), "boom", SessionId.From("sess-1"))]))
            .ShouldBe("Resumed 1 of 3, each in its own tab. Notes: repos-f6: never sent a message, so there is nothing to resume; d deletes it; fix-123: boom; resume it with claude --bg --resume sess-1");
    }

    [Fact]
    public void GiveTheCommandThatBringsBackASessionWhoseResumeFailed()
    {
        Wording.Describe(new ResumeSessionsOutcome(0, 1, [new ResumeNote.Failed(Listed.As("fix-123"), "Workspace not trusted", SessionId.From("sess-1"))]))
            .ShouldBe("Resumed 0 of 1. Notes: fix-123: Workspace not trusted; resume it with claude --bg --resume sess-1");
    }

    [Fact]
    public void SayASessionWasDeleted()
    {
        Wording.Describe(new DeleteSessionOutcome.Deleted(Listed.As("fix-123"), TabClosed: false)).ShouldBe("Deleted fix-123.");
        Wording.Describe(new DeleteSessionOutcome.Deleted(Listed.As("fix-123"), TabClosed: true)).ShouldBe("Deleted fix-123 and closed its tab.");
        Wording.Describe(new DeleteSessionOutcome.RemovedWorktreeAndBranch(Listed.As("fix-123"), BranchName.From("fix-123"), TabClosed: true)).ShouldBe("Deleted fix-123 and closed its tab, and removed the worktree and branch fix-123.");
    }

    [Fact]
    public void SayWhyADeletedSessionsWorktreeWasKept()
    {
        Wording.Describe(new DeleteSessionOutcome.KeptWorktree(Listed.As("fix-123"), new RemovalReason.ChangesWouldBeLost(1), TabClosed: false))
            .ShouldBe("Deleted fix-123; kept its worktree because 1 changed or untracked files would be lost.");
    }

    [Fact]
    public void SayHowToDeleteAKeptWorktreeThatStillHoldsFiles()
    {
        Wording.Describe(new DeleteSessionOutcome.KeptWorktree(Listed.As("fix-123"), new RemovalReason.LeftoverFilesWouldBeLost(12, AtLeast: false), TabClosed: false))
            .ShouldBe("Deleted fix-123; kept its worktree because 12 files that git no longer tracks would be lost; force remove (D) on the Worktrees tab deletes it.");
    }

    [Fact]
    public void SayAKeptWorktreeWhoseRepositoryHasGone()
    {
        Wording.Describe(new DeleteSessionOutcome.KeptWorktree(Listed.As("fix-123"), new RemovalReason.RepositoryGone(FolderPath.From(@"C:\Users\me\source\repos\Northwind.Orders")), TabClosed: false))
            .ShouldBe(@"Deleted fix-123; kept its worktree because its repository C:\Users\me\source\repos\Northwind.Orders no longer exists; remove it from the Worktrees tab.");
    }

    [Fact]
    public void SayAKeptWorktreeThatGitCouldNotInspect()
    {
        Wording.Describe(new DeleteSessionOutcome.KeptWorktree(Listed.As("fix-123"), new RemovalReason.GitFailed("fatal: not a git repository"), TabClosed: false))
            .ShouldBe("Deleted fix-123; kept its worktree because git could not inspect it: fatal: not a git repository; check it on the Worktrees tab.");
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
        Wording.Describe(new DeleteSessionOutcome.RemovedWorktreeAndBranch(Listed.As("fix-123"), BranchName.From("fix-123"), TabClosed: false)).ShouldBe("Deleted fix-123 and removed the worktree and branch fix-123.");
    }

    [Fact]
    public void SayADeletedSessionsBranchIsNotMerged()
    {
        Wording.Describe(new DeleteSessionOutcome.BranchNotMerged(Listed.As("fix-123"), Branch, TabClosed: false)).ShouldBe("Deleted fix-123 and removed the worktree; branch fix-123 is not merged.");
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
        Wording.Describe(new DeleteSessionOutcome.WorktreeInUse(Listed.As("fix-123"), "Northwind.Orders-fix-123", TabClosed: false))
            .ShouldBe("Deleted fix-123; its worktree folder Northwind.Orders-fix-123 is still in use by a tab or program: close it and remove the folder from the worktrees view.");
    }

    [Fact]
    public void SayABrokenWorktreeWasDeletedWithTheSession()
    {
        Wording.Describe(new DeleteSessionOutcome.RemovedWorktree(Listed.As("fix-123"), new RemoveWorktreeOutcome.DeletedFolderAndPruned("Northwind.Orders-fix-123", 1, "Northwind.Orders"), TabClosed: false))
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
        Wording.Describe(new RemoveWorktreeOutcome.BranchKept("Northwind.Orders-fix-123", Branch, CreatedHere: false))
            .ShouldBe("Removed Northwind.Orders-fix-123; branch fix-123 was not created by Torni, so it is kept.");
    }

    [Fact]
    public void SayAnUnmergedBranchCreatedHereWasKept()
    {
        Wording.Describe(new RemoveWorktreeOutcome.BranchKept("Northwind.Orders-fix-123", Branch, CreatedHere: true))
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
        Wording.Describe(new OpenSolutionOutcome.Opened("Northwind.Orders.slnx")).ShouldBe("Opened Northwind.Orders.slnx.");
    }

    [Fact]
    public void SayNoApplicationOpensTheSolutionsType()
    {
        Wording.Describe(new OpenSolutionOutcome.NoApplication(".slnx")).ShouldBe("No application opens .slnx files; install one, or choose one in Windows' Default apps.");
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
        Wording.Describe(new OpenTabOutcome.Opened(Listed.As("Fix flaky tests"), "Red", ColourReused: false)).ShouldBe("Opened Fix flaky tests in a red tab.");
    }

    [Fact]
    public void WarnWhenATabColourWasReused()
    {
        Wording.Describe(new OpenTabOutcome.Opened(Listed.As("Fix flaky tests"), "Red", ColourReused: true))
            .ShouldBe("Opened Fix flaky tests in a red tab (every palette colour is in use, so one was reused).");
    }

    [Fact]
    public void SayTheOpenTabWasSelected()
    {
        Wording.Describe(new OpenTabOutcome.SwitchedTo(Listed.As("Refund retry policy"))).ShouldBe("Switched to the tab of Refund retry policy.");
    }

    [Fact]
    public void SayTheOpenTabCouldNotBeFound()
    {
        Wording.Describe(new OpenTabOutcome.NotFound(Listed.As("Refund retry policy")))
            .ShouldBe("Refund retry policy is open in another tab, but Torni could not find it. Is it in another window?");
    }

    [Fact]
    public void SayASessionStartedAndWhereItsTabOpened()
    {
        Wording.Describe(new StartSessionOutcome.StartedAndOpened("Fix flaky tests", JobId.From("81cdb0f9"), new OpenTabOutcome.Opened(Listed.As("Fix flaky tests"), "Red", ColourReused: false)))
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

    // torni here.

    [Fact]
    public void SayWhereHereStartedTheSession()
    {
        Wording.Here(new LaunchOutcome.Listed(JobId.From("81cdb0f9"), SessionTestData.Job(@"C:\repos\Northwind.Orders")), "fix-tests", @"C:\repos\Northwind.Orders")
            .ShouldBe(@"Started fix-tests (81cdb0f9) in C:\repos\Northwind.Orders.");
    }

    [Fact]
    public void SayWhereHereStartedTheSessionBeforeItIsListed()
    {
        Wording.Here(new LaunchOutcome.NotListedYet(JobId.From("81cdb0f9")), "fix-tests", @"C:\repos\Northwind.Orders")
            .ShouldBe(@"Started fix-tests (81cdb0f9) in C:\repos\Northwind.Orders.");
    }

    [Fact]
    public void TellHereToTrustTheFolderFirst()
    {
        Wording.Here(new LaunchOutcome.WorkspaceNotTrusted(), "fix-tests", @"C:\repos\Northwind.Orders")
            .ShouldBe(@"Could not start: C:\repos\Northwind.Orders is not trusted yet. Run claude here once and accept the prompt.");
    }

    [Fact]
    public void SayWhyHereCouldNotStart()
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

    [Fact]
    public void SayAStopClaudeRefused()
    {
        Wording.Describe(new StopSessionOutcome.NotStopped(Listed.As("fix-123"), "Session 81cdb0f9 not found")).ShouldBe("Could not stop fix-123: Session 81cdb0f9 not found");
    }

    [Fact]
    public void SayARenameWhoseJobCouldNotBeRemoved()
    {
        Wording.Describe(new RenameSessionOutcome.NotRemoved(Listed.As("fix-123"), "Session 81cdb0f9 not found"))
            .ShouldBe("Could not rename fix-123: Session 81cdb0f9 not found; it is still listed under its old name.");
    }

    [Fact]
    public void NameASessionWhoseJobCouldNotBeRemovedWhenResuming()
    {
        Wording.Describe(new ResumeSessionsOutcome(0, 1, [new ResumeNote.NotRemoved(Listed.As("fix-123"), "Session 81cdb0f9 not found")]))
            .ShouldContain("fix-123: could not remove its job (Session 81cdb0f9 not found), so it was not resumed");
    }

    [Fact]
    public void SayADeleteClaudeRefused()
    {
        Wording.Describe(new DeleteSessionOutcome.NotDeleted(Listed.As("fix-123"), "Session 81cdb0f9 not found", TabClosed: false)).ShouldBe("Could not delete fix-123: Session 81cdb0f9 not found");
    }
}