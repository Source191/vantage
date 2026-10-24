using Source191.Vantage.Application.Views;
using Source191.Vantage.Tui;

namespace Source191.Vantage.Core.Tests.Tui;

/// <summary>
/// Every effect marked <see cref="IDialogEffect"/> has a flow in <see cref="DialogEffects"/>; without one the user would
/// meet "No dialog is defined for..." at runtime. The same shape as the registration fact in <c>EffectDispatcherShould</c>.
/// </summary>
public sealed class DialogEffectsShould
{
    public static TheoryData<Type> DialogEffectTypes => new(
        typeof(ViewEffect).Assembly.GetTypes().Where(type => !type.IsAbstract && type.IsAssignableTo(typeof(IDialogEffect))));

    [Theory]
    [MemberData(nameof(DialogEffectTypes))]
    public void HaveAFlowForEveryDialogEffect(Type type)
    {
        DialogEffects.Handles(type).ShouldBeTrue($"{type.Name} is an IDialogEffect with no flow in DialogEffects");
    }

    [Fact]
    public void NotClaimABackgroundEffect()
    {
        DialogEffects.Handles(typeof(StopSession)).ShouldBeFalse();
    }
}