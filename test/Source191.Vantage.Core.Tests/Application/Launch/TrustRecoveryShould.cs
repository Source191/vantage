using Source191.Vantage.Application.Launch;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Core.Tests.Application.Launch;

public sealed class TrustRecoveryShould
{
    [Fact]
    public void OpenAPlainClaudeTabInTheUntrustedDirectorySoTheTrustPromptCanBeAccepted()
    {
        TabLaunch tab = TrustRecovery.TabFor(new FolderPath(@"C:\repos\Northwind.Orders"), @"C:\Users\me\.local\bin\claude.exe");

        tab.Title.ShouldBe("Trust Northwind.Orders");
        tab.StartingDirectory.Value.ShouldBe(@"C:\repos\Northwind.Orders");
        tab.Script.ShouldContain("& 'C:\\Users\\me\\.local\\bin\\claude.exe'");
        tab.Script.ShouldContain("accept the trust prompt");
    }

    [Fact]
    public void KeepAFolderNameWithAnApostropheInsideTheMessage()
    {
        TabLaunch tab = TrustRecovery.TabFor(new FolderPath(@"C:\repos\Bob's tools"), @"C:\Users\me\.local\bin\claude.exe");

        tab.Script.Split('\n')[0].ShouldBe(
            "Write-Host 'Vantage: accept the trust prompt for Bob''s tools, then exit Claude and press n in Vantage to retry.' -ForegroundColor Cyan");
    }
}