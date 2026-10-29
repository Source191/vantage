using Source191.Vantage.Adapters.Claude;
using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application.Actions;

public sealed class TerminalEffectsShould
{
    private readonly FakeProcessRunner processes = new FakeProcessRunner().AnswerAll(new ProcessResult(0, string.Empty, string.Empty));
    private readonly FakeTerminalConsole console = new();

    private TerminalEffects Effects => new(new ClaudeCli(processes, "claude.exe"), console);

    [Fact]
    public async Task ShowTheHintBeforeTheAttachAndRemoveItAfter()
    {
        processes.OnInteractive = request => console.Events.Add(string.Join(' ', request.Arguments));

        await Effects.RunAsync(new AttachToSession(JobId.From("81cdb0f9")), CancellationToken.None);

        console.Events.ShouldBe(["return hint shown", "attach 81cdb0f9", "return hint removed"]);
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
        console.Events.ShouldBeEmpty();
    }
}