using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Adapters.Claude;
using Source191.Vantage.Application;
using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.UnitTests.TestDoubles;

namespace Source191.Vantage.UnitTests.Application;

public sealed class ScreenLoopShould : IDisposable
{
    private static readonly DateTimeOffset Noon = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);
    private static readonly SessionRow First = Row("sess-1", "change-feed");
    private static readonly SessionRow Second = Row("sess-2", "fix-tests");

    private readonly FakeProcessRunner processes = new FakeProcessRunner().AnswerAll(new ProcessResult(0, string.Empty, string.Empty));
    private readonly FakeProcessRegistry registry = new FakeProcessRegistry().Running(100, Noon);
    private readonly FakeTerminalConsole console = new();
    private readonly RecordingLogger log = new();
    private readonly StringWriter terminal = new();

    private ScreenLoop Loop(ScriptedSessionsScreen sessions, ScriptedWorktreesScreen worktrees)
    {
        OwnTab ownTab = new(new InMemoryState(), registry, new FakeTimeProvider(Noon));
        TerminalEffects effects = new(new ClaudeCli(processes, "claude.exe"), terminal);

        return new ScreenLoop(sessions, worktrees, ownTab, console, effects, log);
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
        ScriptedWorktreesScreen worktrees = new(new JumpToSession("sess-2"), new SwitchToSessions());

        await Loop(sessions, worktrees).RunAsync(processId: 100, CancellationToken.None);

        worktrees.Runs.ShouldBe(2);
        sessions.Given[1].Selected.ShouldBe(Second);
        sessions.Given[2].Selected.ShouldBe(Second, "the selection survives the second trip");
    }

    [Fact]
    public async Task RunTerminalEffectsBetweenScreensAndRelabelTheTabAfterwards()
    {
        ScriptedSessionsScreen sessions = new(
            state => new ScreenExit(state, new AttachToSession("81cdb0f9")),
            state => new ScreenExit(state, new OpenAgentView()),
            state => new ScreenExit(state, new Quit()));

        await Loop(sessions, new ScriptedWorktreesScreen()).RunAsync(processId: 100, CancellationToken.None);

        processes.InteractiveRequests.Select(request => request.Arguments[0]).ShouldBe(["attach", "agents"]);
        console.Recovered.ShouldBe(2);
        console.Shown.Count.ShouldBe(3, "once at start and once after each hand-off");
        log.Lines.ShouldContain("Handing the terminal to AttachToSession { JobId = 81cdb0f9 }");
    }

    public void Dispose()
    {
        terminal.Dispose();
    }

    private static SessionRow Row(string sessionId, string name)
    {
        Session session = new(sessionId, sessionId[..4] + "0000", name, @"C:\Users\me\source\repos\Northwind.Orders", SessionKind.Background, Noon, DisplayState.Idle);

        return new SessionRow(session, "Northwind.Orders", "main", IsWorktree: false, Description: null, HasLiveTab: false, HasTranscript: true);
    }
}