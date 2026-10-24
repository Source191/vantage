using Source191.Vantage.HeadlessTerminal;

namespace Source191.Vantage.UnitTests.HeadlessTerminal;

public sealed class MarkerCountShould
{
    // \u escapes are fixed length; \x07C would be read as one character.
    private const string Capture = "\u001b[2J\u001b[1;1H\u001b]0;title\u0007Cancelled; nothing removed.\u001b[31mCancelled; nothing removed.\u001b[0m\u001bP>|wt\u001b\\";

    [Fact]
    public void StripControlSequencesBeforeCounting()
    {
        string plain = EscapeSequences.Strip(Capture);

        plain.ShouldBe("Cancelled; nothing removed.Cancelled; nothing removed.");
        MarkerCount.Of(plain, "Cancelled; nothing removed.").ShouldBe(2);
        MarkerCount.Of(plain, "Removing").ShouldBe(0);
    }

    [Fact]
    public void ShowTheTextAroundTheFirstMarker()
    {
        MarkerCount.Around("abc MARKER def", "MARKER", before: 2, after: 8).ShouldBe("c MARKER d");
        MarkerCount.Around("abc", "MARKER").ShouldBeEmpty();
    }
}