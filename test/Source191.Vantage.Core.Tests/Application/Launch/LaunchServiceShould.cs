using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Adapters.Claude;
using Source191.Vantage.Adapters.Git;
using Source191.Vantage.Application.Launch;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application.Launch;

public sealed class LaunchServiceShould
{
    private const string Claude = @"C:\Users\me\.local\bin\claude.exe";
    private const string Started = "Starting background service…\nbackgrounded · 81cdb0f9 · Fix flaky tests (idle — send a prompt to start)\n";
    private const string Listing = """[{"pid":1,"id":"81cdb0f9","cwd":"C:\\repos\\Northwind.Orders","kind":"background","startedAt":1,"sessionId":"sess-1","name":"Fix flaky tests","status":"idle","state":"blocked"}]""";

    private static readonly DateTimeOffset Noon = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);
    private static readonly RepositoryChoice Repository = new("Northwind.Orders", FolderPath.From(@"C:\repos\Northwind.Orders"), IsRepository: true);

    private readonly FakeProcessRunner processes = new FakeProcessRunner()
        .AnswerAll(new ProcessResult(0, string.Empty, string.Empty))
        .Answer(request => request.FileName == Claude && request.Arguments.Contains("--bg"), new ProcessResult(0, Started, string.Empty))
        .Answer(request => request.FileName == Claude && request.Arguments.Contains("agents"), new ProcessResult(0, Listing, string.Empty))
        .Answer(request => request.FileName == "git" && request.Arguments.Contains("symbolic-ref"), new ProcessResult(0, "origin/main\n", string.Empty));

    private readonly InMemoryState Store = new();

    private LaunchService Service
    {
        get
        {
            GitCli git = new(processes, "git");
            return new LaunchService(new ClaudeCli(processes, Claude), git, Store, Store, new WorktreeIncludeCopier(git), new FakeTimeProvider(Noon));
        }
    }

    private static readonly PlannedWorktree FlakyTests = new(FolderPath.From(@"C:\Users\me\source\worktrees\Northwind.Orders-fix-flaky-tests"), BranchName.From("fix-flaky-tests"));

    private static LaunchPlan CurrentBranch => new(
        Repository.Path, SessionName.From("Fix flaky tests"), new LaunchPlace.CurrentBranch(),
        WorktreeBase.RemoteDefault, new OpeningPrompt.None(), Repository);

    private static LaunchPlan NewWorktree => CurrentBranch with
    {
        Directory = FolderPath.From(@"C:\Users\me\source\worktrees\Northwind.Orders-fix-flaky-tests"),
        Place = new LaunchPlace.NewWorktree(FlakyTests),
    };

    [Fact]
    public async Task StartTheSessionInTheRepositoryForACurrentBranchPlan()
    {
        LaunchOutcome outcome = await Service.LaunchAsync(CurrentBranch, CancellationToken.None);

        outcome.ShouldBeAssignableTo<LaunchOutcome.Started>().ShouldNotBeNull().JobId.ShouldBe(JobId.From("81cdb0f9"));
        ProcessRequest start = processes.CapturedRequests.Single(request => request.Arguments.Contains("--bg"));
        start.WorkingDirectory.ShouldBe(new WorkingFolder.At(@"C:\repos\Northwind.Orders"));
        processes.CapturedRequests.ShouldNotContain(request => request.Arguments.Contains("worktree"));
    }

    [Fact]
    public async Task CreateTheWorktreeUnderTheWorktreeRootFromTheRemoteDefaultBeforeStarting()
    {
        await Service.LaunchAsync(NewWorktree, CancellationToken.None);

        ProcessRequest add = processes.CapturedRequests.Single(request => request.Arguments.Contains("add"));
        add.WorkingDirectory.ShouldBe(new WorkingFolder.At(@"C:\repos\Northwind.Orders"));
        add.Arguments.ShouldBe(["worktree", "add", @"C:\Users\me\source\worktrees\Northwind.Orders-fix-flaky-tests", "-b", "fix-flaky-tests", "origin/main"]);
        processes.CapturedRequests.Single(request => request.Arguments.Contains("--bg")).WorkingDirectory.ShouldBe(new WorkingFolder.At(@"C:\Users\me\source\worktrees\Northwind.Orders-fix-flaky-tests"));
        (await Store.ReadCreatedWorktreesAsync()).Single().Branch.ShouldBe(BranchName.From("fix-flaky-tests"));
    }

    [Fact]
    public async Task RecordAWorktreeCreatedUnderTheWorktreeRoot()
    {
        await Service.LaunchAsync(NewWorktree, CancellationToken.None);

        CreatedWorktree created = (await Store.ReadCreatedWorktreesAsync()).Single();
        created.Path.ShouldBe(FolderPath.From(@"C:\Users\me\source\worktrees\Northwind.Orders-fix-flaky-tests"));
        created.Repository.ShouldBe(Repository.Path);
    }

    [Fact]
    public async Task BranchFromHeadWhenAskedOrWhenThereIsNoRemoteDefault()
    {
        await Service.LaunchAsync(NewWorktree with { Base = WorktreeBase.Head }, CancellationToken.None);
        processes.CapturedRequests.Single(request => request.Arguments.Contains("add")).Arguments[^1].ShouldBe("HEAD");

        processes.Answer(request => request.Arguments.Contains("symbolic-ref"), new ProcessResult(128, string.Empty, "fatal: not a symbolic ref"));
        await Service.LaunchAsync(NewWorktree, CancellationToken.None);
        processes.CapturedRequests.Last(request => request.Arguments.Contains("add")).Arguments[^1].ShouldBe("HEAD");
    }

    [Fact]
    public async Task SkipCreationWhenReusingAnExistingWorktree()
    {
        await Service.LaunchAsync(NewWorktree with { Place = new LaunchPlace.ExistingWorktree(FlakyTests) }, CancellationToken.None);

        processes.CapturedRequests.ShouldNotContain(request => request.Arguments.Contains("add"));
        (await Store.ReadCreatedWorktreesAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task RecordTheRepositoryAsRecent()
    {
        await Service.LaunchAsync(CurrentBranch, CancellationToken.None);

        (await Store.ReadRecentRepositoriesAsync()).ShouldBe([@"C:\repos\Northwind.Orders"]);
    }

    [Fact]
    public async Task ReportTheListedSessionAfterAStart()
    {
        LaunchOutcome outcome = await Service.LaunchAsync(CurrentBranch, CancellationToken.None);

        LaunchOutcome.Listed listed = outcome.ShouldBeOfType<LaunchOutcome.Listed>();
        listed.JobId.ShouldBe(JobId.From("81cdb0f9"));
        listed.Session.SessionId.Value.ShouldBe("sess-1");
    }

    [Fact]
    public async Task StillReportStartedWhenTheSessionIsNotListedYet()
    {
        processes.Answer(request => request.Arguments.Contains("agents"), new ProcessResult(0, "[]", string.Empty));

        LaunchOutcome outcome = await Service.LaunchAsync(CurrentBranch, CancellationToken.None);

        outcome.ShouldBe(new LaunchOutcome.NotListedYet(JobId.From("81cdb0f9")));
    }

    [Fact]
    public async Task ReportAnUntrustedWorkspaceWithoutThrowing()
    {
        processes.Answer(request => request.Arguments.Contains("--bg"), new ProcessResult(1, string.Empty, "Workspace not trusted. Run `claude` in C:\\repos\\Northwind.Orders once and accept the trust prompt, then retry."));

        LaunchOutcome outcome = await Service.LaunchAsync(CurrentBranch, CancellationToken.None);

        outcome.ShouldBeOfType<LaunchOutcome.WorkspaceNotTrusted>();
    }

    [Fact]
    public async Task ReportAStartTheCliRefusedAsAFailureWithItsMessage()
    {
        processes.Answer(request => request.Arguments.Contains("--bg"), new ProcessResult(1, string.Empty, "Couldn't start the background service"));

        LaunchOutcome outcome = await Service.LaunchAsync(CurrentBranch, CancellationToken.None);

        outcome.ShouldBe(new LaunchOutcome.Failed("Couldn't start the background service"));
    }

    [Fact]
    public async Task ReportAGitRefusalAsAFailureWithItsMessage()
    {
        processes.Answer(request => request.Arguments.Contains("add"), new ProcessResult(128, string.Empty, "fatal: 'fix-flaky-tests' is already checked out"));

        LaunchOutcome outcome = await Service.LaunchAsync(NewWorktree, CancellationToken.None);

        outcome.ShouldBeOfType<LaunchOutcome.Failed>().Message.ShouldContain("already checked out");
        processes.CapturedRequests.ShouldNotContain(request => request.Arguments.Contains("--bg"));
    }
}