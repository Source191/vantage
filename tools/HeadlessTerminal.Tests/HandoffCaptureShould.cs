namespace Source191.Vantage.HeadlessTerminal.Tests;

public sealed class HandoffCaptureShould
{
    private const string Here = "Started fix-tests (81cdb0f9) in C:\\Users\\me\\source\\repos\\Northwind.Orders.\r\nCtrl+Z detaches; the session keeps running.\r\n";
    private const string Enter = "\u001b[?1049h";
    private const string Leave = "\u001b[?1049l";
    private const string Clears = "\u001b[H\u001b[K\r\n\u001b[K\r\n\u001b[H\u001b[?25h";

    [Fact]
    public void AcceptClaudesScreenDrawnAfterVantageHereHandsOver()
    {
        string capture = Here + Enter + "\u001b[1m Claude Code v2.1.310" + Leave + Clears;

        HandoffCapture.ShowsClaudeScreen(capture).ShouldBeTrue();
    }

    [Fact]
    public void AcceptTheBannerWithStylingBetweenItsWords()
    {
        string capture = Here + Enter + "\u001b[1mClaude\u001b[0m \u001b[2mCode v\u001b[0m2.1.310" + Leave;

        HandoffCapture.ShowsClaudeScreen(capture).ShouldBeTrue();
    }

    [Fact]
    public void AcceptAHandOffThatNeverReturned()
    {
        string capture = Here + Enter + "Claude Code v2.1.310";

        HandoffCapture.ShowsClaudeScreen(capture).ShouldBeTrue();
    }

    [Fact]
    public void RefuseAHandOffThatOnlyClearedTheScreen()
    {
        string capture = Here + Enter + Clears + Leave;

        HandoffCapture.ShowsClaudeScreen(capture).ShouldBeFalse();
    }

    [Fact]
    public void RefuseAStartThatFailedBeforeTheAttach()
    {
        string capture = "Workspace not trusted. Run claude in C:\\Users\\me\\source\\repos\\Northwind.Orders once and accept the trust prompt.\r\n";

        HandoffCapture.ShowsClaudeScreen(capture).ShouldBeFalse();
    }
}