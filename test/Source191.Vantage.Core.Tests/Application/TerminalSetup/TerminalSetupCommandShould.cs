using Source191.Vantage.Application.TerminalSetup;

namespace Source191.Vantage.Core.Tests.Application.TerminalSetup;

public sealed class TerminalSetupCommandShould
{
    [Fact]
    public void ParseNoArgumentsAsDefaultsWithConfirmation()
    {
        TerminalSetupParse parsed = TerminalSetupCommand.Parse([]);

        parsed.Request.ShouldBe(new TerminalSetupRequest(Sound: null, SessionSound: null, Yes: false, Remove: false));
        parsed.Problem.ShouldBeNull();
    }

    [Fact]
    public void ParseSoundSessionSoundAndYes()
    {
        TerminalSetupParse parsed = TerminalSetupCommand.Parse(["--sound", @"C:\Windows\Media\chimes.wav", "--yes", "--session-sound", @"C:\Windows\Media\Ring01.wav"]);

        parsed.Request.ShouldBe(new TerminalSetupRequest(@"C:\Windows\Media\chimes.wav", @"C:\Windows\Media\Ring01.wav", Yes: true, Remove: false));
    }

    [Fact]
    public void ParseRemove()
    {
        TerminalSetupCommand.Parse(["--remove"]).Request.ShouldBe(new TerminalSetupRequest(null, null, Yes: false, Remove: true));
        TerminalSetupCommand.Parse(["--remove", "--yes"]).Request.ShouldBe(new TerminalSetupRequest(null, null, Yes: true, Remove: true));
    }

    [Fact]
    public void RefuseAnUnknownOption()
    {
        TerminalSetupCommand.Parse(["--loud"]).Problem.ShouldBe(new TerminalSetupProblem.UnknownOption("--loud"));
    }

    [Fact]
    public void RefuseAnOptionWithoutAValue()
    {
        TerminalSetupCommand.Parse(["--sound"]).Problem.ShouldBe(new TerminalSetupProblem.OptionNeedsValue("--sound"));
        TerminalSetupCommand.Parse(["--session-sound"]).Problem.ShouldBe(new TerminalSetupProblem.OptionNeedsValue("--session-sound"));
    }

    [Fact]
    public void RefuseAnArgumentThatIsNotAnOption()
    {
        TerminalSetupCommand.Parse(["chimes.wav"]).Problem.ShouldBe(new TerminalSetupProblem.UnexpectedArgument("chimes.wav"));
    }

    [Fact]
    public void RefuseRemoveWithASound()
    {
        TerminalSetupCommand.Parse(["--remove", "--sound", @"C:\a.wav"]).Problem.ShouldBe(new TerminalSetupProblem.RemoveWithSound());
        TerminalSetupCommand.Parse(["--session-sound", @"C:\a.wav", "--remove"]).Problem.ShouldBe(new TerminalSetupProblem.RemoveWithSound());
    }
}