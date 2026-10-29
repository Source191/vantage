using Source191.Vantage.Application;
using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Launch;
using Source191.Vantage.Application.TerminalSetup;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;
using Sessions = Source191.Vantage.Core.Tests.Application.Actions.SessionTestData;
using Worktrees = Source191.Vantage.Core.Tests.Application.Actions.WorktreeTestData;

namespace Source191.Vantage.Core.Tests.Application;

/// <summary>
/// Every sentence Vantage shows, verbatim, with the facts that produce it: one fact per footer case, dialog question,
/// working text and help text. A new footer case needs a fact here; nothing else checks that its words exist.
/// </summary>
public sealed class WordingShould
{
    private const string FixedFolder = @"C:\Users\me\source\repos\Northwind.Orders-fix-123";

    private static readonly BackgroundSession Job = Sessions.Job(Sessions.Repository);
    private static readonly InteractiveSession Terminal = Sessions.Terminal("fix-123", 4242);
    private static readonly WorktreeRow Worktree = Worktrees.Row(WorktreeHealthKind.Ok);

    public static TheoryData<Type> FooterCases => new(typeof(Footer).GetNestedTypes().Where(type => type.IsAssignableTo(typeof(Footer))));

    [Theory]
    [MemberData(nameof(FooterCases))]
    public void HaveWordsForEveryFooterCase(Type type)
    {
        System.Reflection.ConstructorInfo constructor = type.GetConstructors().Single(candidate => candidate.GetParameters().All(parameter => parameter.ParameterType != type));
        object[] arguments = [.. constructor.GetParameters().Select(parameter => Placeholder(parameter.ParameterType))];
        Footer footer = (Footer)constructor.Invoke(arguments);

        Wording.Describe(footer).ShouldNotBe(footer.ToString(), $"{type.Name} has no arm in Wording.Describe");
    }

    // Standing footers.

    [Fact]
    public void SayNothingForNoFooter()
    {
        Wording.Describe(Footer.None).ShouldBe(string.Empty);
    }

    [Fact]
    public void CountTheSessionsThatNeedInput()
    {
        Wording.Describe(new Footer.NeedInput(2)).ShouldBe("2 need input");
    }

    [Fact]
    public void HintThatRemovalIsSafe()
    {
        Wording.Describe(new Footer.RemoveHint()).ShouldBe("d removes only what is safe to remove");
    }

    [Fact]
    public void ExplainWhyTheSelectedWorktreeIsNotHealthy()
    {
        Wording.Describe(new Footer.Unhealthy("Northwind.Orders-stale", WorktreeHealthKind.Broken, new HealthReason.FolderGone()))
            .ShouldBe("Northwind.Orders-stale is broken: registered with git but the folder is gone.");
    }

    // The sessions view.

    [Fact]
    public void SayNoSessionIsSelected()
    {
        Wording.Describe(new Footer.NoSessionSelected()).ShouldBe("No session selected.");
    }

    [Fact]
    public void ExplainATerminalSessionWithMessages()
    {
        Wording.Describe(new Footer.RunsInOwnTerminal("fix-123", HasTranscript: true))
            .ShouldBe("fix-123 runs in its own terminal. Press b to move it under Vantage (its tab becomes a plain shell), or type /bg there.");
    }

    [Fact]
    public void ExplainATerminalSessionWithoutMessages()
    {
        Wording.Describe(new Footer.RunsInOwnTerminal("fix-123", HasTranscript: false))
            .ShouldBe("fix-123 runs in its own terminal and has no messages yet. d closes its tab, C closes every such tab, or send it something there and press b.");
    }

    [Fact]
    public void PointToTheTabThatAlreadyShowsTheSession()
    {
        Wording.Describe(new Footer.OpenInAnotherTab("fix-123")).ShouldBe("fix-123 is open in another tab; press o to switch to it.");
    }

    [Fact]
    public void SayASessionNeverSentAMessageHasNothingToResume()
    {
        Wording.Describe(new Footer.NeverSentAMessage("fix-123")).ShouldBe("fix-123 has nothing to resume: it was never sent a message. d deletes it.");
    }

    [Fact]
    public void SayThereAreNoEmptyTerminals()
    {
        Wording.Describe(new Footer.NoEmptyTerminals()).ShouldBe("No terminal sessions without messages.");
    }

    [Fact]
    public void RefuseToRenameASessionWithoutMessages()
    {
        Wording.Describe(new Footer.NoMessagesToRename("fix-123")).ShouldBe("fix-123 has no messages yet, so it cannot be renamed; send it something first.");
    }

    [Fact]
    public void WaitForAWorkingSessionBeforeRenaming()
    {
        Wording.Describe(new Footer.WorkingSoRenameWaits("fix-123")).ShouldBe("fix-123 is working; wait until it is idle or needs input, because renaming restarts it.");
    }

    [Fact]
    public void SayARenameWasCancelled()
    {
        Wording.Describe(new Footer.RenameCancelled()).ShouldBe("Rename cancelled.");
    }

    [Fact]
    public void SayANameIsRequired()
    {
        Wording.Describe(NameProblem.Missing).ShouldBe("A session name is required.");
        Wording.Describe(new Footer.InvalidName(NameProblem.Missing)).ShouldBe("A session name is required.");
    }


    [Fact]
    public void SayABackgroundSessionIsAlreadyUnderVantage()
    {
        Wording.Describe(new Footer.AlreadyUnderVantage("fix-123")).ShouldBe("fix-123 already runs in the background under Vantage.");
    }

    [Fact]
    public void WaitForAWorkingSessionBeforeAdopting()
    {
        Wording.Describe(new Footer.WorkingSoAdoptWaits("fix-123")).ShouldBe("fix-123 is working; wait until it is idle or needs input, so nothing in flight is lost.");
    }

    // The worktrees view.

    [Fact]
    public void SayNoWorktreeIsSelected()
    {
        Wording.Describe(new Footer.NoWorktreeSelected()).ShouldBe("No worktree selected.");
    }

    [Fact]
    public void GiveTheReasonARemovalWasRefusedAndOfferTheForceKey()
    {
        Wording.Describe(new Footer.NotRemoved(new RemovalReason.ChangesWouldBeLost(3), OfferForce: true))
            .ShouldBe("Not removed: 3 changed or untracked files would be lost. D forces after a typed confirmation.");
    }

    [Fact]
    public void GiveTheReasonARemovalWasRefusedWithoutTheForceKey()
    {
        Wording.Describe(new Footer.NotRemoved(new RemovalReason.Locked(), OfferForce: false))
            .ShouldBe("Not removed: the worktree is locked.");
    }

    [Fact]
    public void SayAForceRemovalWasCancelled()
    {
        Wording.Describe(new Footer.ForceCancelled()).ShouldBe("Cancelled; nothing removed.");
    }

    [Fact]
    public void SayNothingWasTypedForAForceRemoval()
    {
        Wording.Describe(new Footer.NothingTyped()).ShouldBe("Nothing was typed; nothing removed.");
    }

    [Fact]
    public void ShowWhatWasTypedWhenItIsNotTheName()
    {
        Wording.Describe(new Footer.TypedWrongName("x", "Northwind.Orders-fix-123")).ShouldBe("You typed x, not Northwind.Orders-fix-123; nothing removed.");
    }

    [Fact]
    public void SayThereIsNothingToPruneWithoutARepository()
    {
        Wording.Describe(new Footer.NothingToPrune("Northwind.Orders")).ShouldBe("Nothing to prune: the repository Northwind.Orders no longer exists.");
    }

    [Fact]
    public void SayALeftoverFolderIsNotAWorktree()
    {
        Wording.Describe(new Footer.LeftoverFolder("fix-service-tests")).ShouldBe("fix-service-tests is a leftover folder, not a worktree.");
    }

    [Fact]
    public void SayNoSessionRunsInTheWorktree()
    {
        Wording.Describe(new Footer.NoSessionIn("Northwind.Orders-fix-123")).ShouldBe("No session is running in Northwind.Orders-fix-123.");
    }

    [Fact]
    public void SayAStoppedSessionHasNothingToMeasure()
    {
        Wording.Describe(new Footer.NothingToMeasure("fix-123")).ShouldBe("fix-123 has no process to measure; it is stopped.");
        Wording.Working(new ShowMemory(Job, 5151)).ShouldBe("Measuring fix-123...");
    }

    [Fact]
    public void TitleTheMemoryDialogWithTheSessionName()
    {
        Wording.MemoryDialogTitle("fix-123").ShouldBe("Memory: fix-123");
    }

    [Fact]
    public void LayOutTheMemoryBreakdownAsAlignedLines()
    {
        MemoryBreakdown breakdown = new(
            "fix-123",
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
        MemoryBreakdown alone = new("fix-123", MemorySize.FromMegabytes(296), [], MemorySize.FromMegabytes(296), 1);

        Wording.MemoryBreakdownLines(alone).ShouldBe("Claude Code   296 MB\n\nTotal         296 MB in 1 process");
    }

    [Fact]
    public void SayNoSolutionWasFound()
    {
        Wording.Describe(new Footer.NoSolution("Northwind.Orders-fix-123")).ShouldBe("No solution found in Northwind.Orders-fix-123 or its src folder.");
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
        Enum.GetValues<PullRequestGap>().ShouldAllBe(gap => Wording.PullRequestCell(new PullRequestLookup.Missing(gap, null)) == string.Empty);
    }

    [Theory]
    [InlineData(PullRequestGap.NoBranch, null, "fix-123 has no branch, so it has no pull request.")]
    [InlineData(PullRequestGap.NoRemote, null, "fix-123's repository has no origin remote to look up pull requests on.")]
    [InlineData(PullRequestGap.UnknownHost, null, "fix-123's origin is on neither GitHub nor Azure DevOps, so Vantage cannot look up its pull requests.")]
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
    public void SayWhyThereIsNoPullRequest(PullRequestGap gap, PullRequestHost? host, string expected)
    {
        Wording.Describe(new Footer.NoPullRequest("fix-123", gap, host)).ShouldBe(expected);
    }

    [Fact]
    public void SayAPullRequestIsOpening()
    {
        PullRequest azure = new(PullRequestHost.AzureDevOps, 4521, new Uri("https://dev.azure.com/northwind/Trading/_git/Northwind.Orders/pullrequest/4521"), PullRequestState.Active, DateTimeOffset.UnixEpoch);

        Wording.Working(new OpenPullRequest(azure)).ShouldBe("Opening !4521...");
    }
    // Dialogs and the screen.

    [Fact]
    public void SayVantageIsUpToDate()
    {
        Wording.Describe(new Footer.UpToDate()).ShouldBe("Vantage is up to date.");
    }

    [Fact]
    public void SayTheUpdateCheckIsOff()
    {
        Wording.Describe(new Footer.UpdateCheckOff()).ShouldBe("The update check is off, or this is a development build; nothing to update to.");
    }

    [Fact]
    public void AskBeforeUpdating()
    {
        Wording.UpdateQuestion(ReleaseVersion.TryParse("0.10.0")!).ShouldBe("Quit Vantage and update to 0.10.0?\nBackground sessions keep running.");
    }

    [Fact]
    public void SayTheUpdateTabIsOpening()
    {
        Wording.Working(new StartUpdate(ReleaseVersion.TryParse("0.10.0")!)).ShouldBe("Opening a tab to update to 0.10.0...");
    }

    [Fact]
    public void SayTheEmptyTabsWereKept()
    {
        Wording.Describe(new Footer.Kept()).ShouldBe("Kept.");
    }

    [Fact]
    public void SayNothingWasChosenToResume()
    {
        Wording.Describe(new Footer.NothingChosenToResume()).ShouldBe("Nothing to resume.");
    }

    [Fact]
    public void NameAnEffectWithNoDialog()
    {
        Wording.Describe(new Footer.NoDialogFor("OpenSettings")).ShouldBe("No dialog is defined for OpenSettings.");
    }

    [Fact]
    public void SayWhatARunningEffectIsDoing()
    {
        Wording.Describe(new Footer.Working(new StopSession(Job))).ShouldBe("Stopping...");
    }

    [Fact]
    public void SayWhatAHandlerDid()
    {
        Wording.Describe(new Footer.Reported(new StopSessionOutcome("fix-123", TabClosed: false))).ShouldBe("Stopped fix-123.");
    }

    [Fact]
    public void SayTheBranchWasKeptAfterTheRemoval()
    {
        Wording.Describe(new Footer.BranchKept(new DeleteSessionOutcome.BranchNotMerged("fix-123", new BranchToConfirm(FolderPath.From(Sessions.Repository), BranchName.From("fix-123")))))
            .ShouldBe("Deleted fix-123 and removed the worktree; branch fix-123 is not merged. Branch kept.");
    }

    [Fact]
    public void SayTheBranchWasDeletedAfterTheRemoval()
    {
        Wording.Describe(new Footer.Followed(new DeleteSessionOutcome.BranchNotMerged("fix-123", new BranchToConfirm(FolderPath.From(Sessions.Repository), BranchName.From("fix-123"))), new DeleteBranchOutcome()))
            .ShouldBe("Deleted fix-123 and removed the worktree; branch fix-123 is not merged. Branch deleted.");
    }

    [Fact]
    public void SayBackgroundWorkFailed()
    {
        Wording.Describe(new Footer.Failed("git exited with 128")).ShouldBe("Failed: git exited with 128");
    }

    [Fact]
    public void AskForANewName()
    {
        Wording.RenamePrompt("fix-123").ShouldBe("Rename fix-123. The session restarts under the new name and keeps its conversation.");
    }

    [Fact]
    public void AskBeforeDeletingASession()
    {
        Wording.DeleteSessionQuestion("fix-123").ShouldBe("Delete fix-123?\nIts worktree is removed only when nothing in it would be lost.");
    }

    [Fact]
    public void AskBeforeClosingOneEmptyTab()
    {
        Wording.CloseEmptyQuestion(["fix-123"]).ShouldBe("Close the tab of fix-123?\nNone of them has been sent a message, so nothing is lost.");
    }

    [Fact]
    public void AskBeforeClosingSeveralEmptyTabs()
    {
        Wording.CloseEmptyQuestion(["fix-123", "repos-f6"]).ShouldBe("Close 2 tabs: fix-123, repos-f6?\nNone of them has been sent a message, so nothing is lost.");
    }

    [Fact]
    public void AskBeforeDeletingAnUnmergedBranchVantageCreated()
    {
        Wording.DeleteBranchQuestion("fix-123", createdByVantage: true).ShouldBe("Branch fix-123 is not merged. Delete it anyway?");
    }

    [Fact]
    public void AskBeforeDeletingABranchVantageDidNotCreate()
    {
        Wording.DeleteBranchQuestion("fix-123", createdByVantage: false).ShouldBe("Branch fix-123 was not created by Vantage. Delete it anyway?");
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
        Wording.ReturnHint.ShouldBe("Ctrl+Z returns to Vantage.");
        Wording.ReturnHint.Length.ShouldBeLessThanOrEqualTo(40);
    }

    [Fact]
    public void SayWhereToSupportVantage()
    {
        Wording.SupportLine.ShouldBe("Buy me a sliver of a Claude Max plan: https://buymeacoffee.com/simonvane");
    }

    [Fact]
    public void WarnBeforeAForcedRemoval()
    {
        Wording.ForceRemoveWarning.ShouldBe("This deletes the folder below and everything in it that is not committed and pushed.\nTo confirm, give its name:");
    }

    // What the footer says while an effect runs.

    [Fact]
    public void SayWhatEachSessionEffectIsDoingWhileItRuns()
    {
        Wording.Working(new OpenTabForSession(Job, HasLiveTab: false)).ShouldBe("Opening a tab for fix-123...");
        Wording.Working(new StopSession(Job)).ShouldBe("Stopping...");
        Wording.Working(new AdoptSession(Terminal)).ShouldBe("Moving fix-123 under Vantage...");
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
            FolderPath.From(Sessions.Repository), SessionName.From("Fix flaky tests"), Worktree: null,
            ReuseExistingWorktree: false, WorktreeBase.RemoteDefault, OpeningPrompt: null, new RepositoryChoice("Northwind.Orders", FolderPath.From(Sessions.Repository), IsRepository: true));

        Wording.Working(new StartSession(plan, OpenInTab: true)).ShouldBe("Starting Fix flaky tests...");
    }

    [Fact]
    public void FallBackToAPlainWordForAnyOtherEffect()
    {
        Wording.Working(new Quit()).ShouldBe("Working...");
    }

    // Why a worktree cannot be removed, and what is wrong with it.

    public static TheoryData<Type> RemovalReasons => new(typeof(RemovalReason).GetNestedTypes().Where(type => type.IsAssignableTo(typeof(RemovalReason))));

    [Theory]
    [MemberData(nameof(RemovalReasons))]
    public void HaveWordsForEveryRemovalReason(Type type)
    {
        System.Reflection.ConstructorInfo constructor = type.GetConstructors().Single(candidate => candidate.GetParameters().All(parameter => parameter.ParameterType != type));
        RemovalReason reason = (RemovalReason)constructor.Invoke([.. constructor.GetParameters().Select(parameter => Placeholder(parameter.ParameterType))]);

        Wording.Describe(reason).ShouldNotBe(reason.ToString(), $"{type.Name} has no arm in Wording.Describe");
    }

    [Fact]
    public void SayNothingForAPlainRemoval()
    {
        Wording.Describe(new RemovalReason.None()).ShouldBeEmpty();
    }

    [Fact]
    public void NameTheSessionUsingTheWorktree()
    {
        Wording.Describe(new RemovalReason.LiveSession("fix-123")).ShouldBe("session fix-123 is using it");
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
    public void CallALeftoverFolderByThatName()
    {
        Wording.Describe(new RemovalReason.LeftoverFolder()).ShouldBe("leftover folder");
    }

    public static TheoryData<Type> HealthReasons => new(typeof(HealthReason).GetNestedTypes().Where(type => type.IsAssignableTo(typeof(HealthReason))));

    [Theory]
    [MemberData(nameof(HealthReasons))]
    public void HaveWordsForEveryHealthReason(Type type)
    {
        System.Reflection.ConstructorInfo constructor = type.GetConstructors().Single(candidate => candidate.GetParameters().All(parameter => parameter.ParameterType != type));
        HealthReason reason = (HealthReason)constructor.Invoke([.. constructor.GetParameters().Select(parameter => Placeholder(parameter.ParameterType))]);

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

    // The typed-confirmation dialog.

    [Fact]
    public void SayWhatToTypeWhenNothingIsTypedYet()
    {
        Wording.NothingTypedYet("worktree folder", "Northwind.Orders-fix-123").ShouldBe("Nothing typed yet; the worktree folder is Northwind.Orders-fix-123.");
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

    private static object Placeholder(Type type)
    {
        return type switch
        {
            _ when type == typeof(string) => "fix-123",
            _ when type == typeof(int) => 2,
            _ when type == typeof(bool) => true,
            _ when type == typeof(WorktreeHealthKind) => WorktreeHealthKind.Locked,
            _ when type == typeof(RemovalReason) => new RemovalReason.Locked(),
            _ when type == typeof(HealthReason) => new HealthReason.FolderGone(),
            _ when type == typeof(NameProblem) => NameProblem.Missing,
            _ when type == typeof(FolderPath) => FolderPath.From(@"D:\nowhere"),
            _ when type == typeof(LaunchProblem) => new LaunchProblem.WorktreeNeedsLetters(),
            _ when type == typeof(ViewEffect) => new Quit(),
            _ when type == typeof(Outcome) => new StopSessionOutcome("fix-123", TabClosed: false),
            _ when type == typeof(PullRequestGap) => PullRequestGap.NotSignedIn,
            _ when type == typeof(PullRequestHost?) => PullRequestHost.GitHub,
            _ => throw new InvalidOperationException($"A footer field of type {type.Name} needs a placeholder."),
        };
    }

    // The new-session wizard.

    public static TheoryData<Type> WizardProblems => new(typeof(WizardProblem).GetNestedTypes().Where(type => type.IsAssignableTo(typeof(WizardProblem))));

    [Theory]
    [MemberData(nameof(WizardProblems))]
    public void HaveWordsForEveryWizardProblem(Type type)
    {
        System.Reflection.ConstructorInfo constructor = type.GetConstructors().Single(candidate => candidate.GetParameters().All(parameter => parameter.ParameterType != type));
        WizardProblem problem = (WizardProblem)constructor.Invoke([.. constructor.GetParameters().Select(parameter => Placeholder(parameter.ParameterType))]);

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

    public static TheoryData<Type> SettingsProblems => new(typeof(SettingsProblem).GetNestedTypes().Where(type => type.IsAssignableTo(typeof(SettingsProblem))));

    [Theory]
    [MemberData(nameof(SettingsProblems))]
    public void HaveWordsForEverySettingsProblem(Type type)
    {
        System.Reflection.ConstructorInfo constructor = type.GetConstructors().Single(candidate => candidate.GetParameters().All(parameter => parameter.ParameterType != type));
        SettingsProblem problem = (SettingsProblem)constructor.Invoke([.. constructor.GetParameters().Select(parameter => Placeholder(parameter.ParameterType))]);

        Wording.Describe(problem).ShouldNotBe(problem.ToString(), $"{type.Name} has no arm in Wording.Describe");
    }

    [Fact]
    public void SaySettingsProblems()
    {
        Wording.Describe(new SettingsProblem.NoRoots()).ShouldBe("Give at least one repository root, one per line.");
        Wording.Describe(new SettingsProblem.RootNotFullPath("repos")).ShouldBe(@"The repository root repos is not a full path, such as C:\Users\me\source\repos.");
        Wording.Describe(new SettingsProblem.RootMissing(FolderPath.From(@"D:\nowhere"))).ShouldBe(@"D:\nowhere does not exist.");
        Wording.Describe(new SettingsProblem.WorktreeRootNotFullPath()).ShouldBe(@"The worktree root must be a full path, such as C:\Users\me\source\worktrees.");
        Wording.Describe(new SettingsProblem.NoExecutable()).ShouldBe("A custom editor needs an executable.");
        Wording.Describe(new SettingsProblem.BatchFileExecutable()).ShouldBe("A batch file cannot be used as the editor: cmd.exe would read the folder path as commands.");
        Wording.Describe(new SettingsProblem.NoFolderPlaceholder()).ShouldBe("A custom editor's arguments need {folder} where the folder goes.");
    }

    [Fact]
    public void SaySettingsAreBeingSaved()
    {
        Wording.Working(new SaveSettings(TestSettings.For(@"C:\Users\me\source\repos"))).ShouldBe("Saving settings...");
    }

    [Fact]
    public void NameEveryEditorPreset()
    {
        Enum.GetValues<EditorPreset>().Select(Wording.Describe).ShouldBe(["VS Code", "Cursor", "Rider", "Visual Studio", "Explorer", "Custom"]);
    }

    [Fact]
    public void SayTheSettingsKeyInHelp()
    {
        Wording.SessionsHelp(@"C:\Users\me\.local\bin\claude.exe").ShouldContain(",      edit roots, worktree root and editor");
        Wording.WorktreesHelp.ShouldContain(",      edit roots, worktree root and editor");
    }

    // Refusals on the launch path, in the wizard and vantage here.
    public static TheoryData<Type> LaunchProblems => new(typeof(LaunchProblem).GetNestedTypes().Where(type => type.IsAssignableTo(typeof(LaunchProblem))));

    [Theory]
    [MemberData(nameof(LaunchProblems))]
    public void HaveWordsForEveryLaunchProblem(Type type)
    {
        System.Reflection.ConstructorInfo constructor = type.GetConstructors().Single(candidate => candidate.GetParameters().All(parameter => parameter.ParameterType != type));
        LaunchProblem problem = (LaunchProblem)constructor.Invoke([.. constructor.GetParameters().Select(parameter => Placeholder(parameter.ParameterType))]);

        Wording.Describe(problem).ShouldNotBe(problem.ToString(), $"{type.Name} has no arm in Wording.Describe");
    }

    // The wizard's problem label is one line, about 104 columns wide in a 120-column terminal.
    private const int ProblemLineWidth = 100;

    [Theory]
    [MemberData(nameof(WizardProblems))]
    [MemberData(nameof(LaunchProblems))]
    public void FitEveryWizardProblemOnItsOneLine(Type type)
    {
        System.Reflection.ConstructorInfo constructor = type.GetConstructors().Single(candidate => candidate.GetParameters().All(parameter => parameter.ParameterType != type));
        object problem = constructor.Invoke([.. constructor.GetParameters().Select(parameter => Placeholder(parameter.ParameterType))]);
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
    public void SayANameLooksLikeAnOption()
    {
        Wording.NameLooksLikeAnOption("--promt").ShouldBe("--promt looks like an option, not a session name. To use it as the name, put -- before it: vantage here -- --promt");
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
        Wording.WizardHint(Wizard()).ShouldBe("Type to filter, ↑↓ to choose, Enter confirms the repository   Enter or Tab next field   F5 start   F6 start and open in tab   Esc cancel");
        Wording.WizardHint(Wizard(FixedFolder)).ShouldBe("Enter or Tab next field   F5 start   F6 start and open in tab   Esc cancel");
    }

    /// <summary>A wizard with no repositories to offer, or fixed in one folder.</summary>
    private static WizardState Wizard(string? fixedDirectory = null)
    {
        FakeFolders folders = new();

        return WizardState.Open(new RepositoryCatalogue(folders, TestSettings.Current()), [], folders, TestSettings.For(), fixedDirectory is null ? null : FolderPath.From(fixedDirectory));
    }

    // vantage setup-terminal.

    private const string SetupUsage = "Usage: vantage setup-terminal [--sound <wav>] [--session-sound <wav>] [--yes] [--remove]";

    [Fact]
    public void GiveTheSetupTerminalUsageLine()
    {
        Wording.SetupTerminalUsage.ShouldBe(SetupUsage);
    }

    public static TheoryData<Type> SetupProblems => new(typeof(TerminalSetupProblem).GetNestedTypes().Where(type => type.IsAssignableTo(typeof(TerminalSetupProblem))));

    [Theory]
    [MemberData(nameof(SetupProblems))]
    public void HaveWordsForEveryTerminalSetupProblem(Type type)
    {
        System.Reflection.ConstructorInfo constructor = type.GetConstructors().Single(candidate => candidate.GetParameters().All(parameter => parameter.ParameterType != type));
        TerminalSetupProblem problem = (TerminalSetupProblem)constructor.Invoke([.. constructor.GetParameters().Select(parameter => Placeholder(parameter.ParameterType))]);

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
        TerminalProfiles profiles = new(@"C:\Users\me\.dotnet\tools\vantage.exe", @"C:\Windows\Media\Windows Notify Messaging.wav", SessionSound: null);

        Wording.TerminalSetupPlan(@"C:\Users\me\AppData\Local\Microsoft\Windows Terminal\Fragments\Vantage\vantage.json", profiles).ShouldBe("""
            Two Windows Terminal profiles will be written to C:\Users\me\AppData\Local\Microsoft\Windows Terminal\Fragments\Vantage\vantage.json
            Your Windows Terminal settings file is not changed.
              Vantage          runs C:\Users\me\.dotnet\tools\vantage.exe; bell sound C:\Windows\Media\Windows Notify Messaging.wav
              Vantage session  runs PowerShell; bell sound is Windows Terminal's own
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
        Wording.ProfilesWritten(@"C:\f\vantage.json").ShouldBe(@"Wrote C:\f\vantage.json. Restart Windows Terminal to see the Vantage and Vantage session profiles.");
    }

    [Fact]
    public void SayAMissingDefaultSoundIsLeftOut()
    {
        Wording.DefaultSoundMissing("Vantage", @"C:\Windows\Media\Windows Notify Messaging.wav")
            .ShouldBe(@"C:\Windows\Media\Windows Notify Messaging.wav was not found, so the Vantage profile uses Windows Terminal's own bell sound.");
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
        Wording.ProfilesRemoved(@"C:\f\vantage.json").ShouldBe(@"Removed C:\f\vantage.json. Restart Windows Terminal to drop the Vantage profiles.");
        Wording.NothingToRemove.ShouldBe("There is nothing to remove.");
    }

    [Fact]
    public void ReportAFailedSetupWrite()
    {
        Wording.TerminalSetupFailed(@"C:\f\vantage.json", "access denied").ShouldBe(@"Could not write C:\f\vantage.json: access denied");
    }
}