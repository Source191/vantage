using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Adapters.Claude;
using Source191.Vantage.Application;
using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class ScreenLoopShould : IDisposable
{
    private static readonly DateTimeOffset Noon = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);
    private static readonly SessionRow First = Row("sess-1", "change-feed");
    private static readonly SessionRow Second = Row("sess-2", "fix-tests");
    private static readonly ToolVersion Running = new("0.9.0");

    private readonly FakeProcessRunner processes = new FakeProcessRunner().AnswerAll(new ProcessResult(0, string.Empty, string.Empty));
    private readonly FakeProcessRegistry registry = new FakeProcessRegistry().Running(100, Noon);
    private readonly FakeTerminalConsole console = new();
    private readonly DiagnosticLog log = new(new FakeTimeProvider(Noon), []);
    private readonly StringWriter terminal = new();

    private ScreenLoop Loop(ScriptedConsoleScreen screen, UpdateCheck? updates = null)
    {
        OwnTab ownTab = new(new InMemoryState(), registry, new FakeTimeProvider(Noon));
        TerminalEffects effects = new(new ClaudeCli(processes, "claude.exe"), console);
        UpdateCheck checkTurnedOff = new(Running, () => false, new InMemoryState(), new FakePackageVersions([]), new FakeTimeProvider(Noon));

        return new ScreenLoop(screen, ownTab, console, effects, updates ?? checkTurnedOff, log);
    }

    [Fact]
    public async Task StartTheUpdateCheckWhenTheConsoleOpens()
    {
        InMemoryState records = new();
        await records.WriteUpdateCheckAsync(new UpdateCheckRecord(Noon.AddHours(-1), "0.10.0"));
        FakePackageVersions packages = new(["0.10.0"]);
        UpdateCheck updates = new(Running, () => true, records, packages, new FakeTimeProvider(Noon));
        ScreenLoop loop = Loop(new ScriptedConsoleScreen(state => new ConsoleExit(state, new Quit())), updates);

        await loop.RunAsync(processId: 100, CancellationToken.None);
        await loop.UpdateCheckTask;

        updates.Status.ShouldBe(new UpdateStatus.Available(ReleaseVersion.TryParse("0.9.0")!, ReleaseVersion.TryParse("0.10.0")!));
        packages.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task LogAFailedUpdateCheckAndKeepRunning()
    {
        UpdateCheck updates = new(Running, () => true, new UnwritableRecords(Noon.AddDays(-1)), new FakePackageVersions(["0.9.0"]), new FakeTimeProvider(Noon));
        ScreenLoop loop = Loop(new ScriptedConsoleScreen(state => new ConsoleExit(state, new Quit())), updates);

        int exitCode = await loop.RunAsync(processId: 100, CancellationToken.None);
        await loop.UpdateCheckTask;

        exitCode.ShouldBe(0);
        updates.Status.ShouldBe(new UpdateStatus.UpToDate());
        Failed failed = log.Snapshot().Select(timed => timed.Event).OfType<Failed>().Single();
        failed.Site.ShouldBe(FailureSite.UpdateCheck);
        failed.What.ShouldBe(CodeName.For(typeof(UpdateCheck)));
    }

    [Fact]
    public async Task LogNothingWhenTheUpdateCheckIsCancelled()
    {
        UpdateCheck updates = new(Running, () => true, new UnwritableRecords(Noon.AddDays(-1)), new FakePackageVersions(["0.9.0"]), new FakeTimeProvider(Noon));
        ScreenLoop loop = Loop(new ScriptedConsoleScreen(state => new ConsoleExit(state, new Quit())), updates);

        await loop.RunAsync(processId: 100, new CancellationToken(canceled: true));

        await Should.ThrowAsync<OperationCanceledException>(() => loop.UpdateCheckTask);
        log.Snapshot().Select(timed => timed.Event).OfType<Failed>().ShouldBeEmpty();
    }

    [Fact]
    public async Task LabelTheOwnTabAtStartAndClearItOnQuit()
    {
        ScriptedConsoleScreen screen = new(state => new ConsoleExit(state, new Quit()));

        int exitCode = await Loop(screen).RunAsync(processId: 100, CancellationToken.None);

        exitCode.ShouldBe(0);
        console.Shown.ShouldBe([TabPalette.Default[4]]);
        console.Cleared.ShouldBe(1);
    }

    [Fact]
    public async Task StartOnTheSessionsTabAndQuitWhenTheConsoleReturnsNoEffect()
    {
        ScriptedConsoleScreen screen = new(state => new ConsoleExit(state, null));

        await Loop(screen).RunAsync(processId: 100, CancellationToken.None);

        screen.Given.Single().ShouldBe(ConsoleState.Empty);
        console.Cleared.ShouldBe(1);
    }

    [Fact]
    public async Task RunTerminalEffectsBetweenRunsAndHandTheStateBack()
    {
        ConsoleState onWorktrees = ConsoleState.Empty with
        {
            Sessions = SessionsView.Reduce(SessionsView.Reduce(SessionsViewState.Empty, new RowsLoaded([First, Second])).State, new KeyPressed(Key.Down)).State,
            Active = ConsoleTab.Worktrees,
        };
        ScriptedConsoleScreen screen = new(
            _ => new ConsoleExit(onWorktrees, new AttachToSession(JobId.From("81cdb0f9"))),
            state => new ConsoleExit(state, new AttachToSession(JobId.From("92def0a1"))),
            state => new ConsoleExit(state, new Quit()));

        await Loop(screen).RunAsync(processId: 100, CancellationToken.None);

        processes.InteractiveRequests.Select(request => request.Arguments[0]).ShouldBe(["attach", "attach"]);
        screen.Given[1].ShouldBe(onWorktrees, "the console comes back to the tab and selection it left");
        screen.Given[1].Sessions.Selected.ShouldBe(Second);
        screen.Given[2].Active.ShouldBe(ConsoleTab.Worktrees);
        console.Recovered.ShouldBe(2);
        console.Shown.Count.ShouldBe(3, "once at start and once after each hand-off");
        log.Snapshot().Select(timed => timed.Event).ShouldBe([
            new HandingOver(CodeName.For(typeof(AttachToSession))),
            new ChildReturned(),
            new HandingOver(CodeName.For(typeof(AttachToSession))),
            new ChildReturned(),
        ]);
        log.Snapshot().Select(DiagnosticLine.Render).ShouldAllBe(line => !line.Contains("81cdb0f9") && !line.Contains("92def0a1"));
    }

    public void Dispose()
    {
        terminal.Dispose();
    }

    private static SessionRow Row(string sessionId, string name)
    {
        BackgroundSession session = new(SessionId.From(sessionId), JobId.From(sessionId == "sess-1" ? "81cdb0f9" : "92ddc1a0"), name, FolderPath.From(@"C:\Users\me\source\repos\Northwind.Orders"), Noon, DisplayState.Idle);

        return new SessionRow(session, "Northwind.Orders", BranchName.From("main"), HasLiveTab: false, HasTranscript: true);
    }

    /// <summary>An update-check record that reads but cannot be written, as when the state folder is read-only.</summary>
    private sealed class UnwritableRecords(DateTimeOffset checkedAt) : IUpdateCheckRecords
    {
        public Task<UpdateCheckRecord?> ReadUpdateCheckAsync()
        {
            return Task.FromResult<UpdateCheckRecord?>(new UpdateCheckRecord(checkedAt, "0.9.0"));
        }

        public Task WriteUpdateCheckAsync(UpdateCheckRecord record)
        {
            throw new UnauthorizedAccessException(@"Access to C:\Users\me\AppData\Local\Vantage\update-check.json is denied.");
        }
    }
}
