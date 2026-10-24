using Source191.Vantage.Application;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class CommandLineShould
{
    [Fact]
    public void ParseConsoleHereClaimTabAndUsage()
    {
        Guid registration = Guid.NewGuid();

        CommandLine.Parse([]).ShouldBe(new OpenConsole());
        CommandLine.Parse(["here"]).ShouldBeOfType<StartHere>().Arguments.ShouldBeEmpty();
        CommandLine.Parse(["here", "fix-tests", "-p", "Fix them"]).ShouldBeOfType<StartHere>().Arguments.ShouldBe(["fix-tests", "-p", "Fix them"]);
        CommandLine.Parse(["claim-tab", registration.ToString("N"), "4242"]).ShouldBe(new ClaimTab(registration, 4242));
        CommandLine.Parse(["claim-tab", "not-a-guid", "4242"]).ShouldBe(new Usage(CommandLine.ClaimTabUsage));
        CommandLine.Parse(["claim-tab", registration.ToString("N")]).ShouldBe(new Usage(CommandLine.ClaimTabUsage));
        CommandLine.Parse(["anything-else"]).ShouldBe(new OpenConsole());
    }
}