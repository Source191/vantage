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
    private static readonly FolderPath Repository = FolderPath.From(@"C:\repos\Northwind.Orders");

    private readonly TabScripts scripts = new(Claude, Vantage);

    [Fact]
    public void BuildTheAttachScript()
    {
        BackgroundSession session = new(SessionId.From("sess-1"), JobId.From("81cdb0f9"), "Refund retry policy", Repository, Noon, DisplayState.Working);
        TabRegistration registration = TabRegistration.Pending(new TabOwner.ForSession(session.SessionId), TabPalette.Default[0], Noon);

        TabLaunch tab = scripts.Attach(session, registration);

        tab.Title.ShouldBe("Refund retry policy");
        tab.StartingDirectory.ShouldBe(Repository);
        tab.Colour.ShouldBe(TabPalette.Default[0]);
        tab.Script.ShouldContain($"& '{Vantage}' 'claim-tab' '{registration.Id:N}'");
        tab.Script.ShouldContain("' $PID", customMessage: "the tab shell must pass its own process id, unquoted, so the registration outlives claim-tab");
        tab.Script.ShouldContain("Clear-Host", customMessage: "profile output must be cleared before the full-screen attach");
        tab.Script.ShouldContain($"& '{Claude}' 'attach' '81cdb0f9'");
        tab.Script.Split('\n')[^1].ShouldBe("if ($LASTEXITCODE -eq 0) { exit }", customMessage: "a detach, a stop or a removal ends the attach with 0 and the tab closes; a failed attach keeps its prompt");
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
        FolderPath worktree = FolderPath.From(@"C:\repos\Northwind.Orders-fix-123");

        TabLaunch tab = TabScripts.Shell(worktree);

        tab.ShouldBe(new TabLaunch("Northwind.Orders-fix-123", TabPalette.Default[^1], worktree, "Clear-Host"));
    }

    [Fact]
    public void WaitUpdateAndRestartInTheUpdateTab()
    {
        FolderPath home = FolderPath.From(@"C:\Users\me");

        TabLaunch tab = scripts.Update(4242, home);

        tab.Title.ShouldBe("Update Vantage");
        tab.StartingDirectory.ShouldBe(home);
        string[] lines = tab.Script.Split('\n');
        lines[0].ShouldBe("Wait-Process -Id '4242' -ErrorAction SilentlyContinue");
        lines[1].ShouldBe("& 'dotnet' 'tool' 'update' '-g' 'Source191.Vantage'");
        lines[2].ShouldBe($"if ($LASTEXITCODE -eq 0) {{ & '{Vantage}' }} else {{ Write-Host {PowerShellScript.Quote(Wording.UpdateFailedHint)} -ForegroundColor Yellow }}");
    }

    [Fact]
    public void QuoteTheVantagePathInTheUpdateTab()
    {
        TabLaunch tab = new TabScripts(Claude, @"C:\Users\O'Brien\.dotnet\tools\vantage.exe").Update(4242, FolderPath.From(@"C:\Users\O'Brien"));

        tab.Script.ShouldContain(@"& 'C:\Users\O''Brien\.dotnet\tools\vantage.exe'");
    }

    [Fact]
    public void QuoteUserTextSafely()
    {
        TabLaunch tab = scripts.Trust(FolderPath.From(@"C:\repos\Bob's tools"));

        tab.Script.Split('\n')[0].ShouldBe(
            "Write-Host 'Vantage: accept the trust prompt for Bob''s tools, then exit Claude and press n in Vantage to retry.' -ForegroundColor Cyan");
    }
}