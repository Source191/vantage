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
        Wording.Describe(new Footer.Unhealthy("Northwind.Orders-stale", WorktreeHealthKind.Broken, "its repository has gone"))
            .ShouldBe("Northwind.Orders-stale is broken: its repository has gone.");
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
    public void SayANameCannotHoldUnsafeCharacters()
    {
        Wording.Describe(NameProblem.UnsafeCharacters)
            .ShouldBe("A session name cannot contain a semicolon or a double quote, because Windows Terminal cannot pass them safely.");
        Wording.Describe(new Footer.InvalidName(NameProblem.UnsafeCharacters))
            .ShouldBe("A session name cannot contain a semicolon or a double quote, because Windows Terminal cannot pass them safely.");
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
        Wording.Describe(new Footer.NotRemoved("3 changed or untracked files would be lost", OfferForce: true))
            .ShouldBe("Not removed: 3 changed or untracked files would be lost. D forces after a typed confirmation.");
    }

    [Fact]
    public void GiveTheReasonARemovalWasRefusedWithoutTheForceKey()
    {
        Wording.Describe(new Footer.NotRemoved("it is locked: in use by another tool", OfferForce: false))
            .ShouldBe("Not removed: it is locked: in use by another tool.");
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

    // Dialogs and the screen.

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
        Wording.Describe(new Footer.Reported(new StopSessionOutcome("fix-123"))).ShouldBe("Stopped fix-123.");
    }

    [Fact]
    public void SayTheBranchWasKeptAfterTheRemoval()
    {
        Wording.Describe(new Footer.BranchKept(new DeleteSessionOutcome.BranchNotMerged("fix-123", new BranchToConfirm(new FolderPath(Sessions.Repository), new BranchName("fix-123")))))
            .ShouldBe("Deleted fix-123 and removed the worktree; branch fix-123 is not merged. Branch kept.");
    }

    [Fact]
    public void SayTheBranchWasDeletedAfterTheRemoval()
    {
        Wording.Describe(new Footer.Followed(new DeleteSessionOutcome.BranchNotMerged("fix-123", new BranchToConfirm(new FolderPath(Sessions.Repository), new BranchName("fix-123"))), new DeleteBranchOutcome()))
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
    public void SayHowToComeBackFromAnAttach()
    {
        Wording.ReturnHint.ShouldBe("Ctrl+Z returns to Vantage. If you see the agent list instead, the session has no messages yet: Space sends it a prompt, Esc returns to Vantage.");
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
        Wording.Working(new RenameSession(Job, new SessionName("fix-124"))).ShouldBe("Renaming fix-123 to fix-124...");
        Wording.Working(new CloseEmptyTerminals([Terminal])).ShouldBe("Closing...");
        Wording.Working(new ResumeSessions([Job, Job])).ShouldBe("Resuming 2...");
        Wording.Working(new DeleteSession(Job)).ShouldBe("Deleting fix-123...");
        Wording.Working(new DeleteBranch(new BranchToConfirm(new FolderPath(Sessions.Repository), new BranchName("fix-123")))).ShouldBe("Deleting branch fix-123...");
    }

    [Fact]
    public void SayWhatEachWorktreeEffectIsDoingWhileItRuns()
    {
        Wording.Working(new RemoveWorktree(Worktree, Forced: false)).ShouldBe("Removing Northwind.Orders-fix-123...");
        Wording.Working(new PruneRepository(new FolderPath(Worktrees.Repository))).ShouldBe("Pruning...");
        Wording.Working(new OpenShellTab(Worktree)).ShouldBe("Opening a shell in Northwind.Orders-fix-123...");
    }

    [Fact]
    public void NameTheSessionBeingStarted()
    {
        LaunchPlan plan = new(
            new FolderPath(Sessions.Repository), new SessionName("Fix flaky tests"), Worktree: null,
            ReuseExistingWorktree: false, WorktreeBase.RemoteDefault, OpeningPrompt: null, new RepositoryChoice("Northwind.Orders", new FolderPath(Sessions.Repository), IsRepository: true));

        Wording.Working(new StartSession(plan, OpenInTab: true)).ShouldBe("Starting Fix flaky tests...");
    }

    [Fact]
    public void FallBackToAPlainWordForAnyOtherEffect()
    {
        Wording.Working(new Quit()).ShouldBe("Working...");
    }

    private static object Placeholder(Type type)
    {
        return type switch
        {
            _ when type == typeof(string) => "fix-123",
            _ when type == typeof(int) => 2,
            _ when type == typeof(bool) => true,
            _ when type == typeof(WorktreeHealthKind) => WorktreeHealthKind.Locked,
            _ when type == typeof(NameProblem) => NameProblem.UnsafeCharacters,
            _ when type == typeof(FolderPath) => new FolderPath(@"D:\nowhere"),
            _ when type == typeof(LaunchProblem) => new LaunchProblem.WorktreeNeedsLetters(),
            _ when type == typeof(ViewEffect) => new Quit(),
            _ when type == typeof(Outcome) => new StopSessionOutcome("fix-123"),
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
        Wording.Describe(new SettingsProblem.RootMissing(new FolderPath(@"D:\nowhere"))).ShouldBe(@"D:\nowhere does not exist.");
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
    public void SayAnOptionNeedsAValueWithTheUsageLine()
    {
        Wording.Describe(new LaunchProblem.OptionNeedsValue("-p")).ShouldBe("-p needs a value. Usage: vantage here [name] [-p \"opening prompt\"]");
    }

    [Fact]
    public void NameAnUnknownOptionWithTheUsageLine()
    {
        Wording.Describe(new LaunchProblem.UnknownOption("--bogus")).ShouldBe("Unknown option --bogus. Usage: vantage here [name] [-p \"opening prompt\"]");
    }

    [Fact]
    public void NameAnUnexpectedArgumentWithTheUsageLine()
    {
        Wording.Describe(new LaunchProblem.UnexpectedArgument("extra")).ShouldBe("Unexpected argument extra. Usage: vantage here [name] [-p \"opening prompt\"]");
    }

    [Fact]
    public void GiveTheUsageLines()
    {
        Wording.HereUsage.ShouldBe("Usage: vantage here [name] [-p \"opening prompt\"]");
        Wording.ClaimTabUsage.ShouldBe("Usage: vantage claim-tab <registration id> <process id>");
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

        return WizardState.Open(new RepositoryCatalogue(folders, TestSettings.Current()), [], folders, TestSettings.For(), fixedDirectory is null ? null : new FolderPath(fixedDirectory));
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
        Wording.DescribeSetupProblem(new TerminalSetupProblem.UnknownOption("--loud")).ShouldBe($"Unknown option --loud. {SetupUsage}");
        Wording.DescribeSetupProblem(new TerminalSetupProblem.OptionNeedsValue("--sound")).ShouldBe($"--sound needs a value. {SetupUsage}");
        Wording.DescribeSetupProblem(new TerminalSetupProblem.UnexpectedArgument("extra")).ShouldBe($"Unexpected argument extra. {SetupUsage}");
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