using Source191.Vantage.Application;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class UpdateCheckSwitchShould
{
    private readonly InMemoryState store = new();

    [Fact]
    public async Task BeOnWhenTheSettingIsOnAndNothingOptsOut()
    {
        UpdateCheckSwitch updateSwitch = new(await SettingsAsync(), string.Empty);

        updateSwitch.IsOn().ShouldBeTrue();
    }

    [Fact]
    public async Task BeOffWhenTheVariableOptsOut()
    {
        UpdateCheckSwitch updateSwitch = new(await SettingsAsync(), "1");

        updateSwitch.IsOn().ShouldBeFalse();
    }

    [Fact]
    public async Task BeOffWhenTheSettingIsOff()
    {
        await store.WriteConfigAsync(new VantageConfig([@"C:\Users\me\source\repos"], null, null, null, CheckForUpdates: false));

        UpdateCheckSwitch updateSwitch = new(await SettingsAsync(), string.Empty);

        updateSwitch.IsOn().ShouldBeFalse();
    }

    private Task<CurrentSettings> SettingsAsync() => CurrentSettings.LoadAsync(store, TestSettings.Home);
}
