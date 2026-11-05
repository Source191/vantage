using Source191.Torni.Adapters.Terminal;

namespace Source191.Torni.HeadlessTerminal.Tests;

public sealed class EscapeSequencesShould
{
    [Fact]
    public void StripTheConsolesTabReset()
    {
        EscapeSequences.Strip(TabEscapes.ResetBackground).ShouldBe(string.Empty);
    }

    [Fact]
    public void KeepTheTextAroundTheConsolesTabReset()
    {
        EscapeSequences.Strip("before" + TabEscapes.ResetBackground + "after").ShouldBe("beforeafter");
    }

    [Theory]
    [InlineData("\x1b[?1049h", "")]
    [InlineData("\x1b[<u", "")]
    [InlineData("\x1b[>4;2m", "")]
    [InlineData("\x1b[=5u", "")]
    [InlineData("\x1b[1;1H", "")]
    [InlineData("\x1b[2 q", "")]
    public void StripCsiWithPrivateAndIntermediateBytes(string raw, string plain)
    {
        EscapeSequences.Strip("a" + raw + "b").ShouldBe("a" + plain + "b");
    }

    [Theory]
    [InlineData("\x1b]2;title\x07")]
    [InlineData("\x1b]2;title\x1b\\")]
    [InlineData("\x1bP>|Windows Terminal 1.25\x1b\\")]
    public void StripOscAndDcsToBelOrStringTerminator(string raw)
    {
        EscapeSequences.Strip("a" + raw + "b").ShouldBe("ab");
    }

    [Fact]
    public void LeaveAnUnterminatedSequenceToTheEnd()
    {
        EscapeSequences.Strip("a\x1b]2;never ended").ShouldBe("a");
        const string Capture = "x\x1b]2;never ended";
        EscapeSequences.EndOf(Capture, 1).ShouldBe(Capture.Length);
    }

    [Fact]
    public void TakeAnyOtherEscapeAsTwoCharacters()
    {
        EscapeSequences.EndOf("\x1b7text", 0).ShouldBe(2);
    }
}