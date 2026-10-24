using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;
using static Source191.Vantage.Core.Tests.Application.Actions.WorktreeTestData;

namespace Source191.Vantage.Core.Tests.Application.Actions;

public sealed class OpenShellHandlerShould
{
    [Fact]
    public async Task OpenAPlainShellTabInTheWorktree()
    {
        FakeWindowsTerminal terminal = new();
        OpenShellHandler handler = new(terminal);

        string message = await handler.HandleAsync(new OpenShellTab(Row(WorktreeHealthKind.Ok)), CancellationToken.None);

        TabLaunch tab = terminal.Opened.Single();
        tab.Title.ShouldBe("Northwind.Orders-fix-123");
        tab.StartingDirectory.ShouldBe(Worktree);
        message.ShouldBe("Opened a shell in Northwind.Orders-fix-123.");
    }
}