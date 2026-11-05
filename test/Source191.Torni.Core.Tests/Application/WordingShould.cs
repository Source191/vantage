using Microsoft.Extensions.Time.Testing;
using Source191.Torni.Application;
using Source191.Torni.Application.Actions;
using Source191.Torni.Application.Diagnostics;
using Source191.Torni.Application.Launch;
using Source191.Torni.Application.Reporting;
using Source191.Torni.Application.TerminalSetup;
using Source191.Torni.Application.Views;
using Source191.Torni.Domain;
using Source191.Torni.Ports;
using Source191.Torni.TestDoubles;
using Sessions = Source191.Torni.Core.Tests.Application.Actions.SessionTestData;
using Worktrees = Source191.Torni.Core.Tests.Application.Actions.WorktreeTestData;

namespace Source191.Torni.Core.Tests.Application;

/// <summary>
/// Every sentence Torni shows, verbatim, with the facts that produce it: one fact per status message case, dialog question,
/// working text and help text. A new status message case needs a fact here; nothing else checks that its words exist.
/// </summary>
public sealed class WordingShould
{
    private const string FixedFolder = @"C:\Users\me\source\repos\Northwind.Orders-fix-123";

    private static readonly Session.Background Job = Sessions.Job(Sessions.Repository);
    private static readonly Session.Interactive Terminal = Sessions.Terminal("fix-123", 4242);
    private static readonly WorktreeRow Worktree = Worktrees.Row(WorktreeHealthKind.Ok);

    public static TheoryData<Type> StatusMessageCases => [.. typeof(StatusMessage).GetNestedTypes().Where(type => type.IsAssignableTo(typeof(StatusMessage)))];

    [Theory]
    [MemberData(nameof(StatusMessageCases))]
    public void HaveWordsForEveryStatusMessageCase(Type type)
    {
        StatusMessage message = (StatusMessage)Fixtures.Build(type);

        Wording.Describe(message).ShouldNotBe(message.ToString(), $"{type.Name} has no arm in Wording.Describe");
    }

    // Standing messages.

    [Fact]
    public void NameANamedSession()
    {
        Wording.Name(Listed.As("fix-123")).ShouldBe("fix-123");
    }

    [Fact]
    public void NameAnUnnamedSessionAsBlank()
    {
        Wording.Name(Listed.None()).ShouldBe(string.Empty);
    }

    [Fact]
    public void SayNothingForNoStatusMessage()
    {
        Wording.Describe(StatusMessage.None).ShouldBe(string.Empty);
    }

    // The sessions view.

    [Fact]
    public void SayNoSessionIsSelected()
    {
        Wording.Describe(new StatusMessage.NoSessionSelected()).ShouldBe("No session selected.");
    }

    [Fact]
    public void ExplainATerminalSessionWithMessages()
    {
        Wording.Describe(new StatusMessage.RunsInOwnTerminal(Listed.As("fix-123"), HasTranscript: true))
            .ShouldBe("fix-123 runs in its own terminal. Press b to move it under Torni (its old tab closes), or type /bg there.");
    }

    [Fact]
    public void ExplainATerminalSessionWithoutMessages()
    {
        Wording.Describe(new StatusMessage.RunsInOwnTerminal(Listed.As("fix-123"), HasTranscript: false))
            .ShouldBe("fix-123 runs in its own terminal and has no messages yet. d closes its tab, C closes every such tab, or send it something there and press b.");
    }

    [Fact]
    public void SayASessionNeverSentAMessageHasNothingToResume()
    {
        Wording.Describe(new StatusMessage.NeverSentAMessage(Listed.As("fix-123"))).ShouldBe("fix-123 has nothing to resume: it was never sent a message. d deletes it.");
    }

    [Fact]
    public void SayThereAreNoEmptyTerminals()
    {
        Wording.Describe(new StatusMessage.NoEmptyTerminals()).ShouldBe("No terminal sessions without messages.");
    }

    [Fact]
    public void RefuseToRenameASessionWithoutMessages()
    {
        Wording.Describe(new StatusMessage.NoMessagesToRename(Listed.As("fix-123"))).ShouldBe("fix-123 has no messages yet, so it cannot be renamed; send it something first.");
    }

    [Fact]
    public void WaitForAWorkingSessionBeforeRenaming()
    {
        Wording.Describe(new StatusMessage.WorkingSoRenameWaits(Listed.As("fix-123"))).ShouldBe("fix-123 is working; wait until it is idle or needs input, because renaming restarts it.");
    }

    [Fact]
    public void SayARenameWasCancelled()
    {
        Wording.Describe(new StatusMessage.RenameCancelled()).ShouldBe("Rename cancelled.");
    }

    [Fact]
    public void SayANameIsRequired()
    {
        Wording.Describe(NameProblem.Missing).ShouldBe("A session name is required.");
        Wording.Describe(new StatusMessage.InvalidName(NameProblem.Missing)).ShouldBe("A session name is required.");
    }

    [Fact]
    public void SayABackgroundSessionIsAlreadyInTheBackground()
    {
        Wording.Describe(new StatusMessage.AlreadyInBackground(Listed.As("fix-123"))).ShouldBe("fix-123 already runs in the background under Torni.");
    }

    [Fact]
    public void WaitForAWorkingSessionBeforeAdopting()
    {
        Wording.Describe(new StatusMessage.WorkingSoAdoptWaits(Listed.As("fix-123"))).ShouldBe("fix-123 is working; wait until it is idle or needs input, so nothing in flight is lost.");
    }

    // The worktrees view.

    [Fact]
    public void SayNoWorktreeIsSelected()
    {
        Wording.Describe(new StatusMessage.NoWorktreeSelected()).ShouldBe("No worktree selected.");
    }

    [Fact]
    public void GiveTheReasonARemovalWasRefusedAndOfferTheForceKey()
    {
        Wording.Describe(new StatusMessage.NotRemoved(new RemovalReason.ChangesWouldBeLost(3), OfferForce: true))
            .ShouldBe("Not removed: 3 changed or untracked files would be lost. D forces after a typed confirmation.");
    }

    [Fact]
    public void GiveTheReasonARemovalWasRefusedWithoutTheForceKey()
    {
        Wording.Describe(new StatusMessage.NotRemoved(new RemovalReason.Locked(), OfferForce: false))
            .ShouldBe("Not removed: the worktree is locked.");
    }

    [Fact]
    public void SayAForceRemovalWasCancelled()
    {
        Wording.Describe(new StatusMessage.ForceCancelled()).ShouldBe("Cancelled; nothing removed.");
    }

    [Fact]
    public void SayNothingWasTypedForAForceRemoval()
    {
        Wording.Describe(new StatusMessage.NothingTyped()).ShouldBe("Nothing was typed; nothing removed.");
    }

    [Fact]
    public void ShowWhatWasTypedWhenItIsNotTheName()
    {
        Wording.Describe(new StatusMessage.TypedWrongName("x", "Northwind.Orders-fix-123")).ShouldBe("You typed x, not Northwind.Orders-fix-123; nothing removed.");
    }

    [Fact]
    public void SayThereIsNothingToPruneWithoutARepository()
    {
        Wording.Describe(new StatusMessage.NothingToPrune("Northwind.Orders")).ShouldBe("Nothing to prune: the repository Northwind.Orders no longer exists.");
    }

    [Fact]
    public void SayALeftoverFolderIsNotAWorktree()
    {
        Wording.Describe(new StatusMessage.LeftoverFolder("fix-service-tests")).ShouldBe("fix-service-tests is a leftover folder, not a worktree.");
    }

    [Fact]
    public void SayNoSessionRunsInTheWorktree()
    {
        Wording.Describe(new StatusMessage.NoSessionIn("Northwind.Orders-fix-123")).ShouldBe("No session is running in Northwind.Orders-fix-123.");
        Wording.Describe(new StatusMessage.SessionNotListed(Listed.As("fix-123"))).ShouldBe("fix-123 is not in the sessions list any more; it may have ended. Try again after the next refresh.");
    }

    [Fact]
    public void SayAStoppedSessionHasNothingToMeasure()
    {
        Wording.Describe(new StatusMessage.NothingToMeasure(Listed.As("fix-123"))).ShouldBe("fix-123 has no process to measure; it is stopped.");
        Wording.Working(new ShowMemory(Job, 5151)).ShouldBe("Measuring fix-123...");
    }

    [Fact]
    public void TitleTheMemoryDialogWithTheSessionName()
    {
        Wording.MemoryDialogTitle(Listed.As("fix-123")).ShouldBe("Memory: fix-123");
    }

    [Fact]
    public void LayOutTheMemoryBreakdownAsAlignedLines()
    {
        MemoryBreakdown breakdown = new(
            Listed.As("fix-123"),
            MemorySize.FromMegabytes(296),
            [new MemoryShare("@azure-devops/mcp", MemorySize.FromMegabytes(100)), new MemoryShare("pwsh", MemorySize.FromMegabytes(78)), new MemoryShare("mcp-grafana", MemorySize.FromMegabytes(30))],
            MemorySize.FromMegabytes(504),
            16);

        Wording.MemoryBreakdownLines(breakdown).ShouldBe("""
            Claude Code         296 MB
            @azure-devops/mcp   100 MB
            pwsh                 78 MB
            mcp-grafana          30 MB

            Total               504 MB in 16 processes
            """.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void LayOutAMemoryBreakdownOfOneProcess()
    {
        MemoryBreakdown alone = new(Listed.As("fix-123"), MemorySize.FromMegabytes(296), [], MemorySize.FromMegabytes(296), 1);

        Wording.MemoryBreakdownLines(alone).ShouldBe("Claude Code   296 MB\n\nTotal         296 MB in 1 process");
    }

    [Fact]
    public void SayNoSolutionWasFound()
    {
        Wording.Describe(new StatusMessage.NoSolution("Northwind.Orders-fix-123")).ShouldBe("No solution found in Northwind.Orders-fix-123 or its src folder.");
    }

    [Fact]
    public void SayThePullRequestCell()
    {
        DateTimeOffset at = new(2026, 10, 20, 9, 0, 0, TimeSpan.Zero);
        PullRequest gitHub = new(PullRequestHost.GitHub, 123, new Uri("https://github.com/northwind/Northwind.Orders/pull/123"), PullRequestState.Open, at);
        PullRequest azure = new(PullRequestHost.AzureDevOps, 4521, new Uri("https://dev.azure.com/northwind/Trading/_git/Northwind.Orders/pullrequest/4521"), PullRequestState.Draft, at);

        Wording.PullRequestCell(new PullRequestLookup.Found(gitHub)).ShouldBe("#123 open");
        Wording.PullRequestCell(new PullRequestLookup.Found(gitHub with { Number = 88, State = PullRequestState.Merged })).ShouldBe("#88 merged");
        Wording.PullRequestCell(new PullRequestLookup.Found(gitHub with { State = PullRequestState.Closed })).ShouldBe("#123 closed");
        Wording.PullRequestCell(new PullRequestLookup.Found(gitHub with { State = PullRequestState.Draft })).ShouldBe("#123 draft");
        Wording.PullRequestCell(new PullRequestLookup.Found(azure)).ShouldBe("!4521 draft");
        Wording.PullRequestCell(new PullRequestLookup.Found(azure with { Number = 4460, State = PullRequestState.Completed })).ShouldBe("!4460 completed");
        Wording.PullRequestCell(new PullRequestLookup.Found(azure with { State = PullRequestState.Active })).ShouldBe("!4521 active");
        Wording.PullRequestCell(new PullRequestLookup.Found(azure with { State = PullRequestState.Abandoned })).ShouldBe("!4521 abandoned");
        Enum.GetValues<PullRequestGap>().ShouldAllBe(gap => Wording.PullRequestCell(new PullRequestLookup.Missing(gap)) == string.Empty);
    }

    [Theory]
    [InlineData(PullRequestGap.NoBranch, null, "fix-123 has no branch, so it has no pull request.")]
    [InlineData(PullRequestGap.NoRemote, null, "fix-123's repository has no origin remote to look up pull requests on.")]
    [InlineData(PullRequestGap.UnknownHost, null, "fix-123's origin is on neither GitHub nor Azure DevOps, so Torni cannot look up its pull requests.")]
    [InlineData(PullRequestGap.CliMissing, PullRequestHost.GitHub, "Install the GitHub CLI (gh) to see fix-123's pull request.")]
    [InlineData(PullRequestGap.CliMissing, PullRequestHost.AzureDevOps, "Install the Azure CLI (az) with the azure-devops extension to see fix-123's pull request.")]
    [InlineData(PullRequestGap.NotSignedIn, PullRequestHost.GitHub, "Sign in with gh auth login to see fix-123's pull request.")]
    [InlineData(PullRequestGap.NotSignedIn, PullRequestHost.AzureDevOps, "Sign in with az login to see fix-123's pull request.")]
    [InlineData(PullRequestGap.CliMissing, null, "Install the GitHub CLI (gh) or the Azure CLI (az) to see fix-123's pull request.")]
    [InlineData(PullRequestGap.NotSignedIn, null, "Sign in with gh auth login or az login to see fix-123's pull request.")]
    [InlineData(PullRequestGap.Looking, null, "Still looking up fix-123's pull request; try again in a moment.")]
    [InlineData(PullRequestGap.Failed, PullRequestHost.GitHub, "The GitHub CLI (gh) could not list fix-123's pull requests.")]
    [InlineData(PullRequestGap.Failed, null, "Could not look up fix-123's pull requests.")]
    [InlineData(PullRequestGap.NoPullRequest, PullRequestHost.AzureDevOps, "No pull request found for fix-123's branch.")]
    [InlineData(PullRequestGap.BranchNotGivenToAz, PullRequestHost.AzureDevOps, "fix-123's branch name holds a character the Azure CLI cannot be given, so it was not looked up.")]
    public void SayWhyThereIsNoPullRequest(PullRequestGap gap, PullRequestHost? host, string expected)
    {
        LookupHost lookupHost = host is PullRequestHost known ? new LookupHost.Known(known) : new LookupHost.NotRead();

        Wording.Describe(new StatusMessage.NoPullRequest("fix-123", gap, lookupHost)).ShouldBe(expected);
    }

    [Fact]
    public void SayAPullRequestIsOpening()
    {
        PullRequest azure = new(PullRequestHost.AzureDevOps, 4521, new Uri("https://dev.azure.com/northwind/Trading/_git/Northwind.Orders/pullrequest/4521"), PullRequestState.Active, DateTimeOffset.UnixEpoch);

        Wording.Working(new OpenPullRequest(azure)).ShouldBe("Opening !4521...");
    }
    // Dialogs and the screen.

    [Fact]
    public void SayItIsUpToDate()
    {
        Wording.Describe(new StatusMessage.UpToDate()).ShouldBe("Torni is up to date.");
    }

    [Fact]
    public void AnnounceANewlyFoundReleaseOnceWithTheKeyThatShowsWhatChanged()
    {
        Wording.Describe(new StatusMessage.NewerReleaseFound(ReleaseVersions.Version("0.21.0"))).ShouldBe("Torni 0.21.0 is available: press U to see what changed.");
        Wording.ToneOf(new StatusMessage.NewerReleaseFound(ReleaseVersions.Version("0.21.0"))).ShouldBe(StatusTone.Notice);
    }

    [Fact]
    public void SayTheUpdateCheckIsOff()
    {
        Wording.Describe(new StatusMessage.UpdateCheckOff()).ShouldBe("The update check is off, or this is a development build; nothing to update to.");
    }

    [Fact]
    public void SayTheUpdateTabIsOpening()
    {
        Wording.Working(new StartUpdate(ReleaseVersions.Version("0.10.0"))).ShouldBe("Opening a tab to update to 0.10.0...");
    }

    [Fact]
    public void SayTheEmptyTabsWereKept()
    {
        Wording.Describe(new StatusMessage.Kept()).ShouldBe("Kept.");
    }

    [Fact]
    public void SayNothingWasChosenToResume()
    {
        Wording.Describe(new StatusMessage.NothingToResume()).ShouldBe("Nothing to resume.");
    }

    [Fact]
    public void NameAnEffectWithNoDialog()
    {
        Wording.Describe(new StatusMessage.NoDialogFor("OpenSettings")).ShouldBe("No dialog is defined for OpenSettings.");
    }

    [Fact]
    public void SayWhatARunningEffectIsDoing()
    {
        Wording.Describe(new StatusMessage.Working(new StopSession(Job))).ShouldBe("Stopping...");
    }

    [Fact]
    public void SayWhatAHandlerDid()
    {
        Wording.Describe(new StatusMessage.Reported(new StopSessionOutcome.Stopped(Listed.As("fix-123"), TabClosed: false))).ShouldBe("Stopped fix-123.");
    }

    [Fact]
    public void SayTheBranchWasKeptAfterTheRemoval()
    {
        Wording.Describe(new StatusMessage.BranchKept(new DeleteSessionOutcome.BranchNotMerged(Listed.As("fix-123"), new BranchToConfirm(FolderPath.From(Sessions.Repository), BranchName.From("fix-123")), TabClosed: false)))
            .ShouldBe("Deleted fix-123 and removed the worktree; branch fix-123 is not merged. Branch kept.");
    }

    [Fact]
    public void SayTheBranchWasDeletedAfterTheRemoval()
    {
        Wording.Describe(new StatusMessage.Followed(new DeleteSessionOutcome.BranchNotMerged(Listed.As("fix-123"), new BranchToConfirm(FolderPath.From(Sessions.Repository), BranchName.From("fix-123")), TabClosed: false), new DeleteBranchOutcome()))
            .ShouldBe("Deleted fix-123 and removed the worktree; branch fix-123 is not merged. Branch deleted.");
    }

    [Fact]
    public void SayBackgroundWorkFailed()
    {
        Wording.Describe(new StatusMessage.Failed("git exited with 128")).ShouldBe("Failed: git exited with 128");
    }

    [Fact]
    public void AskForANewName()
    {
        Wording.RenamePrompt(Listed.As("fix-123")).ShouldBe("Rename fix-123. The session restarts under the new name and keeps its conversation.");
    }

    [Fact]
    public void AskBeforeDeletingASession()
    {
        Wording.DeleteSessionQuestion(Listed.As("fix-123")).ShouldBe("Delete fix-123?\nIts worktree is removed only when nothing in it would be lost.");
    }

    [Fact]
    public void AskBeforeClosingOneEmptyTab()
    {
        Wording.CloseEmptyQuestion([Listed.As("fix-123")]).ShouldBe("Close the tab of fix-123?\nNone of them has been sent a message, so nothing is lost.");
    }

    [Fact]
    public void AskBeforeClosingSeveralEmptyTabs()
    {
        Wording.CloseEmptyQuestion([Listed.As("fix-123"), Listed.As("repos-f6")]).ShouldBe("Close 2 tabs: fix-123, repos-f6?\nNone of them has been sent a message, so nothing is lost.");
    }

    [Fact]
    public void AskBeforeDeletingAnUnmergedBranchCreatedHere()
    {
        Wording.DeleteBranchQuestion("fix-123", createdHere: true).ShouldBe("Branch fix-123 is not merged. Delete it anyway?");
    }

    [Fact]
    public void AskBeforeDeletingABranchNotCreatedHere()
    {
        Wording.DeleteBranchQuestion("fix-123", createdHere: false).ShouldBe("Branch fix-123 was not created by Torni. Delete it anyway?");
    }

    [Fact]
    public void GiveEveryKeyATip()
    {
        foreach (KeyHelp key in Wording.SessionsKeys.Concat(Wording.WorktreesKeys))
        {
            key.Tip.ShouldNotBeNullOrWhiteSpace($"{key.Key} has no tip");
            key.Tip.Length.ShouldBeLessThanOrEqualTo(80, $"{key.Key}'s tip is too long");
            key.Tip.ShouldNotBe(key.Line, $"{key.Key}'s tip only repeats the help line");
        }
    }

    [Fact]
    public void FitEveryColumnMeaningOnItsLines()
    {
        foreach (ColumnHelp column in Wording.SessionsColumns.Concat(Wording.WorktreesColumns))
        {
            column.Meaning.Split('\n').ShouldAllBe(line => line.Length <= 100, $"{column.Name} has a line over 100 characters");
        }
    }

    [Fact]
    public void ReturnHintIsOneShortLine()
    {
        Wording.ReturnHint.ShouldBe("Ctrl+Z detaches; the session keeps running.");
        Wording.ReturnHint.Length.ShouldBeLessThanOrEqualTo(60);
    }

    [Fact]
    public void SayWhereToSupportIt()
    {
        Wording.SupportLine.ShouldBe("Buy me a sliver of a Claude Max plan: https://buymeacoffee.com/simonvane");
    }

    [Fact]
    public void WarnBeforeAForcedRemoval()
    {
        Wording.ForceRemoveWarning.ShouldBe("This deletes the folder below and everything in it that is not committed and pushed.\nTo confirm, give its name:");
    }

    // What the status line says while an effect runs.

    [Fact]
    public void SayWhatEachSessionEffectIsDoingWhileItRuns()
    {
        Wording.Working(new OpenTabForSession(Job, new LiveTab.None())).ShouldBe("Opening a tab for fix-123...");
        Wording.Working(new StopSession(Job)).ShouldBe("Stopping...");
        Wording.Working(new AdoptSession(Terminal)).ShouldBe("Moving fix-123 under Torni...");
        Wording.Working(new RenameSession(Job, SessionName.From("fix-124"))).ShouldBe("Renaming fix-123 to fix-124...");
        Wording.Working(new CloseEmptyTerminals([Terminal])).ShouldBe("Closing...");
        Wording.Working(new ResumeSessions([Job, Job])).ShouldBe("Resuming 2...");
        Wording.Working(new DeleteSession(Job)).ShouldBe("Deleting fix-123...");
        Wording.Working(new DeleteBranch(new BranchToConfirm(FolderPath.From(Sessions.Repository), BranchName.From("fix-123")))).ShouldBe("Deleting branch fix-123...");
    }

    [Fact]
    public void SayWhatEachWorktreeEffectIsDoingWhileItRuns()
    {
        Wording.Working(new RemoveWorktree(Worktree, Forced: false)).ShouldBe("Removing Northwind.Orders-fix-123...");
        Wording.Working(new PruneRepository(FolderPath.From(Worktrees.Repository))).ShouldBe("Pruning...");
        Wording.Working(new OpenShellTab(Worktree)).ShouldBe("Opening a shell in Northwind.Orders-fix-123...");
    }

    [Fact]
    public void SayWhatEachFolderEffectIsDoingWhileItRuns()
    {
        FolderPath folder = FolderPath.From(@"C:\Users\me\source\worktrees\Northwind.Orders-fix-123");

        Wording.Working(new CopyFolderPath(folder)).ShouldBe("Copying...");
        Wording.Working(new OpenFolderInEditor(folder)).ShouldBe("Opening Northwind.Orders-fix-123 in the editor...");
        Wording.Working(new FindSolution(folder, "Northwind.Orders")).ShouldBe("Looking for a solution in Northwind.Orders-fix-123...");
        Wording.Working(new OpenSolution(@"C:\Users\me\source\worktrees\Northwind.Orders-fix-123\Northwind.Orders.slnx")).ShouldBe("Opening Northwind.Orders.slnx...");
    }

    [Fact]
    public void AskWhichSolutionToOpen()
    {
        Wording.ChooseSolutionTitle.ShouldBe("Open solution");
        Wording.ChooseSolutionQuestion("Northwind.Orders-fix-123").ShouldBe("Northwind.Orders-fix-123 holds more than one solution. Which one?");
    }

    [Fact]
    public void NameTheSessionBeingStarted()
    {
        LaunchPlan plan = new(
            FolderPath.From(Sessions.Repository), SessionName.From("Fix flaky tests"), new LaunchPlace.CurrentBranch(),
            WorktreeBase.RemoteDefault, new OpeningPrompt.None(), new RepositoryChoice("Northwind.Orders", FolderPath.From(Sessions.Repository), IsRepository: true));

        Wording.Working(new StartSession(plan)).ShouldBe("Starting Fix flaky tests...");
    }

    [Fact]
    public void FallBackToAPlainWordForAnyOtherEffect()
    {
        Wording.Working(new Quit()).ShouldBe("Working...");
    }

    // Why a worktree cannot be removed, and what is wrong with it.

    public static TheoryData<Type> RemovalReasons => [.. typeof(RemovalReason).GetNestedTypes().Where(type => type.IsAssignableTo(typeof(RemovalReason)))];

    [Theory]
    [MemberData(nameof(RemovalReasons))]
    public void HaveWordsForEveryRemovalReason(Type type)
    {
        RemovalReason reason = (RemovalReason)Fixtures.Build(type);

        Wording.Describe(reason).ShouldNotBe(reason.ToString(), $"{type.Name} has no arm in Wording.Describe");
    }

    [Fact]
    public void NameTheSessionUsingTheWorktree()
    {
        Wording.Describe(new RemovalReason.LiveSession(Listed.As("fix-123"))).ShouldBe("session fix-123 is using it");
    }

    [Fact]
    public void SayTheWorktreeIsLocked()
    {
        Wording.Describe(new RemovalReason.Locked()).ShouldBe("the worktree is locked");
    }

    [Fact]
    public void CountTheLeftoverFilesThatWouldBeLost()
    {
        Wording.Describe(new RemovalReason.LeftoverFilesWouldBeLost(12, AtLeast: false)).ShouldBe("12 files that git no longer tracks would be lost");
    }

    [Fact]
    public void SayAtLeastWhenTheLeftoverFilesWereCountedUpToALimit()
    {
        Wording.Describe(new RemovalReason.LeftoverFilesWouldBeLost(1000, AtLeast: true)).ShouldBe("at least 1000 files that git no longer tracks would be lost");
    }

    [Fact]
    public void CountTheChangesThatWouldBeLost()
    {
        Wording.Describe(new RemovalReason.ChangesWouldBeLost(3)).ShouldBe("3 changed or untracked files would be lost");
    }

    [Fact]
    public void CountTheUnpushedCommitsOnTheBranch()
    {
        Wording.Describe(new RemovalReason.UnpushedCommits("fix-123", 2)).ShouldBe("branch fix-123 has 2 unpushed commits");
    }

    [Fact]
    public void SayAWorktreeOfARepositoryOutsideTheRootsIsNotRemoved()
    {
        Wording.Describe(new RemovalReason.RepositoryOutsideRoots()).ShouldBe("its repository is not under the roots; add that root in settings or remove the worktree with git there");
    }

    [Fact]
    public void SayALeftoverOutsideTheRootsIsNotDeleted()
    {
        Wording.Describe(new RemovalReason.OutsideTheRoots(FolderPath.From(@"D:\Northwind.Archive"))).ShouldBe(@"D:\Northwind.Archive is outside the repository roots and the worktree root, so it is not deleted");
    }

    [Fact]
    public void SayARepositoryThatHasGoneCanBeRemovedFromTheWorktreesTab()
    {
        Wording.Describe(new RemovalReason.RepositoryGone(FolderPath.From(@"C:\Users\me\source\repos\Northwind.Orders")))
            .ShouldBe(@"its repository C:\Users\me\source\repos\Northwind.Orders no longer exists; remove it from the Worktrees tab");
    }

    [Fact]
    public void SayGitCouldNotInspectTheWorktree()
    {
        Wording.Describe(new RemovalReason.GitFailed("fatal: not a git repository"))
            .ShouldBe("git could not inspect it: fatal: not a git repository; check it on the Worktrees tab");
    }

    public static TheoryData<Type> HealthReasons => [.. typeof(HealthReason).GetNestedTypes().Where(type => type.IsAssignableTo(typeof(HealthReason)))];

    [Theory]
    [MemberData(nameof(HealthReasons))]
    public void HaveWordsForEveryHealthReason(Type type)
    {
        HealthReason reason = (HealthReason)Fixtures.Build(type);

        Wording.Describe(reason).ShouldNotBe(reason.ToString(), $"{type.Name} has no arm in Wording.Describe");
    }

    [Fact]
    public void SayNothingForAHealthyWorktree()
    {
        Wording.Describe(new HealthReason.None()).ShouldBeEmpty();
    }

    [Fact]
    public void SayTheFolderIsGone()
    {
        Wording.Describe(new HealthReason.FolderGone()).ShouldBe("registered with git but the folder is gone");
    }

    [Fact]
    public void SayTheGitFileIsGone()
    {
        Wording.Describe(new HealthReason.GitFileGone()).ShouldBe("registered with git but its .git file is gone");
    }

    [Fact]
    public void GiveGitsOwnWordsForALock()
    {
        Wording.Describe(new HealthReason.Locked("in use by another tool")).ShouldBe("in use by another tool");
    }

    [Fact]
    public void SayTheRegistrationIsGone()
    {
        Wording.Describe(new HealthReason.RegistrationGone()).ShouldBe("its .git file points at a registration that no longer exists");
    }

    [Fact]
    public void SayAFolderUnderClaudeWorktreesIsNoLongerListed()
    {
        Wording.Describe(new HealthReason.UnderClaudeWorktrees()).ShouldBe(@"under .claude\worktrees but git no longer lists it");
    }

    [Fact]
    public void SayALeftoverFolderIsEmpty()
    {
        Wording.Describe(new HealthReason.EmptyLeftover()).ShouldBe("leftover folder with nothing in it");
    }

    [Fact]
    public void SayAFolderIsNotRegistered()
    {
        Wording.Describe(new HealthReason.NotRegistered()).ShouldBe("folder is not registered with git");
    }

    [Fact]
    public void NameTheRepositoryOfAWorktreeThatIsElsewhere()
    {
        Wording.Describe(new HealthReason.RepositoryOutsideRoots(FolderPath.From(@"D:\other\Northwind.Ledger"))).ShouldBe(@"its repository D:\other\Northwind.Ledger is not under the roots");
    }

    // The typed-confirmation dialog.

    [Fact]
    public void SayWhatToTypeWhenNothingIsTypedYet()
    {
        Wording.NothingTypedYet("Northwind.Orders-fix-123").ShouldBe("Nothing typed yet; the folder name is Northwind.Orders-fix-123.");
    }

    [Fact]
    public void SayWhatWasTypedWhenItIsNotTheSame()
    {
        Wording.NotTheSame("Northwind.Orders").ShouldBe("Not the same: you typed Northwind.Orders.");
    }

    [Fact]
    public void SayTheNameWasCopiedToTheClipboard()
    {
        Wording.CopiedToClipboard("Northwind.Orders-fix-123").ShouldBe("Copied Northwind.Orders-fix-123 to the clipboard; Ctrl+V pastes it into the box.");
    }

    [Fact]
    public void SayTheClipboardIsNotAvailable()
    {
        Wording.ClipboardUnavailable.ShouldBe("The clipboard is not available here; Shift+drag selects text in Windows Terminal.");
    }

    // The new-session wizard.

    public static TheoryData<Type> WizardProblems => [.. typeof(WizardProblem).GetNestedTypes().Where(type => type.IsAssignableTo(typeof(WizardProblem)))];

    [Theory]
    [MemberData(nameof(WizardProblems))]
    public void HaveWordsForEveryWizardProblem(Type type)
    {
        WizardProblem problem = (WizardProblem)Fixtures.Build(type);

        Wording.Describe(problem).ShouldNotBe(problem.ToString(), $"{type.Name} has no arm in Wording.Describe");
    }

    [Fact]
    public void SayNothingWhenTheWizardHasNoProblem()
    {
        Wording.Describe(new WizardProblem.None()).ShouldBeEmpty();
    }

    [Fact]
    public void AskForARepositoryInTheWizard()
    {
        Wording.Describe(new WizardProblem.NoRepository()).ShouldBe("Choose a repository: type part of its name, then ↑↓.");
    }

    [Fact]
    public void GiveThePlanBuildersReasonInTheWizard()
    {
        Wording.Describe(new WizardProblem.Refused(new LaunchProblem.WorktreeNeedsLetters()))
            .ShouldBe("A worktree needs a name with some letters or digits in it.");
    }

    // The settings dialog.

    public static TheoryData<Type> SettingsProblems => [.. typeof(SettingsProblem).GetNestedTypes().Where(type => type.IsAssignableTo(typeof(SettingsProblem)))];

    [Theory]
    [MemberData(nameof(SettingsProblems))]
    public void HaveWordsForEverySettingsProblem(Type type)
    {
        SettingsProblem problem = (SettingsProblem)Fixtures.Build(type);

        Wording.Describe(problem).ShouldNotBe(problem.ToString(), $"{type.Name} has no arm in Wording.Describe");
    }

    [Fact]
    public void SaySettingsProblems()
    {
        Wording.Describe(new SettingsProblem.NoRoots()).ShouldBe(@"Give a repository root: a full path, such as C:\Users\me\source\repos.");
        Wording.Describe(new SettingsProblem.RootAlreadyListed(FolderPath.From(@"D:\work"))).ShouldBe(@"D:\work is already a repository root.");
        Wording.Describe(new SettingsProblem.RootNotFullPath("repos")).ShouldBe(@"The repository root repos is not a full path, such as C:\Users\me\source\repos.");
        Wording.Describe(new SettingsProblem.RootMissing(FolderPath.From(@"D:\nowhere"))).ShouldBe(@"D:\nowhere does not exist.");
        Wording.Describe(new SettingsProblem.WorktreeRootNotFullPath()).ShouldBe(@"The worktree root must be a full path, such as C:\Users\me\source\worktrees.");
        Wording.Describe(new SettingsProblem.NoExecutable()).ShouldBe("A custom editor needs an executable.");
        Wording.Describe(new SettingsProblem.ExecutableNotFullPath()).ShouldBe(@"A custom editor's executable must be a full path, such as C:\Tools\subl.exe, so it is never looked for in the folder Torni was started from.");
        Wording.Describe(new SettingsProblem.BatchFileExecutable()).ShouldBe("A batch file cannot be used as the editor: cmd.exe would read the folder path as commands.");
        Wording.Describe(new SettingsProblem.NoFolderPlaceholder()).ShouldBe("A custom editor's arguments need {folder} where the folder goes.");
        Wording.Describe(new SettingsProblem.ExecutableMissing(@"C:\Tools\subl.exe")).ShouldBe(@"C:\Tools\subl.exe does not exist.");
    }

    [Fact]
    public void SaySettingsAreBeingSaved()
    {
        Wording.Working(new SaveSettings(TestSettings.For(@"C:\Users\me\source\repos"))).ShouldBe("Saving settings...");
    }

    [Fact]
    public void NameEveryEditorPreset()
    {
        Enum.GetValues<EditorPreset>().Select(Wording.Describe).ShouldBe(["VS Code", "Visual Studio", "Rider", "Explorer", "Custom"]);
    }

    [Fact]
    public void MarkAPresetNotFoundOnceTheDialogHasLooked()
    {
        PresetSearch looked = new PresetSearch.Looked(new HashSet<EditorPreset> { EditorPreset.Rider, EditorPreset.Explorer, EditorPreset.Custom });

        Wording.Describe(EditorPreset.Rider, looked).ShouldBe("Rider (not found)");
        Wording.Describe(EditorPreset.VsCode, looked).ShouldBe("VS Code");
        Wording.Describe(EditorPreset.Explorer, looked).ShouldBe("Explorer", "Explorer is never looked for");
        Wording.Describe(EditorPreset.Custom, looked).ShouldBe("Custom", "Custom is never marked");
        Wording.Describe(EditorPreset.Rider, new PresetSearch.NotYetLooked()).ShouldBe("Rider");
    }

    [Fact]
    public void SayTheTabArrowsInEveryHelpAndNoSettingsKey()
    {
        foreach (string help in new[] { Wording.SessionsHelp(@"C:\Users\me\.local\bin\claude.exe"), Wording.WorktreesHelp, Wording.ConfigHelp })
        {
            help.ShouldContain("the tab to the right");
            help.ShouldNotContain("the Config tab");
            help.ShouldNotContain("edit roots, worktree root and editor");
        }
    }

    // Refusals on the launch path, in the wizard and torni here.
    public static TheoryData<Type> LaunchProblems => [.. typeof(LaunchProblem).GetNestedTypes().Where(type => type.IsAssignableTo(typeof(LaunchProblem)))];

    [Theory]
    [MemberData(nameof(LaunchProblems))]
    public void HaveWordsForEveryLaunchProblem(Type type)
    {
        LaunchProblem problem = (LaunchProblem)Fixtures.Build(type);

        Wording.Describe(problem).ShouldNotBe(problem.ToString(), $"{type.Name} has no arm in Wording.Describe");
    }

    // The wizard's problem label is one line, about 104 columns wide in a 120-column terminal.
    private const int ProblemLineWidth = 100;

    [Theory]
    [MemberData(nameof(WizardProblems))]
    [MemberData(nameof(LaunchProblems))]
    public void FitEveryWizardProblemOnItsOneLine(Type type)
    {
        object problem = Fixtures.Build(type);
        string sentence = problem is WizardProblem wizardProblem ? Wording.Describe(wizardProblem) : Wording.Describe((LaunchProblem)problem);

        sentence.Length.ShouldBeLessThanOrEqualTo(ProblemLineWidth, sentence);
    }

    [Fact]
    public void GiveTheNameSentenceForARefusedLaunchName()
    {
        Wording.Describe(new LaunchProblem.InvalidName(NameProblem.Missing)).ShouldBe("A session name is required.");
    }

    [Fact]
    public void SayAWorktreeNeedsARepository()
    {
        Wording.Describe(new LaunchProblem.WorktreeNeedsRepository()).ShouldBe("A worktree needs a repository; choose one or start on the current branch.");
    }

    [Fact]
    public void SayAWorktreeNeedsLettersOrDigits()
    {
        Wording.Describe(new LaunchProblem.WorktreeNeedsLetters()).ShouldBe("A worktree needs a name with some letters or digits in it.");
    }

    [Fact]
    public void SayAnOpeningPromptCannotStartWithADash()
    {
        Wording.Describe(new LaunchProblem.PromptLooksLikeAnOption()).ShouldBe("A prompt cannot start with a dash: Claude Code would read it as options. Start with a word.");
    }

    [Fact]
    public void SayANameLooksLikeAnOption()
    {
        Wording.NameLooksLikeAnOption("--promt").ShouldBe("--promt looks like an option, not a session name. To use it as the name, put -- before it: torni here -- --promt");
    }

    [Fact]
    public void SayClaudeCodeWasNotFound()
    {
        Wording.ClaudeNotFound.ShouldBe("Claude Code was not found. Install it with irm https://claude.ai/install.ps1 | iex, or put claude.exe on the PATH.");
    }

    [Fact]
    public void SayClaudeCodeIsOnlyABatchFileAndNameIt()
    {
        Wording.ClaudeIsABatchFile(@"C:\Users\me\AppData\Roaming\npm\claude.cmd")
            .ShouldBe(@"Claude Code was found only as the batch file C:\Users\me\AppData\Roaming\npm\claude.cmd, which Torni does not run: cmd.exe would read session names and prompts as commands. Install the native build with irm https://claude.ai/install.ps1 | iex.");
    }

    [Fact]
    public void SayGitWasNotFound()
    {
        Wording.GitNotFound.ShouldBe("git was not found on the PATH. Install Git for Windows (winget install --id Git.Git --exact --source winget) and open a new terminal.");
    }

    [Fact]
    public void TitleTheWizardByWhereTheSessionStarts()
    {
        Wording.WizardTitle(Wizard()).ShouldBe("New session");
        Wording.WizardTitle(Wizard(FixedFolder)).ShouldBe("New session in Northwind.Orders-fix-123");
    }

    [Fact]
    public void CaptionTheChosenRepositoryOrTheFixedDirectory()
    {
        Wording.WizardChosenCaption(Wizard()).ShouldBe("Repository");
        Wording.WizardChosenCaption(Wizard(FixedFolder)).ShouldBe("Directory");
    }

    [Fact]
    public void NameTheChosenRepositoryOrSayNone()
    {
        Wording.WizardChosen(Wizard()).ShouldBe("(none)");
        Wording.WizardChosen(Wizard(FixedFolder)).ShouldBe("Northwind.Orders-fix-123");
    }

    [Fact]
    public void HintTheWizardsKeys()
    {
        Wording.WizardHint(Wizard()).ShouldBe("Type to filter, ↑↓ to choose, Enter confirms the repository   Enter or Tab next field   F5 start and open in tab   Esc cancel");
        Wording.WizardHint(Wizard(FixedFolder)).ShouldBe("Enter or F5 starts and opens in tab   Esc cancel");
    }

    // torni setup-terminal.

    private const string SetupUsage = "Usage: torni setup-terminal [--sound <wav>] [--session-sound <wav>] [--yes] [--remove]";

    [Fact]
    public void GiveTheSetupTerminalUsageLine()
    {
        Wording.SetupTerminalUsage.ShouldBe(SetupUsage);
    }

    public static TheoryData<Type> SetupProblems => [.. typeof(TerminalSetupProblem).GetNestedTypes().Where(type => type.IsAssignableTo(typeof(TerminalSetupProblem)))];

    [Theory]
    [MemberData(nameof(SetupProblems))]
    public void HaveWordsForEveryTerminalSetupProblem(Type type)
    {
        TerminalSetupProblem problem = (TerminalSetupProblem)Fixtures.Build(type);

        Wording.DescribeSetupProblem(problem).ShouldEndWith(SetupUsage);
        Wording.DescribeSetupProblem(problem).ShouldNotBe(problem.ToString());
    }

    [Fact]
    public void SayWhyTerminalSetupWillNotRun()
    {
        Wording.DescribeSetupProblem(new TerminalSetupProblem.RemoveWithSound()).ShouldBe($"--remove cannot be combined with --sound or --session-sound. {SetupUsage}");
        Wording.DescribeSetupProblem(new TerminalSetupProblem.SoundNotFound(@"C:\Users\me\missing.wav")).ShouldBe($@"The sound file C:\Users\me\missing.wav does not exist. {SetupUsage}");
        Wording.DescribeSetupProblem(new TerminalSetupProblem.SoundNotWav(@"C:\Windows\Media\notes.txt")).ShouldBe($@"The sound file C:\Windows\Media\notes.txt is not a .wav file. {SetupUsage}");
    }

    [Fact]
    public void ShowWhatTheProfilesWillBe()
    {
        TerminalProfiles profiles = new(@"C:\Users\me\.dotnet\tools\torni.exe", new BellSound.File(@"C:\Windows\Media\Windows Notify Messaging.wav"), new BellSound.TerminalDefault());

        Wording.TerminalSetupPlan(@"C:\Users\me\AppData\Local\Microsoft\Windows Terminal\Fragments\Torni\torni.json", profiles).ShouldBe("""
            Two Windows Terminal profiles will be written to C:\Users\me\AppData\Local\Microsoft\Windows Terminal\Fragments\Torni\torni.json
            Your Windows Terminal settings file is not changed.
              Torni          runs C:\Users\me\.dotnet\tools\torni.exe; bell sound C:\Windows\Media\Windows Notify Messaging.wav
              Torni session  runs PowerShell; bell sound is Windows Terminal's own
            Both ring the bell and flash the window and the taskbar.
            """);
    }

    [Fact]
    public void AskBeforeWritingTheProfiles()
    {
        Wording.WriteProfilesQuestion.ShouldBe("Write the Windows Terminal profiles? [y/N] ");
    }

    [Fact]
    public void SayWhatWasWrittenAndToRestart()
    {
        Wording.ProfilesWritten(@"C:\f\torni.json").ShouldBe(@"Wrote C:\f\torni.json. Restart Windows Terminal to see the Torni and Torni session profiles.");
    }

    [Fact]
    public void SayAMissingDefaultSoundIsLeftOut()
    {
        Wording.DefaultSoundMissing("Torni", @"C:\Windows\Media\Windows Notify Messaging.wav")
            .ShouldBe(@"C:\Windows\Media\Windows Notify Messaging.wav was not found, so the Torni profile uses Windows Terminal's own bell sound.");
    }

    [Fact]
    public void ShowTheClaudeSettingBeforeAskingForIt()
    {
        Wording.ClaudeBellPlan(@"C:\Users\me\.claude\settings.json", replacing: null).ShouldBe("""
            Claude Code can ring the terminal bell when a session finishes or needs permission.
            This sets "preferredNotifChannel": "terminal_bell" in C:\Users\me\.claude\settings.json and keeps every other setting.
            """);
        Wording.ClaudeBellPlan(@"C:\Users\me\.claude\settings.json", replacing: "iterm2").ShouldBe("""
            Claude Code can ring the terminal bell when a session finishes or needs permission.
            This replaces "preferredNotifChannel": "iterm2" with "terminal_bell" in C:\Users\me\.claude\settings.json and keeps every other setting.
            """);
        Wording.TurnOnClaudeBellQuestion.ShouldBe("Turn on Claude Code's terminal bell? [y/N] ");
    }

    [Fact]
    public void SayWhatBecameOfClaudesSetting()
    {
        Wording.ClaudeBellAlreadyOn.ShouldBe("Claude Code's terminal bell is already on.");
        Wording.ClaudeBellEnabled(@"C:\Users\me\.claude\settings.json").ShouldBe(@"Set preferredNotifChannel to terminal_bell in C:\Users\me\.claude\settings.json. It applies to sessions started from now on.");
        Wording.ClaudeSettingsUnreadable(@"C:\Users\me\.claude\settings.json", "bad json").ShouldBe(@"Could not read C:\Users\me\.claude\settings.json (bad json), so it was left alone.");
    }

    [Fact]
    public void SayNothingWasWritten()
    {
        Wording.NothingWritten.ShouldBe("Nothing was written.");
    }

    [Fact]
    public void SayWhatRemovingDid()
    {
        Wording.ProfilesRemoved(@"C:\f\torni.json").ShouldBe(@"Removed C:\f\torni.json. Restart Windows Terminal to drop the Torni profiles.");
        Wording.NothingToRemove.ShouldBe("There is nothing to remove.");
    }

    [Fact]
    public void ReportAFailedSetupWrite()
    {
        Wording.TerminalSetupFailed(@"C:\f\torni.json", "access denied").ShouldBe(@"Could not write C:\f\torni.json: access denied");
    }

    [Fact]
    public void SayWhereAProblemReportGoesBeforeItGoes()
    {
        Wording.ProblemReportDialogTitle.ShouldBe("Report a problem");
        Wording.ProblemReportExplanation.ShouldBe("Holds no names, paths or code. Opening the page passes the title and this text to GitHub in its address; nothing is public until you submit it there, where you describe the problem.");
        Wording.ProblemReportTitleCaption.ShouldBe("Title (blank for a default)");
        Wording.ProblemReportPreviewCaption.ShouldBe("Diagnostics the page will be filled in with");
        Wording.ProblemReportButtons.ShouldBe(["Open in browser", "Copy text", "Cancel"]);
    }

    [Fact]
    public async Task SayWhatTheProblemReportIsDoing()
    {
        ProblemReport report = await new ProblemReports(new DiagnosticLog(new FakeTimeProvider(Fixtures.Noon), []), new ProcessCounts(), new FakeCrashFile(), new Started(new ToolVersion("0.18.0"), null, new Version(10, 0), new Version(10, 0), 120, 36), TestSettings.Current(@"C:\Users\me\source\repos"), new FakeTimeProvider(Fixtures.Noon)).BuildAsync();

        Wording.Working(new ShowProblemReport()).ShouldBe("Gathering the diagnostics...");
        Wording.Working(new OpenProblemReport(ReportTitle.From("x"), report)).ShouldBe("Opening the problem report in your browser...");
        Wording.Working(new CopyProblemReport(ReportTitle.From("x"), report)).ShouldBe("Copying the problem report...");
    }

    [Fact]
    public void GiveABlankReportTitleTheVersion()
    {
        Wording.DefaultReportTitle(new ToolVersion("0.18.0")).ShouldBe("Problem report from Torni 0.18.0");
    }

    [Fact]
    public void SayWhyAReportTitleIsRefused()
    {
        Wording.Describe(ReportTitleProblem.TooLong).ShouldBe("A title can have at most 200 characters.");
        Wording.Describe(ReportTitleProblem.Missing).ShouldBe("Type a title.");
    }

    [Fact]
    public void PreviewWhatTheWizardsNameBecomes()
    {
        FolderPath root = FolderPath.From(@"C:\Users\me\source\worktrees");

        Wording.PreviewCurrentBranch("Northwind.Orders").ShouldBe("Runs in Northwind.Orders on its current branch; the name is only the session's label.");
        Wording.PreviewNeedsRepository.ShouldBe("A worktree needs a repository; this folder is not one.");
        Wording.PreviewBeforeAName("Northwind.Orders", root).ShouldBe(@"The name becomes the worktree folder Northwind.Orders-<name> under C:\Users\me\source\worktrees and its branch.");
        Wording.PreviewWorktree("Northwind.Orders-fix-123-flaky-tests", root, BranchName.From("fix-123-flaky-tests"))
            .ShouldBe(@"Worktree Northwind.Orders-fix-123-flaky-tests under C:\Users\me\source\worktrees, on branch fix-123-flaky-tests.");
    }

    [Fact]
    public void TitleTheTrustAndUpdateTabs()
    {
        Wording.TrustTabTitle("Northwind.Orders").ShouldBe("Trust Northwind.Orders");
        Wording.TrustTabMessage("Bob's tools").ShouldBe("Torni: accept the trust prompt for Bob's tools, then exit Claude and press n in Torni to retry.");
        Wording.UpdateTabTitle.ShouldBe("Update Torni");
    }

    [Fact]
    public void HintWhatToDoWhenTheUpgradeFails()
    {
        Wording.UpdateFailedHint.ShouldBe("Torni: the upgrade failed. If winget says a file is in use, quit every Torni that is still open; if it found no newer package, winget may not have this release yet. Then press U in Torni again.");
    }

    [Fact]
    public void NameEveryDialogAndItsButtons()
    {
        Wording.KeysTitle.ShouldBe("Keys");
        Wording.DeleteBranchTitle.ShouldBe("Delete branch");
        Wording.DeleteBranchButtons.ShouldBe(["Delete branch", "Keep"]);
        Wording.RenameTitle.ShouldBe("Rename session");
        Wording.CloseEmptyTitle.ShouldBe("Close empty terminal sessions");
        Wording.CloseEmptyButtons.ShouldBe(["Close", "Keep"]);
        Wording.ForceRemoveTitle.ShouldBe("Force remove");
        Wording.DeleteSessionTitle.ShouldBe("Delete session");
        Wording.DeleteButtons.ShouldBe(["Delete", "Cancel"]);
        Wording.CancelButton.ShouldBe("_Cancel");
        Wording.CancelWithEscButton.ShouldBe("_Cancel (Esc)");
        Wording.CopyNameButton.ShouldBe("Cop_y name (F2)");
        Wording.StartButton.ShouldBe("_Start (F5)");
        Wording.SaveButton.ShouldBe("_Save (F5)");
    }

    /// <summary>A wizard with no repositories to offer.</summary>
    private static WizardState Wizard() => Wizard(new WizardOrigin.Anywhere());

    /// <summary>A wizard fixed in one folder.</summary>
    private static WizardState Wizard(string fixedDirectory) => Wizard(WizardOrigin.In(FolderPath.From(fixedDirectory)));

    private static WizardState Wizard(WizardOrigin origin)
    {
        FakeFolders folders = new();

        return WizardState.Open(new RepositoryCatalogue(folders, TestSettings.Current()), [], folders, TestSettings.For(), origin);
    }
}