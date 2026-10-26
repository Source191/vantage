using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Adapters.Claude;
using Source191.Vantage.Adapters.Git;
using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Launch;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application.Launch;

public sealed class HereCommandRunnerShould : IDisposable
{
    private const string Claude = @"C:\Users\me\.local\bin\claude.exe";
    private const string Directory = @"C:\Users\me\source\repos\Northwind.Orders";
    private const string Started = "Starting background service…\nbackgrounded · 81cdb0f9 · Northwind.Orders (idle — send a prompt to start)\n";

    private static readonly DateTimeOffset Noon = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeProcessRunner processes = new FakeProcessRunner()
        .AnswerAll(new ProcessResult(0, "[]", string.Empty))
        .Answer(request => request.FileName == Claude && request.Arguments.Contains("--bg"), new ProcessResult(0, Started, string.Empty));
    private readonly FakeFolders folders = new FakeFolders().WithDirectory(Path.Combine(Directory, ".git"));
    private readonly FakeTerminalConsole console = new();
    private readonly StringWriter output = new();
    private readonly StringWriter error = new();

    private HereCommandRunner Runner
    {
        get
        {
            ClaudeCli claude = new(processes, Claude);
            GitCli git = new(processes);
            InMemoryState state = new();
            LaunchService launches = new(claude, git, state, state, new WorktreeIncludeCopier(git), new FakeTimeProvider(Noon));

            return new HereCommandRunner(folders, launches, new TerminalEffects(claude, output), console, output, error);
        }
    }

    [Fact]
    public async Task StartInTheFolderAttachAndExitZeroAfterTheDetach()
    {
        int exitCode = await Runner.RunAsync(Directory, "fix-tests", "Fix the tests", CancellationToken.None);

        exitCode.ShouldBe(0);
        output.ToString().ShouldContain("Started fix-tests (81cdb0f9) in " + Directory);
        processes.CapturedRequests.Single(request => request.Arguments.Contains("--bg")).WorkingDirectory.ShouldBe(Directory);
        processes.InteractiveRequests.Single().Arguments.ShouldBe(["attach", "81cdb0f9"]);
        console.Recovered.ShouldBe(1);
    }

    [Fact]
    public async Task ExitOneWithTheTrustMessageWhenTheWorkspaceIsNotTrusted()
    {
        processes.Answer(request => request.Arguments.Contains("--bg"), new ProcessResult(1, string.Empty, "Workspace not trusted. Run `claude` once and accept the trust prompt."));

        int exitCode = await Runner.RunAsync(Directory, name: null, prompt: null, CancellationToken.None);

        exitCode.ShouldBe(1);
        error.ToString().ShouldContain("is not trusted yet");
        processes.InteractiveRequests.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExitOneWithTheMessageWhenTheStartFails()
    {
        processes.Answer(request => request.Arguments.Contains("--bg"), new ProcessResult(1, string.Empty, "something broke"));

        int exitCode = await Runner.RunAsync(Directory, name: null, prompt: null, CancellationToken.None);

        exitCode.ShouldBe(1);
        error.ToString().ShouldContain("Could not start: something broke");
    }

    [Fact]
    public async Task PrintOnlyTheSentenceForARefusedName()
    {
        int exitCode = await Runner.RunAsync(Directory, "a;b", prompt: null, CancellationToken.None);

        exitCode.ShouldBe(2);
        error.ToString().ShouldBe("A session name cannot contain a semicolon or a double quote, because Windows Terminal cannot pass them safely." + Environment.NewLine);
        processes.CapturedRequests.ShouldBeEmpty();
    }

    public void Dispose()
    {
        output.Dispose();
        error.Dispose();
    }
}