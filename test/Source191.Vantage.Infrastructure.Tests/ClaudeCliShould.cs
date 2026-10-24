using Source191.Vantage.Adapters.Claude;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Infrastructure.Tests;

public sealed class ClaudeCliShould
{
    private const string Executable = @"C:\Users\me\.local\bin\claude.exe";
    private const string Directory = @"C:\repos\Northwind.Orders";

    private const string StartedOutput = """
        Starting background service…
        backgrounded · 81cdb0f9 · cc-spike (idle — send a prompt to start)
          claude agents             list sessions
          claude attach 81cdb0f9    open in this terminal
        """;

    private readonly FakeProcessRunner processes = new FakeProcessRunner().AnswerAll(new ProcessResult(0, string.Empty, string.Empty));
    private readonly SessionName name = SessionName.Parse("Refund retry policy");

    private ClaudeCli Cli => new(processes, Executable);

    [Fact]
    public async Task ListSessionsByRunningTheJsonListingIncludingCompletedOnes()
    {
        processes.Answer(request => request.Arguments.Contains("agents"), new ProcessResult(0, """[{"sessionId":"81cdb0f9-0000-4000-8000-000000000001","id":"81cdb0f9","name":"one","cwd":"C:\\x","kind":"background","startedAt":1,"status":"idle","state":"done","pid":5}]""", string.Empty));

        IReadOnlyList<Session> sessions = await Cli.ListSessionsAsync(CancellationToken.None);

        processes.CapturedRequests.Single().ShouldSatisfyAllConditions(
            request => request.FileName.ShouldBe(Executable),
            request => request.Arguments.ShouldBe(["agents", "--json", "--all"]));
        sessions.Single().Name.ShouldBe("one");
    }

    [Fact]
    public async Task StartABackgroundSessionNamedInTheChosenDirectory()
    {
        processes.AnswerAll(new ProcessResult(0, StartedOutput, string.Empty));

        StartResult result = await Cli.StartBackgroundAsync(Directory, name, openingPrompt: null, CancellationToken.None);

        processes.CapturedRequests.Single().ShouldSatisfyAllConditions(
            request => request.WorkingDirectory.ShouldBe(Directory),
            request => request.Arguments.ShouldBe(["--bg", "-n", "Refund retry policy"]));
        result.Outcome.ShouldBe(StartOutcome.Started);
        result.JobId.ShouldBe("81cdb0f9");
    }

    [Fact]
    public async Task PassTheOpeningPromptAfterTheFlags()
    {
        processes.AnswerAll(new ProcessResult(0, StartedOutput, string.Empty));

        await Cli.StartBackgroundAsync(Directory, name, "Fix the flaky test", CancellationToken.None);

        processes.CapturedRequests.Single().Arguments.ShouldBe(["--bg", "-n", "Refund retry policy", "Fix the flaky test"]);
    }

    [Fact]
    public async Task ReportAnUntrustedWorkspaceWhenStartingIsRefused()
    {
        processes.AnswerAll(new ProcessResult(1, string.Empty, "Workspace not trusted. Run `claude` in C:\\repos\\Northwind.Orders once and accept the trust prompt, then retry."));

        StartResult result = await Cli.StartBackgroundAsync(Directory, name, openingPrompt: null, CancellationToken.None);

        result.Outcome.ShouldBe(StartOutcome.WorkspaceNotTrusted);
        result.JobId.ShouldBeNull();
    }

    [Fact]
    public async Task ReportAnyOtherStartFailureWithItsMessage()
    {
        processes.AnswerAll(new ProcessResult(1, string.Empty, "Couldn't start the background service"));

        StartResult result = await Cli.StartBackgroundAsync(Directory, name, openingPrompt: null, CancellationToken.None);

        result.Outcome.ShouldBe(StartOutcome.Failed);
        result.Message.ShouldContain("background service");
    }

    [Fact]
    public async Task AttachInteractivelyByJobId()
    {
        processes.InteractiveExitCode = 0;

        int exitCode = await Cli.AttachAsync("81cdb0f9", CancellationToken.None);

        processes.InteractiveRequests.Single().Arguments.ShouldBe(["attach", "81cdb0f9"]);
        exitCode.ShouldBe(0);
    }

    [Fact]
    public async Task StopAndRemoveByJobId()
    {
        await Cli.StopAsync("81cdb0f9", CancellationToken.None);
        await Cli.RemoveAsync("81cdb0f9", CancellationToken.None);

        processes.CapturedRequests.Select(request => request.Arguments).ShouldBe([["stop", "81cdb0f9"], ["rm", "81cdb0f9"]]);
    }

    [Fact]
    public async Task ResumeABackgroundSessionBySessionIdInItsDirectory()
    {
        processes.AnswerAll(new ProcessResult(0, "backgrounded · 81cdb0f9 · cc-spike", string.Empty));

        ResumeResult result = await Cli.ResumeBackgroundAsync("81cdb0f9-8c3f-4c9b-b478-ca0a5ebf85c6", Directory, name: null, CancellationToken.None);

        processes.CapturedRequests.Single().ShouldSatisfyAllConditions(
            request => request.WorkingDirectory.ShouldBe(Directory),
            request => request.Arguments.ShouldBe(["--bg", "--resume", "81cdb0f9-8c3f-4c9b-b478-ca0a5ebf85c6"]));
        result.Continued.ShouldBeTrue();
    }

    [Fact]
    public async Task KeepTheDisplayNameWhenResumingWithOne()
    {
        processes.AnswerAll(new ProcessResult(0, "backgrounded · 81cdb0f9 · cc-spike", string.Empty));

        await Cli.ResumeBackgroundAsync("81cdb0f9-8c3f-4c9b-b478-ca0a5ebf85c6", Directory, SessionName.Parse("cc-spike"), CancellationToken.None);

        processes.CapturedRequests.Single().Arguments.ShouldBe(["--bg", "--resume", "81cdb0f9-8c3f-4c9b-b478-ca0a5ebf85c6", "-n", "cc-spike"]);
    }

    [Fact]
    public async Task ReportWhenResumeStartedACopyInstead()
    {
        processes.AnswerAll(new ProcessResult(0, "backgrounded · 99aa88bb · cc-spike\nnote: the session is already running, so a copy was started", string.Empty));

        ResumeResult result = await Cli.ResumeBackgroundAsync("81cdb0f9-8c3f-4c9b-b478-ca0a5ebf85c6", Directory, name: null, CancellationToken.None);

        result.Continued.ShouldBeFalse();
        result.Message.ShouldContain("copy");
    }

    [Fact]
    public async Task ReportAResumeThatFailedOutrightWithWhatClaudeSaid()
    {
        processes.AnswerAll(new ProcessResult(1, string.Empty, "Session 81cdb0f9 can't start: source session not found\n"));

        ResumeResult result = await Cli.ResumeBackgroundAsync("81cdb0f9-8c3f-4c9b-b478-ca0a5ebf85c6", Directory, name: null, CancellationToken.None);

        result.Continued.ShouldBeFalse();
        result.Message.ShouldBe("Session 81cdb0f9 can't start: source session not found");
    }

    [Fact]
    public async Task OpenTheNativeAgentViewInteractively()
    {
        await Cli.OpenAgentViewAsync(CancellationToken.None);

        processes.InteractiveRequests.Single().Arguments.ShouldBe(["agents"]);
    }
}