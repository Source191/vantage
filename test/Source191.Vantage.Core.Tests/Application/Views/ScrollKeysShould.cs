using Source191.Vantage.Application.Views;

namespace Source191.Vantage.Core.Tests.Application.Views;

public sealed class ScrollKeysShould
{
    [Theory]
    [InlineData('<', -1)]
    [InlineData('>', 1)]
    public void StepTowardsTheArrowOfTheKey(char character, int expected)
    {
        ScrollKeys.TryFor(Key.Of(character), out int step).ShouldBeTrue();
        step.ShouldBe(expected);
    }

    [Fact]
    public void LeaveACharacterThatIsNotAnArrowAlone()
    {
        ScrollKeys.TryFor(Key.Of('a'), out _).ShouldBeFalse();
    }

    [Fact]
    public void LeaveTheTabKeysAlone()
    {
        ScrollKeys.TryFor(Key.Left, out _).ShouldBeFalse();
    }
}