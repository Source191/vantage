using Source191.Vantage.Application;
using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application.Actions;

public sealed class StartUpdateHandlerShould
{
    private const string Vantage = @"C:\Users\me\.dotnet\tools\vantage.exe";
    private static readonly FolderPath Home = FolderPath.From(@"C:\Users\me");

    [Fact]
    public async Task OpenTheUpdateTabAndReportIt()
    {
        FakeWindowsTerminal terminal = new();
        StartUpdateHandler handler = new(terminal, new TabScripts(@"C:\Users\me\.local\bin\claude.exe", Vantage), 4242, Home);

        Outcome outcome = await handler.HandleAsync(new StartUpdate(ReleaseVersions.Version("0.10.0")), CancellationToken.None);

        TabLaunch tab = terminal.Opened.Single();
        tab.Title.ShouldBe("Update Vantage");
        tab.StartingDirectory.ShouldBe(Home);
        tab.Script.ShouldContain("Wait-Process -Id '4242'");
        outcome.ShouldBe(new UpdateStartedOutcome("0.10.0"));
    }
}