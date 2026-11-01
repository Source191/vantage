using Source191.Vantage.Application.Views;

namespace Source191.Vantage.Core.Tests.Application.Views;

public sealed class TabKeysShould
{
    public static TheoryData<Key, ViewEffect> TabKeysAndTheirEffects => new()
    {
        { Key.Left, new PreviousTab() },
        { Key.Right, new NextTab() },
        { Key.Of('1'), new SelectTab(ConsoleTab.Sessions) },
        { Key.Of('2'), new SelectTab(ConsoleTab.Worktrees) },
        { Key.Of('3'), new SelectTab(ConsoleTab.Config) },
    };

    [Theory]
    [MemberData(nameof(TabKeysAndTheirEffects))]
    public void AnswerTheEffectOfATabKey(Key key, ViewEffect expected)
    {
        TabKeys.TryFor(key, out ViewEffect? effect).ShouldBeTrue();
        effect.ShouldBe(expected);
    }

    [Theory]
    [InlineData('4')]
    [InlineData('q')]
    public void AnswerNoEffectForAKeyThatIsNotATabKey(char character)
    {
        TabKeys.TryFor(Key.Of(character), out ViewEffect? effect).ShouldBeFalse();
        effect.ShouldBeNull();
    }
}