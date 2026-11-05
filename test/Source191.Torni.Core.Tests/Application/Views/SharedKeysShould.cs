using Source191.Torni.Application.Views;

namespace Source191.Torni.Core.Tests.Application.Views;

public sealed class SharedKeysShould
{
    public static TheoryData<char, ViewEffect> SharedKeysAndTheirEffects => new()
    {
        { 'q', new Quit() },
        { '?', new ShowHelp() },
        { 'U', new SelfUpdate() },
        { '!', new ShowProblemReport() },
    };

    [Theory]
    [MemberData(nameof(SharedKeysAndTheirEffects))]
    public void AnswerTheEffectOfASharedKey(char character, ViewEffect expected)
    {
        SharedKeys.TryFor(Key.Of(character), out ViewEffect? effect).ShouldBeTrue();
        effect.ShouldBe(expected);
    }

    [Fact]
    public void AnswerNoEffectForAKeyThatIsNotShared()
    {
        Key[] others = [Key.Of('1'), Key.Of('x'), Key.Enter];

        foreach (Key key in others)
        {
            SharedKeys.TryFor(key, out ViewEffect? effect).ShouldBeFalse();
            effect.ShouldBeNull();
        }
    }
}