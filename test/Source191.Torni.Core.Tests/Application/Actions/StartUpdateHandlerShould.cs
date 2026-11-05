using Source191.Torni.Application;
using Source191.Torni.Application.Actions;
using Source191.Torni.Application.Effects;
using Source191.Torni.Application.Views;
using Source191.Torni.Domain;
using Source191.Torni.Ports;
using Source191.Torni.TestDoubles;

namespace Source191.Torni.Core.Tests.Application.Actions;

public sealed class StartUpdateHandlerShould
{
    private const string Torni = @"C:\Users\me\AppData\Local\Microsoft\WinGet\Links\torni.exe";
    private const string Winget = @"C:\Users\me\AppData\Local\Microsoft\WindowsApps\winget.exe";
    private static readonly FolderPath Home = FolderPath.From(@"C:\Users\me");

    private readonly FakeWindowsTerminal terminal = new();

    [Fact]
    public async Task OpenTheUpdateTabAndReportIt()
    {
        StartUpdateHandler handler = Handler(new ProgramLocation.Found(Winget));

        Outcome outcome = await handler.HandleAsync(new StartUpdate(ReleaseVersions.Version("0.10.0")), CancellationToken.None);

        TabLaunch tab = terminal.Opened.Single();
        tab.Title.ShouldBe("Update Torni");
        tab.StartingDirectory.ShouldBe(Home);
        tab.Script.ShouldContain("Wait-Process -Id '4242'");
        tab.Script.ShouldContain($"& '{Winget}' 'upgrade'");
        outcome.ShouldBe(new UpdateStartedOutcome("0.10.0"));
    }

    [Fact]
    public async Task OpenNoTabWithoutWinget()
    {
        StartUpdateHandler handler = Handler(new ProgramLocation.Missing());

        Outcome outcome = await handler.HandleAsync(new StartUpdate(ReleaseVersions.Version("0.21.0")), CancellationToken.None);

        terminal.Opened.ShouldBeEmpty();
        outcome.ShouldBe(new WingetMissingOutcome("0.21.0"));
    }

    private StartUpdateHandler Handler(ProgramLocation winget)
    {
        return new StartUpdateHandler(terminal, new TabScripts(@"C:\Users\me\.local\bin\claude.exe", Torni), new OwnProcess(4242, Home, winget));
    }
}