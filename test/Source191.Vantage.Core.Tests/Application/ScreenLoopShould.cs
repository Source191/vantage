using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Adapters.Claude;
using Source191.Vantage.Application;
using Source191.Vantage.Application.Actions;
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
    private readonly RecordingLogger log = new();
    private readonly StringWriter terminal = new();

    private ScreenLoop Loop(ScriptedSessionsScreen sessions, ScriptedWorktreesScreen worktrees, UpdateCheck? updates = null)
    {
        OwnTab ownTab = new(new InMemoryState(), registry, new FakeTimeProvider(Noon));
        TerminalEffects effects = new(new ClaudeCli(processes, "claude.exe"), console);
        UpdateCheck checkTurnedOff = new(Running, () => false, new InMemoryState(), new FakePackageVersions([]), new FakeTimeProvider(Noon));

        return new ScreenLoop(sessions, worktrees, ownTab, console, effects, updates ?? checkTurnedOff, log);
    }

    [Fact]
    public async Task StartTheUpdateCheckWhenTheConsoleOpens()
    {
        InMemoryState records = new();
        await records.WriteUpdateCheckAsync(new UpdateCheckRecord(Noon.AddHours(-1), "0.10.0"));
        FakePackageVersions packages = new(["0.10.0"]);
        UpdateCheck updates = new(Running, () => true, records, packages, new FakeTimeProvider(Noon));
        ScreenLoop loop = Loop(new ScriptedSessionsScreen(state => new ScreenExit(state, new Quit())), new ScriptedWorktreesScreen(), updates);

        await loop.RunAsync(processId: 100, CancellationToken.None);
        await loop.UpdateCheckTask;

        updates.Status.ShouldBe(new UpdateStatus.Available(ReleaseVersion.TryParse("0.9.0")!, ReleaseVersion.TryParse("0.10.0")!));
        packages.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task LogAFailedUpdateCheckAndKeepRunning()
    {
        UpdateCheck updates = new(Running, () => true, new UnwritableRecords(Noon.AddDays(-1)), new FakePackageVersions(["0.9.0"]), new FakeTimeProvider(Noon));
        ScreenLoop loop = Loop(new ScriptedSessionsScreen(state => new ScreenExit(state, new Quit())), new ScriptedWorktreesScreen(), updates);

        int exitCode = await loop.RunAsync(processId: 100, CancellationToken.None);
        await loop.UpdateCheckTask;

        exitCode.ShouldBe(0);
        updates.Status.ShouldBe(new UpdateStatus.UpToDate());
        log.Lines.ShouldBe(["The update check failed"]);
    }

    [Fact]
    public async Task LogNothingWhenTheUpdateCheckIsCancelled()
    {
        UpdateCheck updates = new(Running, () => true, new UnwritableRecords(Noon.AddDays(-1)), new FakePackageVersions(["0.9.0"]), new FakeTimeProvider(Noon));
        ScreenLoop loop = Loop(new ScriptedSessionsScreen(state => new ScreenExit(state, new Quit())), new ScriptedWorktreesScreen(), updates);

        await loop.RunAsync(processId: 100, new CancellationToken(canceled: true));

        await Should.ThrowAsync<OperationCanceledException>(() => loop.UpdateCheckTask);
        log.Lines.ShouldBeEmpty();
    }

    [Fact]
    public async Task LabelTheOwnTabAtStartAndClearItOnQuit()
    {
        ScriptedSessionsScreen sessions = new(state => new ScreenExit(state, new Quit()));

        int exitCode = await Loop(sessions, new ScriptedWorktreesScreen()).RunAsync(processId: 100, CancellationToken.None);

        exitCode.ShouldBe(0);
        console.Shown.ShouldBe([TabPalette.Default[4]]);
        console.Cleared.ShouldBe(1);
    }

    [Fact]
    public async Task AlternateScreensAndSelectTheJumpedToSessionById()
    {
        ScriptedSessionsScreen sessions = new(
            state => new ScreenExit(SessionsView.Reduce(state, new RowsLoaded([First, Second])).State, new SwitchToWorktrees()),
            state => new ScreenExit(state, new SwitchToWorktrees()),
            state => new ScreenExit(state, null));
        ScriptedWorktreesScreen worktrees = new(new JumpToSession(SessionId.From("sess-2")), new SwitchToSessions());

        await Loop(sessions, worktrees).RunAsync(processId: 100, CancellationToken.None);

        worktrees.Runs.ShouldBe(2);
        sessions.Given[1].Selected.ShouldBe(Second);
        sessions.Given[2].Selected.ShouldBe(Second, "the selection survives the second trip");
    }

    [Fact]
    public async Task RunTerminalEffectsBetweenScreensAndRelabelTheTabAfterwards()
    {
        ScriptedSessionsScreen sessions = new(
            state => new ScreenExit(state, new AttachToSession(JobId.From("81cdb0f9"))),
            state => new ScreenExit(state, new OpenAgentView()),
            state => new ScreenExit(state, new Quit()));

        await Loop(sessions, new ScriptedWorktreesScreen()).RunAsync(processId: 100, CancellationToken.None);

        processes.InteractiveRequests.Select(request => request.Arguments[0]).ShouldBe(["attach", "agents"]);
        console.Recovered.ShouldBe(2);
        console.Shown.Count.ShouldBe(3, "once at start and once after each hand-off");
        log.Lines.ShouldContain("Handing the terminal to AttachToSession { JobId = 81cdb0f9 }");
    }

    public static TheoryData<Type> EffectsThatLeaveTheScreen => new(
        typeof(ViewEffect).Assembly.GetTypes().Where(type => !type.IsAbstract && type.IsAssignableTo(typeof(ILeavesScreen))));

    [Theory]
    [MemberData(nameof(EffectsThatLeaveTheScreen))]
    public async Task SwitchQuitOrUseTheTerminalForEveryEffectThatLeavesTheScreen(Type type)
    {
        ScriptedSessionsScreen sessions = new(state => new ScreenExit(state, Instantiate(type)), state => new ScreenExit(state, new Quit()));

        await Loop(sessions, new ScriptedWorktreesScreen(new SwitchToSessions())).RunAsync(processId: 100, CancellationToken.None);

        bool usedTheTerminalWhenHandedIt = console.Recovered == 0 || processes.InteractiveRequests.Count == 1;
        usedTheTerminalWhenHandedIt.ShouldBeTrue($"{type.Name} was handed the terminal, which did nothing with it");
    }

    public void Dispose()
    {
        terminal.Dispose();
    }

    /// <summary>An instance of an effect record whose fields are ids, such as a job or session id.</summary>
    private static ViewEffect Instantiate(Type type)
    {
        System.Reflection.ConstructorInfo constructor = type.GetConstructors().Single();
        object[] arguments = [.. constructor.GetParameters().Select(parameter => parameter.ParameterType switch
        {
            Type id when id == typeof(JobId) => JobId.From("81cdb0f9"),
            Type id when id == typeof(SessionId) => (object)SessionId.From("sess-1"),
            _ => throw new InvalidOperationException($"{type.Name} needs a {parameter.ParameterType.Name}; extend {nameof(Instantiate)}."),
        })];

        return (ViewEffect)constructor.Invoke(arguments);
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