using Source191.Vantage.Application;
using Source191.Vantage.Domain;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class CurrentSettingsShould
{
    private static readonly FolderPath Home = FolderPath.From(@"C:\Users\me");
    private static readonly Settings Saved = Settings.Resolve([FolderPath.From(@"D:\work")], FolderPath.From(@"D:\worktrees"), EditorChoice.Of(EditorPreset.Cursor), WorktreeBase.Head, Home);

    [Fact]
    public async Task AnswerTheSettingsItWasBuiltWith()
    {
        CurrentSettings current = await CurrentSettings.LoadAsync(new InMemoryState(), Home);

        current.Value.ShouldBe(SettingsFile.From(config: null, Home));
    }

    [Fact]
    public async Task AnswerTheSavedSettingsAfterASave()
    {
        CurrentSettings current = await CurrentSettings.LoadAsync(new InMemoryState(), Home);

        await current.SaveAsync(Saved);

        current.Value.ShouldBe(Saved);
    }

    [Fact]
    public async Task WriteTheSavedSettingsThroughThePort()
    {
        InMemoryState state = new();
        CurrentSettings current = await CurrentSettings.LoadAsync(state, Home);

        await current.SaveAsync(Saved);

        (await CurrentSettings.LoadAsync(state, Home)).Value.ShouldBe(Saved);
    }

    [Fact]
    public void RefuseNullInitialSettings()
    {
        ArgumentNullException refused = Should.Throw<ArgumentNullException>(() => new CurrentSettings(null!, new InMemoryState()));

        refused.ParamName.ShouldBe("initial");
    }

    [Fact]
    public void RefuseANullStore()
    {
        ArgumentNullException refused = Should.Throw<ArgumentNullException>(() => new CurrentSettings(Saved, null!));

        refused.ParamName.ShouldBe("store");
    }
}