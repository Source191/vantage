using Source191.Vantage.Application;
using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Launch;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Application;

/// <summary>
/// The tone each status line message starts with: its glyph and colour, and the tone chosen for every footer and
/// outcome case. A new case needs a line in the tables here.
/// </summary>
public sealed class StatusTonesShould
{
    private static readonly BranchToConfirm Branch = new(FolderPath.From(@"C:\repos\Northwind.Orders"), BranchName.From("fix-123"));

    public static TheoryData<StatusTone, string, TextTone, TextTone> Tones => new()
    {
        { StatusTone.Success, "✓", TextTone.Green, TextTone.Green },
        { StatusTone.Notice, "!", TextTone.Orange, TextTone.Orange },
        { StatusTone.Failure, "✗", TextTone.Red, TextTone.Red },
        { StatusTone.Working, string.Empty, TextTone.Cyan, TextTone.Cyan },
        { StatusTone.Hint, string.Empty, TextTone.Plain, TextTone.Plain },
    };

    [Theory]
    [MemberData(nameof(Tones))]
    public void MarkEachToneWithItsGlyph(StatusTone tone, string glyph, TextTone markTone, TextTone wordsTone)
    {
        Wording.StatusMark(tone).ShouldBe(new TextPart(glyph, markTone));
        Wording.StatusWordsTone(tone).ShouldBe(wordsTone);
    }

    [Fact]
    public void OfferAMarkToCloseAMessage()
    {
        Wording.StatusClose.ShouldBe("✕");
        Wording.StatusCloseTip.ShouldBe("Close this message");
    }
    // Outcomes, each built with the placeholders below; an outcome whose tone depends on its data has its own facts too.

    private static readonly Dictionary<Type, StatusTone> OutcomeTones = new()
    {
        [typeof(StopSessionOutcome)] = StatusTone.Success,
        [typeof(AdoptSessionOutcome.NoMessages)] = StatusTone.Notice,
        [typeof(AdoptSessionOutcome.Moved)] = StatusTone.Success,
        [typeof(AdoptSessionOutcome.Failed)] = StatusTone.Failure,
        [typeof(AdoptSessionOutcome.ProcessGone)] = StatusTone.Notice,
        [typeof(RenameSessionOutcome.NoMessages)] = StatusTone.Notice,
        [typeof(RenameSessionOutcome.Renamed)] = StatusTone.Success,
        [typeof(RenameSessionOutcome.Restored)] = StatusTone.Failure,
        [typeof(RenameSessionOutcome.Unsupervised)] = StatusTone.Failure,
        [typeof(CloseEmptyTerminalsOutcome)] = StatusTone.Notice,
        [typeof(ResumeSessionsOutcome)] = StatusTone.Success,
        [typeof(DeleteSessionOutcome.Deleted)] = StatusTone.Success,
        [typeof(DeleteSessionOutcome.KeptWorktree)] = StatusTone.Notice,
        [typeof(DeleteSessionOutcome.RemovedWorktreeAndBranch)] = StatusTone.Success,
        [typeof(DeleteSessionOutcome.BranchNotMerged)] = StatusTone.Success,
        [typeof(DeleteSessionOutcome.RemovedWorktree)] = StatusTone.Success,
        [typeof(DeleteSessionOutcome.WorktreeInUse)] = StatusTone.Notice,
        [typeof(DeleteBranchOutcome)] = StatusTone.Success,
        [typeof(RemoveWorktreeOutcome.Refused)] = StatusTone.Notice,
        [typeof(RemoveWorktreeOutcome.Removed)] = StatusTone.Success,
        [typeof(RemoveWorktreeOutcome.RemovedAndDeletedBranch)] = StatusTone.Success,
        [typeof(RemoveWorktreeOutcome.BranchKept)] = StatusTone.Success,
        [typeof(RemoveWorktreeOutcome.DeletedFolder)] = StatusTone.Success,
        [typeof(RemoveWorktreeOutcome.DeletedFolderAndPruned)] = StatusTone.Success,
        [typeof(RemoveWorktreeOutcome.InUse)] = StatusTone.Notice,
        [typeof(PruneRepositoryOutcome)] = StatusTone.Success,
        [typeof(OpenShellOutcome)] = StatusTone.Success,
        [typeof(FolderPathCopied)] = StatusTone.Success,
        [typeof(ClipboardBusy)] = StatusTone.Notice,
        [typeof(OpenedSolution)] = StatusTone.Success,
        [typeof(OpenPullRequestOutcome)] = StatusTone.Success,
        [typeof(OpenInEditorOutcome.Opened)] = StatusTone.Success,
        [typeof(OpenInEditorOutcome.EditorNotFound)] = StatusTone.Failure,
        [typeof(OpenInEditorOutcome.FolderMissing)] = StatusTone.Notice,
        [typeof(OpenTabOutcome.Opened)] = StatusTone.Notice,
        [typeof(OpenTabOutcome.SwitchedTo)] = StatusTone.Success,
        [typeof(OpenTabOutcome.NotFound)] = StatusTone.Notice,
        [typeof(StartSessionOutcome.StartedAndOpened)] = StatusTone.Success,
        [typeof(StartSessionOutcome.NotListedYet)] = StatusTone.Notice,
        [typeof(StartSessionOutcome.NotTrusted)] = StatusTone.Failure,
        [typeof(StartSessionOutcome.Failed)] = StatusTone.Failure,
        [typeof(SaveSettingsOutcome)] = StatusTone.Success,
        [typeof(ToggleUpdateCheckOutcome)] = StatusTone.Success,
        [typeof(ProblemReportOutcome.Opened)] = StatusTone.Success,
        [typeof(ProblemReportOutcome.NotOpened)] = StatusTone.Failure,
        [typeof(ProblemReportOutcome.Copied)] = StatusTone.Success,
        [typeof(UpdateStartedOutcome)] = StatusTone.Success,
    };

    public static TheoryData<Type> OutcomeCases =>
        [.. typeof(Outcome).Assembly.GetTypes().Where(type => !type.IsAbstract && type.IsAssignableTo(typeof(Outcome)))];

    public static TheoryData<Type, StatusTone> OutcomeTable => [.. OutcomeTones.Select(entry => (entry.Key, entry.Value))];

    [Theory]
    [MemberData(nameof(OutcomeCases))]
    public void ChooseAToneForEveryOutcomeCase(Type type)
    {
        OutcomeTones.ShouldContainKey(type, $"{type.Name} has no tone in this test's table: choose one, and an arm in Wording.ToneOf");
    }

    [Theory]
    [MemberData(nameof(OutcomeTable))]
    public void GiveEachOutcomeItsTone(Type type, StatusTone tone)
    {
        Wording.ToneOf((Outcome)Built(type)).ShouldBe(tone);
    }

    [Fact]
    public void MarkATabOpenedInAColourOfItsOwnAsDone()
    {
        Wording.ToneOf(new OpenTabOutcome.Opened("fix-123", "Purple", ColourReused: false)).ShouldBe(StatusTone.Success);
        Wording.ToneOf(new OpenTabOutcome.Opened("fix-123", "Purple", ColourReused: true)).ShouldBe(StatusTone.Notice);
    }

    [Fact]
    public void MarkAStartByWhatHappenedToItsTab()
    {
        Wording.ToneOf(new StartSessionOutcome.StartedAndOpened("fix-123", JobId.From("81cdb0f9"), new OpenTabOutcome.Opened("fix-123", "Purple", ColourReused: false))).ShouldBe(StatusTone.Success);
        Wording.ToneOf(new StartSessionOutcome.StartedAndOpened("fix-123", JobId.From("81cdb0f9"), new OpenTabOutcome.Opened("fix-123", "Purple", ColourReused: true))).ShouldBe(StatusTone.Notice);
        Wording.ToneOf(new StartSessionOutcome.StartedAndOpened("fix-123", JobId.From("81cdb0f9"), new OpenTabOutcome.NotFound("fix-123"))).ShouldBe(StatusTone.Notice);
    }

    [Fact]
    public void MarkADeleteByWhatHappenedToItsWorktree()
    {
        Wording.ToneOf(new DeleteSessionOutcome.RemovedWorktree("fix-123", new RemoveWorktreeOutcome.DeletedFolder("Northwind.Orders-fix-123"))).ShouldBe(StatusTone.Success);
        Wording.ToneOf(new DeleteSessionOutcome.RemovedWorktree("fix-123", new RemoveWorktreeOutcome.InUse("Northwind.Orders-fix-123"))).ShouldBe(StatusTone.Notice);
    }

    [Fact]
    public void MarkClosingEmptyTabsAsDoneOnlyWhenNothingWasLeft()
    {
        Wording.ToneOf(new CloseEmptyTerminalsOutcome(["fix-123"], [], [], [])).ShouldBe(StatusTone.Success);
        Wording.ToneOf(new CloseEmptyTerminalsOutcome([], ["fix-123"], [], [])).ShouldBe(StatusTone.Success);
        Wording.ToneOf(new CloseEmptyTerminalsOutcome(["fix-123"], [], [new SkippedSession("templates-sync")], [])).ShouldBe(StatusTone.Notice);
        Wording.ToneOf(new CloseEmptyTerminalsOutcome(["fix-123"], [], [], ["templates-sync"])).ShouldBe(StatusTone.Notice);
        Wording.ToneOf(new CloseEmptyTerminalsOutcome([], [], [], [])).ShouldBe(StatusTone.Notice);
    }

    [Fact]
    public void MarkAResumeAsDoneOnlyWhenEveryChosenSessionResumed()
    {
        Wording.ToneOf(new ResumeSessionsOutcome(2, 2, [])).ShouldBe(StatusTone.Success);
        Wording.ToneOf(new ResumeSessionsOutcome(1, 2, [])).ShouldBe(StatusTone.Notice);
        Wording.ToneOf(new ResumeSessionsOutcome(2, 2, [new ResumeNote("fix-123", null)])).ShouldBe(StatusTone.Notice);
    }

    // Footers, each built with the placeholders below; the placeholder outcome is a success.

    private static readonly Dictionary<Type, StatusTone> FooterTones = new()
    {
        [typeof(StatusMessage.Blank)] = StatusTone.Hint,
        [typeof(StatusMessage.NoSessionSelected)] = StatusTone.Notice,
        [typeof(StatusMessage.RunsInOwnTerminal)] = StatusTone.Notice,
        [typeof(StatusMessage.NeverSentAMessage)] = StatusTone.Notice,
        [typeof(StatusMessage.NoEmptyTerminals)] = StatusTone.Notice,
        [typeof(StatusMessage.NoMessagesToRename)] = StatusTone.Notice,
        [typeof(StatusMessage.WorkingSoRenameWaits)] = StatusTone.Notice,
        [typeof(StatusMessage.RenameCancelled)] = StatusTone.Hint,
        [typeof(StatusMessage.InvalidName)] = StatusTone.Notice,
        [typeof(StatusMessage.AlreadyUnderVantage)] = StatusTone.Notice,
        [typeof(StatusMessage.WorkingSoAdoptWaits)] = StatusTone.Notice,
        [typeof(StatusMessage.NoWorktreeSelected)] = StatusTone.Notice,
        [typeof(StatusMessage.NotRemoved)] = StatusTone.Notice,
        [typeof(StatusMessage.ForceCancelled)] = StatusTone.Hint,
        [typeof(StatusMessage.NothingTyped)] = StatusTone.Notice,
        [typeof(StatusMessage.TypedWrongName)] = StatusTone.Notice,
        [typeof(StatusMessage.NothingToPrune)] = StatusTone.Notice,
        [typeof(StatusMessage.LeftoverFolder)] = StatusTone.Notice,
        [typeof(StatusMessage.NoSessionIn)] = StatusTone.Notice,
        [typeof(StatusMessage.NoPullRequest)] = StatusTone.Notice,
        [typeof(StatusMessage.NothingToMeasure)] = StatusTone.Notice,
        [typeof(StatusMessage.NoSolution)] = StatusTone.Notice,
        [typeof(StatusMessage.Kept)] = StatusTone.Hint,
        [typeof(StatusMessage.NothingChosenToResume)] = StatusTone.Hint,
        [typeof(StatusMessage.UpToDate)] = StatusTone.Success,
        [typeof(StatusMessage.UpdateCheckOff)] = StatusTone.Notice,
        [typeof(StatusMessage.NoDialogFor)] = StatusTone.Failure,
        [typeof(StatusMessage.Working)] = StatusTone.Working,
        [typeof(StatusMessage.Reported)] = StatusTone.Success,
        [typeof(StatusMessage.BranchKept)] = StatusTone.Success,
        [typeof(StatusMessage.Followed)] = StatusTone.Success,
        [typeof(StatusMessage.Failed)] = StatusTone.Failure,
        [typeof(StatusMessage.SettingHint)] = StatusTone.Hint,
        [typeof(StatusMessage.SetupAdvice)] = StatusTone.Hint,
        [typeof(StatusMessage.NoPathHere)] = StatusTone.Notice,
        [typeof(StatusMessage.NoSettingSelected)] = StatusTone.Notice,
    };

    public static TheoryData<Type> FooterCases => [.. typeof(StatusMessage).GetNestedTypes().Where(type => type.IsAssignableTo(typeof(StatusMessage)))];

    public static TheoryData<Type, StatusTone> FooterTable => [.. FooterTones.Select(entry => (entry.Key, entry.Value))];

    [Theory]
    [MemberData(nameof(FooterCases))]
    public void ChooseAToneForEveryFooterCase(Type type)
    {
        FooterTones.ShouldContainKey(type, $"{type.Name} has no tone in this test's table: choose one, and an arm in Wording.ToneOf");
    }

    [Theory]
    [MemberData(nameof(FooterTable))]
    public void GiveEachFooterItsTone(Type type, StatusTone tone)
    {
        Wording.ToneOf((StatusMessage)Built(type)).ShouldBe(tone);
    }

    [Fact]
    public void MarkAReportWithItsOutcomesTone()
    {
        Wording.ToneOf(new StatusMessage.Reported(new ClipboardBusy())).ShouldBe(StatusTone.Notice);
        Wording.ToneOf(new StatusMessage.Reported(new StartSessionOutcome.Failed("no claude"))).ShouldBe(StatusTone.Failure);
    }

    [Fact]
    public void MarkAKeptBranchWithItsRemovalsTone()
    {
        Wording.ToneOf(new StatusMessage.BranchKept(new RemoveWorktreeOutcome.BranchKept("Northwind.Orders-fix-123", Branch, CreatedByVantage: true))).ShouldBe(StatusTone.Success);
        Wording.ToneOf(new StatusMessage.BranchKept(new DeleteSessionOutcome.WorktreeInUse("fix-123", "Northwind.Orders-fix-123"))).ShouldBe(StatusTone.Notice);
    }

    [Fact]
    public void MarkAResultAndWhatFollowedWithWhatFollowed()
    {
        Wording.ToneOf(new StatusMessage.Followed(new RemoveWorktreeOutcome.InUse("Northwind.Orders-fix-123"), new DeleteBranchOutcome())).ShouldBe(StatusTone.Success);
        Wording.ToneOf(new StatusMessage.Followed(new DeleteBranchOutcome(), new ClipboardBusy())).ShouldBe(StatusTone.Notice);
    }

    [Fact]
    public void MarkAFailedPullRequestLookUpAsAFailure()
    {
        Wording.ToneOf(new StatusMessage.NoPullRequest("fix-123", PullRequestGap.Failed, PullRequestHost.GitHub)).ShouldBe(StatusTone.Failure);
        Wording.ToneOf(new StatusMessage.NoPullRequest("fix-123", PullRequestGap.NoPullRequest, PullRequestHost.GitHub)).ShouldBe(StatusTone.Notice);
    }

    private static object Built(Type type)
    {
        System.Reflection.ConstructorInfo constructor = type.GetConstructors().Single(candidate => candidate.GetParameters().All(parameter => parameter.ParameterType != type));

        return constructor.Invoke([.. constructor.GetParameters().Select(parameter => Placeholder(parameter.ParameterType))]);
    }

    private static object Placeholder(Type type)
    {
        return type switch
        {
            _ when type == typeof(string) => "fix-123",
            _ when type == typeof(int) => 2,
            _ when type == typeof(bool) => true,
            _ when type == typeof(RemovalReason) => new RemovalReason.Locked(),
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
            _ when type == typeof(PullRequestHost?) => PullRequestHost.GitHub,
            _ when type == typeof(PullRequestGap) => PullRequestGap.NotSignedIn,
            _ when type == typeof(WorktreeHealthKind) => WorktreeHealthKind.Locked,
            _ when type == typeof(HealthReason) => new HealthReason.FolderGone(),
            _ when type == typeof(NameProblem) => NameProblem.Missing,
            _ when type == typeof(LaunchProblem) => new LaunchProblem.WorktreeNeedsLetters(),
            _ when type == typeof(ViewEffect) => new Quit(),
            _ when type == typeof(Outcome) => new StopSessionOutcome("fix-123", TabClosed: false),
            _ when type == typeof(SetupItem) => SetupItem.TerminalProfiles,
            _ when type == typeof(ConfigRow) => new ConfigRow.CheckForUpdates(On: true),
            _ => throw new InvalidOperationException($"A field of type {type.Name} needs a placeholder."),
        };
    }
}