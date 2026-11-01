using Source191.Vantage.Application.Views;

namespace Source191.Vantage.Core.Tests.Application.Views;

public sealed class TabKeysShould
{
    public static TheoryData<Key, ViewEffect> TabKeysAndTheirEffects => new()
    {
        { Key.Left, new PreviousTab() },
        { Key.Right, new NextTab() },
    };

    [Theory]
    [MemberData(nameof(TabKeysAndTheirEffects))]
    public void AnswerTheEffectOfATabKey(Key key, ViewEffect expected)
    {
        TabKeys.TryFor(key, out ViewEffect? effect).ShouldBeTrue();
        effect.ShouldBe(expected);
    }

    [Theory]
    [InlineData('1')]
    [InlineData('2')]
    [InlineData('3')]
    [InlineData('4')]
    [InlineData('q')]
    public void AnswerNoEffectForAKeyThatIsNotATabKey(char character)
    {
        TabKeys.TryFor(Key.Of(character), out ViewEffect? effect).ShouldBeFalse();
        effect.ShouldBeNull();
    }
}