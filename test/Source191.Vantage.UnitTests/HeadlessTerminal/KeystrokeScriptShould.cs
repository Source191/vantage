using Source191.Vantage.HeadlessTerminal;

namespace Source191.Vantage.UnitTests.HeadlessTerminal;

public sealed class KeystrokeScriptShould
{
    [Fact]
    public void AccumulateDelaysAndDecodeJsonEscapes()
    {
        IReadOnlyList<Keystroke> steps = KeystrokeScript.Parse(@"8000:w,2000:\u001b[B,1000:\r,500:q");

        steps.ShouldBe([new Keystroke(8000, "w"), new Keystroke(10000, "\x1b[B"), new Keystroke(11000, "\r"), new Keystroke(11500, "q")]);
    }

    [Fact]
    public void RefuseAStepWithoutADelay()
    {
        Should.Throw<FormatException>(() => KeystrokeScript.Parse("q"));
    }
}