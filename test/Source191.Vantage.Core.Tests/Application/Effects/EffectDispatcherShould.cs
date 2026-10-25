using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Adapters.Claude;
using Source191.Vantage.Adapters.Git;
using Source191.Vantage.Application;
using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Launch;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;
using static Source191.Vantage.Core.Tests.Application.Actions.SessionTestData;

namespace Source191.Vantage.Core.Tests.Application.Effects;

public sealed class EffectDispatcherShould
{
    private readonly FakeProcessRunner processes = new FakeProcessRunner().AnswerAll(new ProcessResult(0, string.Empty, string.Empty));

    [Fact]
    public async Task RouteAnEffectToItsHandler()
    {
        EffectDispatcher dispatcher = new EffectDispatcher().Register(new StopSessionHandler(new ClaudeCli(processes, Claude)));
        StopSession stop = new(Job(Repository));

        dispatcher.Handles(typeof(StopSession)).ShouldBeTrue();
        (await dispatcher.DispatchAsync(stop, CancellationToken.None)).ShouldBe(new StopSessionOutcome("fix-123"));
        processes.CapturedRequests.Single().Arguments.ShouldBe(["stop", "81cdb0f9"]);
    }

    [Fact]
    public async Task RefuseAnUnregisteredEffectByName()
    {
        EffectDispatcher dispatcher = new();
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
        FakeTimeProvider clock = new(Noon);
        LaunchService launches = new(claude, git, store, store, new WorktreeIncludeCopier(git), clock);
        EffectPorts ports = new(claude, git, new FakeProcessRegistry(), new FakeTranscripts(), new FakeWindowsTerminal(), store, clock);
        EffectDispatcher dispatcher = Composition.Handlers(ports, launches, new TabScripts(Claude, @"C:\Users\me\.dotnet\tools\vantage.exe"), TestSettings.Current());
        // A dialog effect that is dispatched itself once its dialog has been answered.
        Type[] dispatchedAfterTheirDialog = [typeof(CloseEmptyTerminals)];

        IEnumerable<Type> background = typeof(ViewEffect).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && type.IsAssignableTo(typeof(ViewEffect)))
            .Where(type => !type.IsAssignableTo(typeof(ILeavesScreen)) && !type.IsAssignableTo(typeof(IDialogEffect)));

        background.Concat(dispatchedAfterTheirDialog)
            .Where(type => !dispatcher.Handles(type))
            .ShouldBeEmpty("every background effect needs a registration in Composition.Handlers");
    }
}