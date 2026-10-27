namespace Source191.Vantage.HeadlessTerminal.Tests;

public sealed class HandoffCaptureShould
{
    private const string Leave = "\u001b[?1049l";
    private const string Enter = "\u001b[?1049h";
    private const string Clears = "\u001b[H\u001b[K\r\n\u001b[K\r\n\u001b[H\u001b[?25h";

    [Fact]
    public void AcceptAClaudeScreenBetweenLeavingAndReturning()
    {
        string capture = "Vantage frames" + Leave + "\u001b[2J  Attaching… \u001b[1m Claude Code v2.1.310" + Enter + "Vantage again";

        HandoffCapture.ShowsClaudeScreen(capture).ShouldBeTrue();
    }

    [Fact]
    public void AcceptAClaudeScreenDrawnOnClaudesOwnAlternateScreen()
    {
        string capture = "Vantage frames" + Leave + "Ctrl+Z returns to Vantage." + Enter + "\u001b[1m Claude Code v2.1.310" + Leave + Clears + Enter + "Vantage again";

        HandoffCapture.ShowsClaudeScreen(capture).ShouldBeTrue();
    }

    [Fact]
    public void RefuseAHandOffThatOnlyClearedTheScreen()
    {
        string capture = "Vantage frames" + Leave + Clears + Enter + "Vantage again";

        HandoffCapture.ShowsClaudeScreen(capture).ShouldBeFalse();
    }

    [Fact]
    public void RefuseACaptureWithNoHandOff()
    {
        HandoffCapture.ShowsClaudeScreen("Vantage frames, Claude Code v2.1.310 in the header").ShouldBeFalse();
    }

    [Fact]
    public void IgnoreTheBannerOutsideTheHandOff()
    {
        string capture = "Claude Code v2.1.310" + Leave + Clears + Enter + "Claude Code v2.1.310";

        HandoffCapture.ShowsClaudeScreen(capture).ShouldBeFalse();
    }

    [Fact]
    public void AcceptAHandOffThatNeverReturned()
    {
        string capture = "Vantage frames" + Leave + "Claude Code v2.1.310";

        HandoffCapture.ShowsClaudeScreen(capture).ShouldBeTrue();
    }
}