using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Views;

namespace Source191.Vantage.Core.Tests.Application;

/// <summary>
/// Every effect that leaves the console is the exit or one the terminal effects run, and every effect that switches
/// tab is one the console applies: a new marked effect fails here before it fails at a key.
/// </summary>
public sealed class EffectRoutingShould
{
    private static IEnumerable<Type> EffectsMarked<TMarker>() => typeof(ViewEffect).Assembly.GetTypes().Where(type => !type.IsAbstract && type.IsAssignableTo(typeof(ViewEffect)) && type.IsAssignableTo(typeof(TMarker)));

    [Fact]
    public void QuitOrUseTheTerminalForEveryEffectThatLeavesTheScreen()
    {
        EffectsMarked<ILeavesScreen>().ShouldAllBe(type => type == typeof(Quit) || TerminalEffects.Handles(type), "an effect that leaves the screen is neither the exit nor run by TerminalEffects");
        TerminalEffects.Handles(typeof(AttachToSession)).ShouldBeTrue();
        TerminalEffects.Handles(typeof(Quit)).ShouldBeFalse();
    }

    [Fact]
    public void ApplyEveryEffectThatSwitchesTab()
    {
        EffectsMarked<ISwitchesTab>().ShouldAllBe(type => ConsoleView.Handles(type), "an effect that switches tab has no case in ConsoleView.Apply");
        EffectsMarked<ISwitchesTab>().ShouldNotContain(type => type.IsAssignableTo(typeof(ILeavesScreen)), "a tab switch never leaves the screen");
    }
}
