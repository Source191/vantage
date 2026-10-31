namespace Source191.Vantage.HeadlessTerminal;

/// <summary>
/// Reads a hand-off capture of <c>vantage here</c>: whether Claude's own screen, its <c>Claude Code v</c> banner, was drawn.
/// <c>here</c> has no screen of its own and prints nothing holding that text before it hands over, so the banner is
/// looked for anywhere in the capture with the escape sequences stripped.
/// </summary>
public static class HandoffCapture
{
    private const string ClaudeBanner = "Claude Code v";

    public static bool ShowsClaudeScreen(string capture) => MarkerCount.Of(EscapeSequences.Strip(capture), ClaudeBanner) > 0;
}