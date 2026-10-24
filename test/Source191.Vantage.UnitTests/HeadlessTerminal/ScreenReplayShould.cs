using Source191.Vantage.HeadlessTerminal;

namespace Source191.Vantage.UnitTests.HeadlessTerminal;

public sealed class ScreenReplayShould
{
    [Fact]
    public void PlaceTextWhereTheCursorSequencesSayAndSkipColoursAndTitles()
    {
        string capture = "\x1b[2J\x1b[2;3HHello\x1b[31m world\x1b[0m\x1b]0;title\x07\x1b[4;1Hrow four\x1b[2X";

        ScreenReplay replay = new ScreenReplay(20, 5).Replay(capture, capture.Length);

        replay.Lines.ShouldBe(["  Hello world", "row four"]);
        replay.FilledCells.ShouldBe(17, "ten letters in Hello world and seven in row four");
    }

    [Fact]
    public void WrapAtTheWidthAndEraseToTheEndOfLine()
    {
        string capture = "\x1b[1;1Habcdefgh\x1b[1;3H\x1b[K";

        ScreenReplay replay = new ScreenReplay(5, 3).Replay(capture, capture.Length);

        replay.Lines.ShouldBe(["ab", "fgh"]);
    }

    [Fact]
    public void StopAfterTheLastMarkerOrAtTheAlternateScreenExitThatFollowsIt()
    {
        string capture = "first marker second marker \x1b[?1049l teardown";

        ScreenReplay.EndFor(capture, "marker").ShouldBe(capture.IndexOf("\x1b[?1049l", StringComparison.Ordinal));
        ScreenReplay.EndFor("no marker here", "absent").ShouldBe(14);
    }
}