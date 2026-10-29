using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Adapters.Claude;
using Source191.Vantage.Adapters.Git;
using Source191.Vantage.Application;
using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Launch;
using Source191.Vantage.Application.Reporting;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;
using System.Web;

namespace Source191.Vantage.Core.Tests.Application.Diagnostics;

/// <summary>
/// The promise the diagnostic log makes: Vantage's real collaborators, run through the flows a user does every day over
/// fixtures full of names, folders, ids, a prompt and program output that names them, record none of those values in
/// any line, in the counts or in the crash file.
/// </summary>
public sealed class DiagnosticsPrivacyShould
{
    private const string Claude = @"C:\Users\me\.local\bin\claude.exe";
    private const string Vantage = @"C:\Users\me\.dotnet\tools\vantage.exe";
    private const string Repository = @"C:\Users\me\source\repos\Northwind.Orders";
    private const string WorktreeRoot = @"C:\Users\me\source\worktrees";
    private const string Worktree = @"C:\Users\me\source\worktrees\Northwind.Orders-fix-flaky-tests";
    private const string Branch = "fix-flaky-tests";
    private const string Name = "Fix flaky tests";
    private const string Job = "81cdb0f9";
    private const string SessionIdentity = "1a4d2d86-8e87-44fe-a9f7-61d47d481ae3";
    private const string Prompt = "Refactor the PaymentGateway retry loop";
    private const string GitRefusal = @"fatal: 'C:/Users/me/source/worktrees/Northwind.Orders-fix-flaky-tests' contains modified or untracked files, use --force to delete it";

    private static readonly string Listing = $$"""[{"pid":4242,"id":"{{Job}}","cwd":"C:\\Users\\me\\source\\repos\\Northwind.Orders","kind":"background","startedAt":1,"sessionId":"{{SessionIdentity}}","name":"{{Name}}","status":"idle","state":"blocked"}]""";
    private static readonly string Started = $"Starting background service…\nbackgrounded · {Job} · {Name} (idle — send a prompt to start)\n";

    /// <summary>Every value the fixtures above seed, in each spelling a program might print it.</summary>
    private static readonly string[] Seeded =
    [
        @"C:\Users\me", "C:/Users/me", @"C:\\Users\\me", "Northwind", Branch, Name, Job, SessionIdentity, Prompt, "PaymentGateway", "fatal", "modified or untracked",
    ];

    private static readonly DateTimeOffset Noon = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider clock = new(Noon);
    private readonly RecordingSink sink = new();
    private readonly ProcessCounts counts = new();
    private readonly FakeCrashFile crashFile = new();
    private readonly DiagnosticLog log;
    private readonly RecordedProcessRunner processes;

    public DiagnosticsPrivacyShould()
    {
        log = new DiagnosticLog(clock, [sink]);
        FakeProcessRunner children = new FakeProcessRunner()
            .AnswerAll(new ProcessResult(0, $"worktree {Repository}\nbranch refs/heads/{Branch}\n", string.Empty))
            .Answer(request => request.Arguments.Contains("--bg"), new ProcessResult(0, Started, string.Empty))
            .Answer(request => request.Arguments.Contains("agents"), new ProcessResult(0, Listing, string.Empty))
            .Answer(request => request.Arguments.Contains("remove"), new ProcessResult(128, string.Empty, GitRefusal));
        processes = new RecordedProcessRunner(children, log, counts, clock);
    }

    [Fact]
    public async Task RecordNothingFromTheFixtures()
    {
        await RunTheDailyFlowsAsync();

        IReadOnlyList<string> lines = sink.Lines;
        lines.ShouldContain(line => line.Contains("hand-off AttachToSession", StringComparison.Ordinal));
        lines.ShouldContain(line => line.Contains("effect StartSession -> StartSessionOutcome.Started", StringComparison.Ordinal));
        lines.ShouldContain(line => line.Contains("failed Effect RemoveWorktree: GitException", StringComparison.Ordinal));
        lines.ShouldContain(line => line.Contains("failed Load SessionsViewState: InvalidOperationException", StringComparison.Ordinal));
        lines.ShouldContain(line => line.Contains("failed Crash: InvalidOperationException", StringComparison.Ordinal));
        DiagnosticLine.Render(counts).ShouldContain("counts git worktree remove exit 128: 1 run, longest 0 ms");
        string everything = string.Join('\n', [.. lines, crashFile.Written ?? string.Empty, .. DiagnosticLine.Render(counts)]);
        Seeded.Where(value => everything.Contains(value, StringComparison.OrdinalIgnoreCase)).ShouldBeEmpty();
    }

    [Fact]
    public async Task ReportNothingFromTheFixturesButTheTitleTyped()
    {
        await RunTheDailyFlowsAsync();
        ReportTitle title = ReportTitle.From("Northwind.Orders secret title");
        Started start = new(new ToolVersion("0.17.0-preview.6"), new Version(2, 1, 312), new Version(10, 0, 26100), new Version(10, 0, 14), 120, 36);
        ProblemReport report = await new ProblemReports(log, counts, crashFile, start, TestSettings.Current(Repository)).BuildAsync();
        FakeDesktop desktop = new();
        EffectDispatcher dispatcher = new EffectDispatcher(log, clock).Register(new OpenProblemReportHandler(desktop, crashFile));

        await dispatcher.DispatchAsync(new OpenProblemReport(title, report), CancellationToken.None);

        report.HasCrash.ShouldBeTrue();
        string diagnostics = HttpUtility.ParseQueryString(new Uri(desktop.Opened.Single()).Query)[ProblemReportAddress.DiagnosticsField]!;
        diagnostics.ShouldBe(report.Fitted);
        string everything = string.Join('\n', report.Text, diagnostics, desktop.Opened.Single().Replace(Uri.EscapeDataString(title.Value), string.Empty, StringComparison.Ordinal));
        Seeded.Where(value => everything.Contains(value, StringComparison.OrdinalIgnoreCase)).ShouldBeEmpty();
        sink.Lines.ShouldContain(line => line.Contains("effect OpenProblemReport -> ProblemReportOutcome.Opened", StringComparison.Ordinal));
        sink.Lines.ShouldAllBe(line => !line.Contains("secret title", StringComparison.Ordinal));
    }

    private async Task RunTheDailyFlowsAsync()
    {
        ClaudeCli claude = new(processes, Claude);
        GitCli git = new(processes);
        CrashRecorder crashes = new(log, counts, crashFile);

        await LoadTheSessionsAsync(claude);
        await AttachAsync(claude);
        await StartASessionWithAPromptAsync(claude, git);
        await RemoveADirtyWorktreeAsync(git);
        await FailALoadAsync();
        await Should.ThrowAsync<InvalidOperationException>(() => crashes.GuardAsync(() => throw new InvalidOperationException($"{Name} in {Worktree} stopped: {Prompt}")));
    }

    private async Task LoadTheSessionsAsync(ClaudeCli claude)
    {
        IReadOnlyList<Session> sessions = await Attempt.RunAsync(log, FailureSite.Load, CodeName.For(typeof(SessionsViewState)), () => claude.ListSessionsAsync(CancellationToken.None), _ => []);

        sessions.Single().Name.ShouldBe(Name);
    }

    private async Task AttachAsync(ClaudeCli claude)
    {
        InMemoryState state = new();
        OwnTab ownTab = new(state, new FakeProcessRegistry().Running(100, Noon), clock);
        UpdateCheck updates = new(new ToolVersion("0.17.0"), () => false, state, new FakePackageVersions([]), clock);
        ScriptedSessionsScreen sessions = new(
            current => new ScreenExit(current, new AttachToSession(JobId.From(Job))),
            current => new ScreenExit(current, new Quit()));
        ScreenLoop loop = new(sessions, new ScriptedWorktreesScreen(), ownTab, new FakeTerminalConsole(), new TerminalEffects(claude, new StringWriter()), updates, log);

        await loop.RunAsync(processId: 100, CancellationToken.None);
    }

    private async Task StartASessionWithAPromptAsync(ClaudeCli claude, GitCli git)
    {
        InMemoryState store = new();
        LaunchService launches = new(claude, git, store, store, new WorktreeIncludeCopier(git), clock);
        EffectPorts ports = new(claude, git, new FakeProcessRegistry(), new FakeTranscripts(), new FakeWindowsTerminal(), store, clock, new FakeClipboard(), new FakeDesktop(), new FakeEditorLocator(), new FakeFolders(), new FakeCrashFile());
        EffectDispatcher dispatcher = Composition.Handlers(ports, launches, new TabScripts(Claude, Vantage), TestSettings.Current(), new VantageProcess(4242, FolderPath.From(@"C:\Users\me")), log);
        PlannedWorktree worktree = PlannedWorktree.For(FolderPath.From(WorktreeRoot), FolderPath.From(Repository), WorktreeSlug.From(Branch));
        LaunchPlan plan = new(FolderPath.From(Repository), SessionName.From(Name), worktree, ReuseExistingWorktree: false, WorktreeBase.RemoteDefault, Prompt, new RepositoryChoice("Northwind.Orders", FolderPath.From(Repository), IsRepository: true));

        Outcome outcome = await dispatcher.DispatchAsync(new StartSession(plan, OpenInTab: false), CancellationToken.None);

        outcome.ShouldBeOfType<StartSessionOutcome.Started>();
    }

    private async Task RemoveADirtyWorktreeAsync(GitCli git)
    {
        string footer = await Attempt.RunAsync(log, FailureSite.Effect, CodeName.For(typeof(RemoveWorktree)), async () =>
        {
            await git.RemoveWorktreeAsync(Repository, Worktree, force: false, CancellationToken.None);
            return "removed";
        }, exception => exception.Message);

        footer.ShouldContain("modified or untracked");
    }

    private async Task FailALoadAsync()
    {
        await Attempt.RunAsync<int>(log, FailureSite.Load, CodeName.For(typeof(SessionsViewState)), () => throw new InvalidOperationException($"Could not read {Repository} for {Name}"), _ => 0);
    }
}