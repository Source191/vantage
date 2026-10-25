using Source191.Vantage.Application;
using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application.Actions;

public sealed class SaveSettingsHandlerShould
{
    [Fact]
    public async Task SaveThroughCurrentSettingsAndReportSettingsSaved()
    {
        InMemoryState state = new();
        CurrentSettings current = new(TestSettings.For(@"C:\Users\me\source\repos"), state);
        Settings saved = TestSettings.For([@"C:\Users\me\source\repos", @"D:\work"], @"D:\worktrees");

        object outcome = await new SaveSettingsHandler(current).HandleAsync(new SaveSettings(saved), CancellationToken.None);

        outcome.ShouldBe(new SaveSettingsOutcome());
        current.Value.ShouldBe(saved);
        SettingsFile.From(await state.ReadConfigAsync(), TestSettings.Home).ShouldBe(saved);
    }
}
