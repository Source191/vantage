namespace Source191.Torni.ReadmeScene.Tests;

public sealed class KeyScriptShould
{
    [Fact]
    public void ReadDelaysKeysAndText()
    {
        IReadOnlyList<KeyStep> steps = KeyScript.Parse("""
            # open the wizard
            1500 press n
            800 type Northwind.Inv
            400 press Tab
            300 press F5
            2000 focus
            500 mark ready
            """);

        steps.ShouldBe(
        [
            new KeyStep.Type(TimeSpan.FromMilliseconds(1500), "n"),
            new KeyStep.Type(TimeSpan.FromMilliseconds(800), "Northwind.Inv"),
            new KeyStep.Press(TimeSpan.FromMilliseconds(400), NamedKey.Tab),
            new KeyStep.Press(TimeSpan.FromMilliseconds(300), NamedKey.F5),
            new KeyStep.FocusOwnTab(TimeSpan.FromMilliseconds(2000)),
            new KeyStep.Mark(TimeSpan.FromMilliseconds(500), "ready"),
        ]);
    }

    [Theory]
    [InlineData("press")]
    [InlineData("100 press PageSideways")]
    [InlineData("soon press Enter")]
    [InlineData("100 dance")]
    public void RefuseALineItCannotRead(string line) => Should.Throw<FormatException>(() => KeyScript.Parse(line));

    [Fact]
    public void AddUpHowLongItTakes()
    {
        IReadOnlyList<KeyStep> steps = KeyScript.Parse("""
            1000 type abc
            500 press Enter
            """);

        KeyScript.Duration(steps).ShouldBe(TimeSpan.FromMilliseconds(1000 + (3 * KeyScript.TypingInterval.TotalMilliseconds) + 500));
    }

    [Fact]
    public void ReadTheShippedScript()
    {
        IReadOnlyList<KeyStep> steps = KeyScript.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "new-session.keys")));

        steps.ShouldContain(new KeyStep.Press(steps.OfType<KeyStep.Press>().First(step => step.Key == NamedKey.F5).Delay, NamedKey.F5));
        steps.OfType<KeyStep.FocusOwnTab>().ShouldNotBeEmpty();
    }
}