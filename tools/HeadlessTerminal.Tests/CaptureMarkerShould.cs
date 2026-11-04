namespace Source191.Torni.HeadlessTerminal.Tests;

public sealed class CaptureMarkerShould
{
    // \u escapes are fixed length; \x07V would be read as one character.
    private const string Capture = "\u001b]0;Torni\u0007\u001b[2J\u001b[1;1HNAME\u001b[31m STATE\u001b[0m\u001b]0;Torni (Keys)\u0007Keys\u001b]2;Torni\u001b\\";

    [Fact]
    public void MatchATitleOnlyWhenItIsTheWholeTitle()
    {
        CaptureMarker keys = new CaptureMarker.Title("Torni (Keys)");
        CaptureMarker torni = new CaptureMarker.Title("Torni");

        int afterKeys = keys.FindAfter(Capture, 0) ?? -1;

        Capture[..afterKeys].ShouldEndWith("Torni (Keys)\u0007");
        torni.FindAfter(Capture, afterKeys).ShouldBe(Capture.Length);
        keys.FindAfter(Capture, afterKeys).ShouldBeNull();
    }

    [Fact]
    public void MatchAnOsc0OrOsc2TitleEndedByBelOrStringTerminator()
    {
        CaptureMarker torni = new CaptureMarker.Title("Torni");

        torni.FindAfter(Capture, 0).ShouldBe("\u001b]0;Torni\u0007".Length);
        torni.FindAfter(Capture, 1).ShouldBe(Capture.Length);
    }

    [Fact]
    public void MatchScreenTextWithTheEscapeSequencesBetweenItsPartsStripped()
    {
        CaptureMarker header = new CaptureMarker.ScreenText("NAME STATE");

        int after = header.FindAfter(Capture, 0) ?? -1;

        Capture[..after].ShouldEndWith("NAME\u001b[31m STATE");
    }

    [Fact]
    public void NeverMatchScreenTextInsideATitle()
    {
        CaptureMarker keys = new CaptureMarker.ScreenText("Torni (Keys)");

        keys.FindAfter(Capture, 0).ShouldBeNull();
    }

    [Fact]
    public void SearchOnlyFromTheGivenPosition()
    {
        CaptureMarker name = new CaptureMarker.ScreenText("NAME");
        int afterName = name.FindAfter(Capture, 0) ?? -1;

        name.FindAfter(Capture, afterName).ShouldBeNull();
    }

    [Fact]
    public void MatchNothingAtOnce()
    {
        CaptureMarker.Nothing.FindAfter(Capture, 7).ShouldBe(7);
    }

    [Fact]
    public void DescribeItselfForAFailedStep()
    {
        new CaptureMarker.Title("Torni (Keys)").ToString().ShouldBe("the title \"Torni (Keys)\"");
        new CaptureMarker.ScreenText("DIRTY").ToString().ShouldBe("the text \"DIRTY\"");
        CaptureMarker.Nothing.ToString().ShouldBe("nothing");
    }
}