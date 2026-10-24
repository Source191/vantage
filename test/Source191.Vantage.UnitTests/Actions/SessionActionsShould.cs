using Source191.Vantage.Actions;
using Source191.Vantage.Adapters.Claude;
using Source191.Vantage.Adapters.Git;
using Source191.Vantage.Adapters.Processes;
using Source191.Vantage.Adapters.State;
using Source191.Vantage.Domain;
using Source191.Vantage.Screens;
using Source191.Vantage.UnitTests.TestDoubles;

namespace Source191.Vantage.UnitTests.Actions;

public sealed class SessionActionsShould : IDisposable
{
    private const string Claude = @"C:\Users\me\.local\bin\claude.exe";
    private const string Repository = @"C:\repos\Northwind.Orders";
    private const string Worktree = @"C:\repos\Northwind.Orders-fix-123";

    private static readonly DateTimeOffset Noon = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    private readonly string stateFolder = Path.Combine(Path.GetTempPath(), "vantage-actions-" + Guid.NewGuid().ToString("N"));
    private readonly FakeProcessRunner processes = new FakeProcessRunner()
        .AnswerAll(new ProcessResult(0, string.Empty, string.Empty))
        .Answer(request => request.Arguments.Contains("--porcelain") && request.Arguments.Contains("list"), new ProcessResult(0, $"worktree C:/repos/Northwind.Orders\nHEAD a\nbranch refs/heads/main\n\nworktree C:/repos/Northwind.Orders-fix-123\nHEAD b\nbranch refs/heads/fix-123\n", string.Empty))
        .Answer(request => request.Arguments.Contains("status"), new ProcessResult(0, string.Empty, string.Empty))
        .Answer(request => request.Arguments.Contains("rev-list"), new ProcessResult(0, "0\n", string.Empty))
        .Answer(request => request.Arguments.Contains("symbolic-ref"), new ProcessResult(0, "origin/main\n", string.Empty))
        .Answer(request => request.Arguments.Contains("merge-base"), new ProcessResult(0, string.Empty, string.Empty));

    private StateStore Store => new(stateFolder, TimeProvider.System);

    private readonly FakeProcessRegistry processTable = new FakeProcessRegistry().Running(4242, Noon);

    private readonly FakeTranscripts transcripts = new FakeTranscripts().WithMessages("sess-9");

    private SessionActions Actions => new(new ClaudeCli(processes, Claude), new GitCli(processes), Store, processTable, transcripts);

    [Fact]
    public async Task RefuseToAdoptASessionThatHasNoMessagesYetBecauseThereIsNothingToResume()
    {
        FakeTranscripts none = new();
        SessionActions actions = new(new ClaudeCli(processes, Claude), new GitCli(processes), Store, processTable, none);

        string message = await actions.AdoptAsync(Interactive("repos-f6", 4242), CancellationToken.None);

        message.ShouldBe("repos-f6 has no messages yet, so there is nothing to move. Send it something in its tab first, or close that tab and start a session from Vantage.");
        processTable.Terminated.ShouldBeEmpty();
        processes.CapturedRequests.ShouldBeEmpty();
    }

    private static SessionRow Interactive(string name, int? processId, DisplayState? state = null)
    {
        Session session = new("sess-9", JobId: null, name, Repository, SessionKind.Interactive, Noon, state ?? DisplayState.Idle, processId);

        return new SessionRow(session, "Northwind.Orders", "main", IsWorktree: false, Description: null, HasLiveTab: false, HasTranscript: true);
    }

    [Fact]
    public async Task AdoptAnInteractiveSessionByEndingItsProcessAndResumingItUnderTheSupervisor()
    {
        processes.Answer(request => request.Arguments.Contains("--resume"), new ProcessResult(0, "backgrounded · 81cdb0f9 · repos-31", string.Empty));

        string message = await Actions.AdoptAsync(Interactive("repos-31", 4242), CancellationToken.None);

        processTable.Terminated.ShouldBe([4242]);
        ProcessRequest resume = processes.CapturedRequests.Single(request => request.Arguments.Contains("--resume"));
        resume.WorkingDirectory.ShouldBe(Repository);
        resume.Arguments.ShouldBe(["--bg", "--resume", "sess-9", "-n", "repos-31"]);
        message.ShouldBe("Moved repos-31 under Vantage; its old tab is now a plain shell.");
    }

    [Fact]
    public async Task ReportWhenTheResumeStartedACopy()
    {
        processes.Answer(request => request.Arguments.Contains("--resume"), new ProcessResult(0, "backgrounded · 99aa88bb · repos-31\nnote: the session is already running, so a copy was started", string.Empty));

        string message = await Actions.AdoptAsync(Interactive("repos-31", 4242), CancellationToken.None);

        message.ShouldStartWith("Could not move repos-31 under Vantage:");
        message.ShouldContain("copy");
    }

    [Fact]
    public async Task CloseTheTabOfATerminalSessionWithNoMessagesByEndingClaudeThenItsShellCleanly()
    {
        // WindowsTerminal (1) owns the tab's pwsh (4000), which started claude (4242); ending the shell with exit code 0 closes the tab.
        processTable.InTree(1, "WindowsTerminal", 0).InTree(4000, "pwsh", 1).InTree(4242, "claude", 4000);
        SessionActions actions = new(new ClaudeCli(processes, Claude), new GitCli(processes), Store, processTable, new FakeTranscripts());

        string message = await actions.CloseEmptyTerminalsAsync([Interactive("repos-f6", 4242)], CancellationToken.None);

        processTable.Terminated.ShouldBe([4242]);
        processTable.EndedCleanly.ShouldBe([4000]);
        processes.CapturedRequests.ShouldBeEmpty();
        message.ShouldBe("Closed 1 tab: repos-f6.");
    }

    [Fact]
    public async Task EndClaudeAloneWhenItIsNotInsideAWindowsTerminalTab()
    {
        processTable.InTree(4242, "claude", 9);
        SessionActions actions = new(new ClaudeCli(processes, Claude), new GitCli(processes), Store, processTable, new FakeTranscripts());

        string message = await actions.CloseEmptyTerminalsAsync([Interactive("repos-f6", 4242)], CancellationToken.None);

        processTable.Terminated.ShouldBe([4242]);
        processTable.EndedCleanly.ShouldBeEmpty();
        message.ShouldBe("Ended 1 session: repos-f6 (not in a Windows Terminal tab, so nothing to close).");
    }

    [Fact]
    public async Task SkipTerminalSessionsThatHaveMessagesOrNoProcessWhenClosingEmptyOnes()
    {
        transcripts.WithMessages("sess-9");

        string message = await Actions.CloseEmptyTerminalsAsync([Interactive("repos-31", 4242), Interactive("repos-93", processId: null)], CancellationToken.None);

        processTable.Terminated.ShouldBeEmpty();
        processTable.EndedCleanly.ShouldBeEmpty();
        message.ShouldBe("Nothing closed: repos-31 has messages (press b to move it under Vantage); repos-93 has no process.");
    }

    [Fact]
    public async Task RefuseToAdoptASessionWhoseProcessIsUnknown()
    {
        string message = await Actions.AdoptAsync(Interactive("repos-31", processId: null), CancellationToken.None);

        message.ShouldBe("repos-31 has no process to move; it may have exited.");
        processTable.Terminated.ShouldBeEmpty();
        processes.CapturedRequests.ShouldBeEmpty();
    }

    private static SessionRow Row(string directory, bool isWorktree, DisplayState? state = null)
    {
        Session session = new("sess-1", "81cdb0f9", "fix-123", directory, SessionKind.Background, Noon, state ?? DisplayState.Working);

        return new SessionRow(session, "Northwind.Orders", isWorktree ? "fix-123" : "main", isWorktree, Description: null, HasLiveTab: false, HasTranscript: true);
    }

    [Fact]
    public async Task StopTheSessionByJobId()
    {
        string message = await Actions.StopAsync(Row(Repository, isWorktree: false), CancellationToken.None);

        processes.CapturedRequests.Single(request => request.FileName == Claude).Arguments.ShouldBe(["stop", "81cdb0f9"]);
        message.ShouldContain("Stopped fix-123");
    }

    [Fact]
    public async Task DeleteASessionOutsideAVantageWorktreeWithoutTouchingGit()
    {
        DeleteOutcome outcome = await Actions.DeleteAsync(Row(Repository, isWorktree: false), forced: false, CancellationToken.None);

        processes.CapturedRequests.Single(request => request.FileName == Claude).Arguments.ShouldBe(["rm", "81cdb0f9"]);
        processes.CapturedRequests.ShouldNotContain(request => request.FileName == "git");
        outcome.Message.ShouldContain("Deleted fix-123");
        outcome.BranchToConfirm.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteASessionAndRemoveItsCleanVantageWorktreeAndMergedBranch()
    {
        await Store.RecordCreatedWorktreeAsync(new CreatedWorktree(Worktree, "fix-123", Repository, Noon));

        DeleteOutcome outcome = await Actions.DeleteAsync(Row(Worktree, isWorktree: true), forced: false, CancellationToken.None);

        processes.CapturedRequests.Single(request => request.Arguments.Contains("remove")).Arguments.ShouldBe(["worktree", "remove", Worktree]);
        processes.CapturedRequests.Single(request => request.Arguments.Contains("branch")).Arguments.ShouldBe(["branch", "-d", "fix-123"]);
        (await Store.ReadCreatedWorktreesAsync()).ShouldBeEmpty();
        outcome.Message.ShouldContain("removed the worktree");
    }

    [Fact]
    public async Task KeepAVantageWorktreeThatStillHasChanges()
    {
        await Store.RecordCreatedWorktreeAsync(new CreatedWorktree(Worktree, "fix-123", Repository, Noon));
        processes.Answer(request => request.Arguments.Contains("status"), new ProcessResult(0, " M a.cs\n", string.Empty));

        DeleteOutcome outcome = await Actions.DeleteAsync(Row(Worktree, isWorktree: true), forced: false, CancellationToken.None);

        processes.CapturedRequests.ShouldNotContain(request => request.Arguments.Contains("remove"));
        outcome.Message.ShouldContain("kept");
        outcome.Message.ShouldContain("1 changed");
    }

    [Fact]
    public async Task AskAboutTheBranchWhenItIsNotMerged()
    {
        await Store.RecordCreatedWorktreeAsync(new CreatedWorktree(Worktree, "fix-123", Repository, Noon));
        processes.Answer(request => request.Arguments.Contains("merge-base"), new ProcessResult(1, string.Empty, string.Empty));

        DeleteOutcome outcome = await Actions.DeleteAsync(Row(Worktree, isWorktree: true), forced: false, CancellationToken.None);

        processes.CapturedRequests.ShouldContain(request => request.Arguments.Contains("remove"));
        processes.CapturedRequests.ShouldNotContain(request => request.Arguments.Contains("branch"));
        outcome.BranchToConfirm.ShouldBe(new BranchToConfirm(Repository, "fix-123"));
    }

    [Fact]
    public async Task DeleteABranchOnRequestWithForce()
    {
        await Actions.DeleteBranchAsync(new BranchToConfirm(Repository, "fix-123"), CancellationToken.None);

        processes.CapturedRequests.Single().Arguments.ShouldBe(["branch", "-D", "fix-123"]);
    }

    [Fact]
    public void SeparateFailedSessionsFromStoppedOnesWhenPlanningAResume()
    {
        SessionRow failed = Row(Repository, isWorktree: false, DisplayState.Failed);
        SessionRow stopped = Row(Repository, isWorktree: false, DisplayState.Stopped) with { Session = Row(Repository, false).Session with { SessionId = "sess-2", State = DisplayState.Stopped } };
        SessionRow stoppedLater = stopped with { Session = stopped.Session with { SessionId = "sess-3", Name = "later" } };
        SessionRow working = Row(Repository, isWorktree: false);

        ResumePlan plan = SessionActions.PlanResume([failed, stoppedLater, stopped, working]);

        plan.Automatic.ShouldBe([failed]);
        plan.ToChoose.ShouldBe([stoppedLater, stopped], "the checklist keeps the order it was given, which is the table's order");
    }

    [Fact]
    public async Task RenameBySwappingTheJobForOneResumedUnderTheNewNameKeepingTheSessionId()
    {
        processes.Answer(request => request.Arguments.Contains("--resume"), new ProcessResult(0, "backgrounded · 81cdb0f9 · fix-124", string.Empty));
        transcripts.WithMessages("sess-1");

        string message = await Actions.RenameAsync(Row(Repository, isWorktree: false, DisplayState.Idle), SessionName.Parse("fix-124"), CancellationToken.None);

        List<ProcessRequest> claude = [.. processes.CapturedRequests.Where(request => request.FileName == Claude)];
        claude.Select(request => string.Join(' ', request.Arguments)).ShouldBe(["rm 81cdb0f9", "--bg --resume sess-1 -n fix-124"]);
        claude[1].WorkingDirectory.ShouldBe(Repository);
        message.ShouldBe("Renamed fix-123 to fix-124; it restarts idle and keeps its conversation.");
    }

    [Fact]
    public async Task RefuseToRenameASessionWithNoTranscriptBecauseTheSwapWouldLoseIt()
    {
        string message = await Actions.RenameAsync(Row(Repository, isWorktree: false, DisplayState.Idle), SessionName.Parse("fix-124"), CancellationToken.None);

        message.ShouldBe("fix-123 has no messages yet, so it cannot be renamed; send it something first.");
        processes.CapturedRequests.ShouldBeEmpty();
    }

    [Fact]
    public async Task PutTheOldNameBackWhenTheResumeUnderTheNewNameFails()
    {
        processes.Answer(request => request.Arguments.Contains("fix-124"), new ProcessResult(1, string.Empty, "boom"));
        processes.Answer(request => request.Arguments.Contains("fix-123"), new ProcessResult(0, "backgrounded · 81cdb0f9 · fix-123", string.Empty));
        transcripts.WithMessages("sess-1");

        string message = await Actions.RenameAsync(Row(Repository, isWorktree: false, DisplayState.Idle), SessionName.Parse("fix-124"), CancellationToken.None);

        processes.CapturedRequests.Where(request => request.FileName == Claude).Select(request => request.Arguments[^1]).ShouldBe(["81cdb0f9", "fix-124", "fix-123"]);
        message.ShouldStartWith("Could not rename fix-123:");
        message.ShouldEndWith("it is back under its old name.");
    }

    [Fact]
    public async Task SkipASessionWithNoTranscriptWhenResumingAndSaySo()
    {
        processes.Answer(request => request.Arguments.Contains("--resume"), new ProcessResult(0, "backgrounded · 81cdb0f9 · fix-123", string.Empty));
        transcripts.WithMessages("sess-1");
        SessionRow withTranscript = Row(Repository, isWorktree: false, DisplayState.Failed);
        SessionRow stub = withTranscript with { Session = withTranscript.Session with { SessionId = "sess-stub", Name = "repos-f6" } };

        string message = await Actions.ResumeAsync([withTranscript, stub], CancellationToken.None);

        processes.CapturedRequests.Count(request => request.Arguments.Contains("--resume")).ShouldBe(1);
        message.ShouldBe("Resumed 1 of 2. Notes: repos-f6: never sent a message, so there is nothing to resume; d deletes it");
    }

    [Fact]
    public async Task ResumeEachChosenSessionInItsOwnDirectory()
    {
        processes.Answer(request => request.Arguments.Contains("--resume"), new ProcessResult(0, "backgrounded · 81cdb0f9 · fix-123", string.Empty));
        transcripts.WithMessages("sess-1").WithMessages("sess-2");
        SessionRow first = Row(Repository, isWorktree: false, DisplayState.Failed);
        SessionRow second = Row(Worktree, isWorktree: true, DisplayState.Stopped) with { Session = Row(Worktree, true).Session with { SessionId = "sess-2" } };

        string message = await Actions.ResumeAsync([first, second], CancellationToken.None);

        List<ProcessRequest> resumes = [.. processes.CapturedRequests.Where(request => request.Arguments.Contains("--resume"))];
        resumes.Select(request => request.WorkingDirectory).ShouldBe([Repository, Worktree]);
        resumes.Select(request => request.Arguments[2]).ShouldBe(["sess-1", "sess-2"]);
        resumes.Select(request => request.Arguments[^1]).ShouldAllBe(name => name == "fix-123", "the display name survives the resume");
        message.ShouldContain("Resumed 2");
    }

    public void Dispose()
    {
        if (Directory.Exists(stateFolder))
        {
            Directory.Delete(stateFolder, recursive: true);
        }
    }
}