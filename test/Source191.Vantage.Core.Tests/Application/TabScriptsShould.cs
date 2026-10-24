using Source191.Vantage.Application;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Core.Tests.Application;

/// <summary>Every tab Vantage opens: what it runs, where, under what title and in which colour.</summary>
public sealed class TabScriptsShould
{
    private const string Claude = @"C:\Users\me\.local\bin\claude.exe";
    private const string Vantage = @"C:\Users\me\.dotnet\tools\vantage.exe";

    private static readonly DateTimeOffset Noon = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);
    private static readonly FolderPath Repository = new(@"C:\repos\Northwind.Orders");

    private readonly TabScripts scripts = new(Claude, Vantage);

    [Fact]
    public void BuildTheAttachScript()
    {
        BackgroundSession session = new(new SessionId("sess-1"), new JobId("81cdb0f9"), "Refund retry policy", Repository, Noon, DisplayState.Working);
        TabRegistration registration = TabRegistration.Pending(new TabOwner.ForSession(session.SessionId), TabPalette.Default[0], Noon);

        TabLaunch tab = scripts.Attach(session, registration);

        tab.Title.ShouldBe("Refund retry policy");
        tab.StartingDirectory.ShouldBe(Repository);
        tab.Colour.ShouldBe(TabPalette.Default[0]);
        tab.Script.ShouldContain($"& '{Vantage}' 'claim-tab' '{registration.Id:N}'");
        tab.Script.ShouldContain("' $PID", customMessage: "the tab shell must pass its own process id, unquoted, so the registration outlives claim-tab");
        tab.Script.ShouldContain("Clear-Host", customMessage: "profile output must be cleared before the full-screen attach");
        tab.Script.ShouldContain($"& '{Claude}' 'attach' '81cdb0f9'");
    }

    [Fact]
    public void BuildTheTrustScript()
    {
        TabLaunch tab = scripts.Trust(Repository);

        tab.Title.ShouldBe("Trust Northwind.Orders");
        tab.StartingDirectory.ShouldBe(Repository);
        tab.Colour.ShouldBe(TabPalette.Default[2]);
        tab.Script.ShouldContain($"& '{Claude}'");
        tab.Script.ShouldContain("accept the trust prompt");
    }

    [Fact]
    public void BuildTheShellScript()
    {
        FolderPath worktree = new(@"C:\repos\Northwind.Orders-fix-123");

        TabLaunch tab = TabScripts.Shell(worktree);

        tab.ShouldBe(new TabLaunch("Northwind.Orders-fix-123", TabPalette.Default[^1], worktree, "Clear-Host"));
    }

    [Fact]
    public void QuoteUserTextSafely()
    {
        TabLaunch tab = scripts.Trust(new FolderPath(@"C:\repos\Bob's tools"));

        tab.Script.Split('\n')[0].ShouldBe(
            "Write-Host 'Vantage: accept the trust prompt for Bob''s tools, then exit Claude and press n in Vantage to retry.' -ForegroundColor Cyan");
    }
}