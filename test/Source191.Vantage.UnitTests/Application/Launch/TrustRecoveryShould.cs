using Source191.Vantage.Adapters.Terminal;
using Source191.Vantage.Application.Launch;

namespace Source191.Vantage.UnitTests.Application.Launch;

public sealed class TrustRecoveryShould
{
    [Fact]
    public void OpenAPlainClaudeTabInTheUntrustedDirectorySoTheTrustPromptCanBeAccepted()
    {
        TabLaunch tab = TrustRecovery.TabFor(@"C:\repos\Northwind.Orders", @"C:\Users\me\.local\bin\claude.exe");

        tab.Title.ShouldBe("Trust Northwind.Orders");
        tab.StartingDirectory.ShouldBe(@"C:\repos\Northwind.Orders");
        tab.Script.ShouldContain("& 'C:\\Users\\me\\.local\\bin\\claude.exe'");
        tab.Script.ShouldContain("accept the trust prompt");
    }

    [Fact]
    public void ExplainWhatToDoInTheFooter()
    {
        TrustRecovery.Explanation(@"C:\repos\Northwind.Orders").ShouldContain("Northwind.Orders");
        TrustRecovery.Explanation(@"C:\repos\Northwind.Orders").ShouldContain("retry");
    }
}