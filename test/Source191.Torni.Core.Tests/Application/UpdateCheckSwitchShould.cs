using Source191.Torni.Application;
using Source191.Torni.Ports;
using Source191.Torni.TestDoubles;

namespace Source191.Torni.Core.Tests.Application;

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
        await store.WriteConfigAsync(new UserConfig([@"C:\Users\me\source\repos"], null, null, null, CheckForUpdates: false));

        UpdateCheckSwitch updateSwitch = new(await SettingsAsync(), string.Empty);

        updateSwitch.IsOn().ShouldBeFalse();
    }

    [Fact]
    public async Task FollowASaveWithoutARestart()
    {
        CurrentSettings settings = await SettingsAsync();
        UpdateCheckSwitch updateSwitch = new(settings, string.Empty);

        bool before = updateSwitch.IsOn();
        await settings.SaveAsync(settings.Value.WithUpdateCheckToggled());

        before.ShouldBeTrue();
        updateSwitch.IsOn().ShouldBeFalse();
    }

    private Task<CurrentSettings> SettingsAsync() => CurrentSettings.LoadAsync(store, TestSettings.Home);
}