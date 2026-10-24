using Source191.Vantage.Adapters.Claude;
using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Effects;
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
        StopSession stop = new(Background(Repository, isWorktree: false));

        dispatcher.Handles(stop).ShouldBeTrue();
        (await dispatcher.DispatchAsync(stop, CancellationToken.None)).ShouldBe("Stopped fix-123.");
        processes.CapturedRequests.Single().Arguments.ShouldBe(["stop", "81cdb0f9"]);
    }

    [Fact]
    public async Task RefuseAnUnregisteredEffectByName()
    {
        EffectDispatcher dispatcher = new();
        Quit quit = new();

        dispatcher.Handles(quit).ShouldBeFalse();
        InvalidOperationException refusal = await Should.ThrowAsync<InvalidOperationException>(() => dispatcher.DispatchAsync(quit, CancellationToken.None));
        refusal.Message.ShouldBe("No handler is registered for Quit.");
    }
}