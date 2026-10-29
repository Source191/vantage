using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Adapters.Claude;
using Source191.Vantage.Adapters.Git;
using Source191.Vantage.Application;
using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Launch;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;
using static Source191.Vantage.Core.Tests.Application.Actions.SessionTestData;

namespace Source191.Vantage.Core.Tests.Application.Effects;

public sealed class EffectDispatcherShould
{
    private readonly FakeProcessRunner processes = new FakeProcessRunner().AnswerAll(new ProcessResult(0, string.Empty, string.Empty));
    private readonly FakeTimeProvider clock = new(Noon);
    private readonly DiagnosticLog log;

    public EffectDispatcherShould()
    {
        log = new DiagnosticLog(clock, []);
    }

    [Fact]
    public async Task RouteAnEffectToItsHandler()
    {
        EffectDispatcher dispatcher = new EffectDispatcher(log, clock).Register(new StopSessionHandler(new ClaudeCli(processes, Claude)));
        StopSession stop = new(Job(Repository));

        dispatcher.Handles(typeof(StopSession)).ShouldBeTrue();
        (await dispatcher.DispatchAsync(stop, CancellationToken.None)).ShouldBe(new StopSessionOutcome("fix-123"));
        processes.CapturedRequests.Single().Arguments.ShouldBe(["stop", "81cdb0f9"]);
    }

    [Fact]
    public async Task RecordEachEffectThatRuns()
    {
        EffectDispatcher dispatcher = new EffectDispatcher(log, clock).Register(new StopSessionHandler(new ClaudeCli(processes, Claude)));

        await dispatcher.DispatchAsync(new StopSession(Job(Repository)), CancellationToken.None);

        log.Snapshot().Select(timed => timed.Event).ShouldBe([new EffectRan(CodeName.For(typeof(StopSession)), CodeName.For(typeof(StopSessionOutcome)), TimeSpan.Zero)]);
        DiagnosticLine.Render(log.Snapshot().Single()).ShouldNotContain("81cdb0f9");
    }

    [Fact]
    public async Task RecordNothingWhenAHandlerFailsAndLetTheFailureThrough()
    {
        EffectDispatcher dispatcher = new EffectDispatcher(log, clock).Register(new FailingStop());

        await Should.ThrowAsync<GitException>(() => dispatcher.DispatchAsync(new StopSession(Job(Repository)), CancellationToken.None));

        log.Snapshot().ShouldBeEmpty();
    }

    [Fact]
    public async Task RefuseAnUnregisteredEffectByName()
    {
        EffectDispatcher dispatcher = new(log, clock);
        Quit quit = new();

        dispatcher.Handles(typeof(Quit)).ShouldBeFalse();
        InvalidOperationException refusal = await Should.ThrowAsync<InvalidOperationException>(() => dispatcher.DispatchAsync(quit, CancellationToken.None));
        refusal.Message.ShouldBe("No handler is registered for Quit.");
    }

    [Fact]
    public void HaveAHandlerForEveryEffectTheReducersCanProduce()
    {
        InMemoryState store = new();
        GitCli git = new(processes);
        ClaudeCli claude = new(processes, Claude);
        LaunchService launches = new(claude, git, store, store, new WorktreeIncludeCopier(git), clock);
        EffectPorts ports = new(claude, git, new FakeProcessRegistry(), new FakeTranscripts(), new FakeWindowsTerminal(), store, clock, new FakeClipboard(), new FakeDesktop(), new FakeEditorLocator(), new FakeFolders(), new FakeCrashFile());
        EffectDispatcher dispatcher = Composition.Handlers(ports, launches, new TabScripts(Claude, @"C:\Users\me\.dotnet\tools\vantage.exe"), TestSettings.Current(), new VantageProcess(4242, FolderPath.From(@"C:\Users\me")), log);
        // A dialog effect that is dispatched itself once its dialog has been answered.
        Type[] dispatchedAfterTheirDialog = [typeof(CloseEmptyTerminals)];

        IEnumerable<Type> background = typeof(ViewEffect).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && type.IsAssignableTo(typeof(ViewEffect)))
            .Where(type => !type.IsAssignableTo(typeof(ILeavesScreen)) && !type.IsAssignableTo(typeof(IDialogEffect)));

        background.Concat(dispatchedAfterTheirDialog)
            .Where(type => !dispatcher.Handles(type))
            .ShouldBeEmpty("every background effect needs a registration in Composition.Handlers");
    }

    private sealed class FailingStop : IEffectHandler<StopSession>
    {
        public Task<Outcome> HandleAsync(StopSession effect, CancellationToken cancellationToken)
        {
            throw new GitException(@"fatal: C:\Users\me\source\repos\Northwind.Orders");
        }
    }
}