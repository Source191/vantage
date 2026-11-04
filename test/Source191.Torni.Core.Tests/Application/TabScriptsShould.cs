using Source191.Torni.Application;
using Source191.Torni.Domain;
using Source191.Torni.Ports;
using Source191.Torni.TestDoubles;

namespace Source191.Torni.Core.Tests.Application;

/// <summary>Every tab Torni opens: what it runs, where, under what title and in which colour.</summary>
public sealed class TabScriptsShould
{
    private const string Claude = @"C:\Users\me\.local\bin\claude.exe";
    private const string Torni = @"C:\Users\me\.dotnet\tools\torni.exe";
    private const string Winget = @"C:\Users\me\AppData\Local\Microsoft\WindowsApps\winget.exe";

    private static readonly FolderPath Repository = FolderPath.From(@"C:\repos\Northwind.Orders");

    private readonly TabScripts scripts = new(Claude, Torni);

    [Fact]
    public void BuildTheAttachScript()
    {
        Session.Background session = new(SessionId.From("sess-1"), JobId.From("81cdb0f9"), Listed.As("Refund retry policy"), Repository, Fixtures.Noon, DisplayState.Working, new JobProcess.None());
        TabRegistration registration = TabRegistration.Pending(new TabOwner.ForSession(session.SessionId), TabPalette.Default[0], Fixtures.Noon);

        TabLaunch tab = scripts.Attach(session, registration);

        tab.Title.ShouldBe("Refund retry policy");
        tab.StartingDirectory.ShouldBe(Repository);
        tab.Colour.ShouldBe(TabPalette.Default[0]);
        tab.Script.ShouldContain($"& '{Torni}' 'claim-tab' '{registration.Id:N}'");
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
        tab.Colour.ShouldBe(TabPalette.Green);
        tab.Script.ShouldContain($"& '{Claude}'");
        tab.Script.ShouldContain("accept the trust prompt");
    }

    [Fact]
    public void BuildTheShellScript()
    {
        FolderPath worktree = FolderPath.From(@"C:\repos\Northwind.Orders-fix-123");

        TabLaunch tab = TabScripts.Shell(worktree);

        tab.ShouldBe(new TabLaunch("Northwind.Orders-fix-123", TabPalette.Last, worktree, "Clear-Host"));
    }

    [Fact]
    public void WaitUpgradeWithWingetAndRestartInTheUpdateTab()
    {
        FolderPath home = FolderPath.From(@"C:\Users\me");

        TabLaunch tab = scripts.Update(4242, home, Winget);

        tab.Title.ShouldBe("Update Torni");
        tab.StartingDirectory.ShouldBe(home);
        string[] lines = tab.Script.Split('\n');
        lines[0].ShouldBe("Wait-Process -Id '4242' -ErrorAction SilentlyContinue");
        lines[1].ShouldBe($"& '{Winget}' 'upgrade' '--id' 'Source191.Torni' '--exact' '--source' 'winget'", customMessage: "the upgrade must come from the source the check consulted, whatever other sources the machine has");
        lines[2].ShouldBe($"if ($LASTEXITCODE -eq 0) {{ & '{Torni}' }} else {{ Write-Host {PowerShellScript.Quote(Wording.UpdateFailedHint)} -ForegroundColor Yellow }}");
        lines.Length.ShouldBe(3);
    }

    [Fact]
    public void QuoteTheTorniPathInTheUpdateTab()
    {
        TabLaunch tab = new TabScripts(Claude, @"C:\Users\O'Brien\AppData\Local\Microsoft\WinGet\Links\torni.exe").Update(4242, FolderPath.From(@"C:\Users\O'Brien"), Winget);

        tab.Script.ShouldContain(@"& 'C:\Users\O''Brien\AppData\Local\Microsoft\WinGet\Links\torni.exe'");
    }
    [Fact]
    public void QuoteUserTextSafely()
    {
        TabLaunch tab = scripts.Trust(FolderPath.From(@"C:\repos\Bob's tools"));

        tab.Script.Split('\n')[0].ShouldBe(
            "Write-Host 'Torni: accept the trust prompt for Bob''s tools, then exit Claude and press n in Torni to retry.' -ForegroundColor Cyan");
    }
}