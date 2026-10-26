using Source191.Vantage.Application;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class CommandLineShould
{
    [Fact]
    public void ParseSetupTerminalWithItsArguments()
    {
        CommandLine.Parse(["setup-terminal"]).ShouldBeOfType<SetupTerminal>().Arguments.ShouldBeEmpty();
        CommandLine.Parse(["setup-terminal", "--yes", "--sound", @"C:\Windows\Media\chimes.wav"]).ShouldBeOfType<SetupTerminal>().Arguments.ShouldBe(["--yes", "--sound", @"C:\Windows\Media\chimes.wav"]);
    }

    [Fact]
    public void ParseConsoleHereClaimTabAndUsage()
    {
        Guid registration = Guid.NewGuid();

        CommandLine.Parse([]).ShouldBe(new OpenConsole());
        CommandLine.Parse(["here"]).ShouldBeOfType<StartHere>().Arguments.ShouldBeEmpty();
        CommandLine.Parse(["here", "fix-tests", "-p", "Fix them"]).ShouldBeOfType<StartHere>().Arguments.ShouldBe(["fix-tests", "-p", "Fix them"]);
        CommandLine.Parse(["claim-tab", registration.ToString("N"), "4242"]).ShouldBe(new ClaimTab(registration, 4242));
        CommandLine.Parse(["claim-tab", "not-a-guid", "4242"]).ShouldBe(new Usage(Wording.ClaimTabUsage));
        CommandLine.Parse(["claim-tab", registration.ToString("N")]).ShouldBe(new Usage(Wording.ClaimTabUsage));
    }

    [Fact]
    public void OpenTheConsoleOnlyWithNoArguments()
    {
        CommandLine.Parse([]).ShouldBe(new OpenConsole());
        CommandLine.Parse(["anything-else"]).ShouldNotBe(new OpenConsole());
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    [InlineData("-?")]
    [InlineData("/?")]
    [InlineData("?")]
    [InlineData("help")]
    public void AskForTheGeneralHelp(string flag)
    {
        CommandLine.Parse([flag]).ShouldBe(new PrintHelp(HelpTopic.Vantage));
    }

    [Theory]
    [InlineData("here", "--help", HelpTopic.Here)]
    [InlineData("here", "-?", HelpTopic.Here)]
    [InlineData("setup-terminal", "-h", HelpTopic.SetupTerminal)]
    [InlineData("setup-terminal", "/?", HelpTopic.SetupTerminal)]
    public void AskForACommandsHelp(string command, string flag, HelpTopic topic)
    {
        CommandLine.Parse([command, flag]).ShouldBe(new PrintHelp(topic));
    }

    [Theory]
    [InlineData("help")]
    [InlineData("?")]
    public void KeepHelpAfterHereAsASessionName(string name)
    {
        CommandLine.Parse(["here", name]).ShouldBeOfType<StartHere>().Arguments.ShouldBe([name]);
    }

    [Theory]
    [InlineData("--version")]
    [InlineData("-v")]
    public void AskForTheVersion(string flag)
    {
        CommandLine.Parse([flag]).ShouldBe(new PrintVersion());
    }

    [Theory]
    [InlineData("frob")]
    [InlineData("--frob")]
    [InlineData("frob", "--yes")]
    public void RefuseAnUnknownCommandWithUsage(params string[] arguments)
    {
        CommandLine.Parse(arguments).ShouldBe(new Usage(Wording.UnknownCommand(arguments[0])));
    }
}