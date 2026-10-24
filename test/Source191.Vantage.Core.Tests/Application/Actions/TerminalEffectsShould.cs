using Source191.Vantage.Adapters.Claude;
using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application.Actions;

public sealed class TerminalEffectsShould : IDisposable
{
    private readonly FakeProcessRunner processes = new FakeProcessRunner().AnswerAll(new ProcessResult(0, string.Empty, string.Empty));
    private readonly StringWriter console = new();

    public void Dispose()
    {
        console.Dispose();
    }

    private TerminalEffects Effects => new(new ClaudeCli(processes, "claude.exe"), console);

    [Fact]
    public async Task PrintTheReturnHintThenAttach()
    {
        await Effects.RunAsync(new AttachToSession(new JobId("81cdb0f9")), CancellationToken.None);

        console.ToString().ShouldContain("Ctrl+Z returns to Vantage");
        console.ToString().ShouldContain("agent list");
        console.ToString().ShouldContain("Esc");
        processes.InteractiveRequests.Single().Arguments.ShouldBe(["attach", "81cdb0f9"]);
    }

    [Fact]
    public async Task OpenTheNativeAgentView()
    {
        await Effects.RunAsync(new OpenAgentView(), CancellationToken.None);

        processes.InteractiveRequests.Single().Arguments.ShouldBe(["agents"]);
    }

    [Fact]
    public async Task IgnoreEffectsThatDoNotNeedTheTerminal()
    {
        await Effects.RunAsync(new Quit(), CancellationToken.None);

        processes.InteractiveRequests.ShouldBeEmpty();
        console.ToString().ShouldBeEmpty();
    }
}