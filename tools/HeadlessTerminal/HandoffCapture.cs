namespace Source191.Vantage.HeadlessTerminal;

/// <summary>
/// Reads a hand-off capture: whether Claude's own screen was drawn while Vantage had left the alternate screen. Claude
/// draws on an alternate screen of its own, so the hand-off runs from Vantage's first exit to the last entry after it
/// (Vantage's return), not to the next entry, which is Claude's.
/// </summary>
public static class HandoffCapture
{
    private const string LeaveAlternateScreen = "\u001b[?1049l";
    private const string EnterAlternateScreen = "\u001b[?1049h";
    private const string ClaudeBanner = "Claude Code v";

    public static bool ShowsClaudeScreen(string capture)
    {
        int left = capture.IndexOf(LeaveAlternateScreen, StringComparison.Ordinal);
        if (left < 0)
        {
            return false;
        }

        int returned = capture.LastIndexOf(EnterAlternateScreen, StringComparison.Ordinal);
        string handOff = returned < left ? capture[left..] : capture[left..returned];

        return MarkerCount.Of(EscapeSequences.Strip(handOff), ClaudeBanner) > 0;
    }
}