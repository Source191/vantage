using Source191.Vantage.Application;
using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Views;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application.Actions;

public sealed class ToggleUpdateCheckHandlerShould
{
    [Fact]
    public async Task TurnTheCheckOffAndSaveIt()
    {
        InMemoryState state = new();
        CurrentSettings current = new(TestSettings.For(@"C:\Users\me\source\repos"), state);

        object outcome = await new ToggleUpdateCheckHandler(current).HandleAsync(new ToggleUpdateCheck(), CancellationToken.None);

        outcome.ShouldBe(new ToggleUpdateCheckOutcome(On: false));
        current.Value.CheckForUpdates.ShouldBeFalse();
        SettingsFile.From(await state.ReadConfigAsync(), TestSettings.Home).CheckForUpdates.ShouldBeFalse();
    }

    [Fact]
    public async Task TurnTheCheckBackOn()
    {
        CurrentSettings current = new(TestSettings.For(@"C:\Users\me\source\repos").WithUpdateCheckToggled(), new InMemoryState());

        object outcome = await new ToggleUpdateCheckHandler(current).HandleAsync(new ToggleUpdateCheck(), CancellationToken.None);

        outcome.ShouldBe(new ToggleUpdateCheckOutcome(On: true));
        current.Value.CheckForUpdates.ShouldBeTrue();
    }
}