using Source191.Torni.Application;
using Source191.Torni.Domain;
using Source191.Torni.Ports;
using Source191.Torni.TestDoubles;

namespace Source191.Torni.Core.Tests.Application;

public sealed class CurrentSettingsShould
{
    private static readonly FolderPath Home = FolderPath.From(@"C:\Users\me");
    private static readonly Settings Saved = Settings.Resolve([FolderPath.From(@"D:\work")], new WorktreeRootChoice.At(FolderPath.From(@"D:\worktrees")), EditorChoice.Of(EditorPreset.VisualStudio), WorktreeBase.Head, Home);

    [Fact]
    public async Task AnswerTheSettingsItWasBuiltWith()
    {
        CurrentSettings current = await CurrentSettings.LoadAsync(new InMemoryState(), Home);

        current.Value.ShouldBe(SettingsFile.From(UserConfig.Missing, Home));
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
}