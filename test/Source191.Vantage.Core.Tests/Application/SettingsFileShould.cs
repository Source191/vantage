using Source191.Vantage.Application;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class SettingsFileShould
{
    private static readonly FolderPath Home = new(@"C:\Users\me");

    [Fact]
    public void ResolveAMissingFileToEveryDefault()
    {
        Settings settings = SettingsFile.From(config: null, Home);

        settings.RepositoryRoots.ShouldBe([new FolderPath(@"C:\Users\me\source\repos")]);
        settings.WorktreeRoot.ShouldBe(new FolderPath(@"C:\Users\me\source\worktrees"));
        settings.Editor.ShouldBe(EditorChoice.VsCode);
        settings.DefaultBase.ShouldBe(WorktreeBase.RemoteDefault);
    }

    [Fact]
    public void ResolveAFileFromAnEarlierVersion()
    {
        Settings settings = SettingsFile.From(new VantageConfig([@"D:\work"], WorktreeBase.Head, WorktreeRoot: null, Editor: null), Home);

        settings.RepositoryRoots.ShouldBe([new FolderPath(@"D:\work")]);
        settings.DefaultBase.ShouldBe(WorktreeBase.Head);
        settings.WorktreeRoot.ShouldBe(new FolderPath(@"D:\worktrees"));
        settings.Editor.ShouldBe(EditorChoice.VsCode);
    }

    [Fact]
    public void SkipRootsThatAreNotFullPaths()
    {
        Settings settings = SettingsFile.From(new VantageConfig(["repos", @"D:\work"], DefaultBase: null, WorktreeRoot: "trees", Editor: null), Home);

        settings.RepositoryRoots.ShouldBe([new FolderPath(@"D:\work")]);
        settings.WorktreeRoot.ShouldBe(new FolderPath(@"D:\worktrees"));
    }

    [Fact]
    public void ReadTheEditorPresetByName()
    {
        SettingsFile.From(new VantageConfig(null, null, null, new EditorSetting("rider", null, null)), Home).Editor.ShouldBe(EditorChoice.Of(EditorPreset.Rider));
    }

    [Fact]
    public void ReadAnUnknownEditorPresetAsVsCode()
    {
        SettingsFile.From(new VantageConfig(null, null, null, new EditorSetting("Notepad", null, null)), Home).Editor.ShouldBe(EditorChoice.VsCode);
    }

    [Fact]
    public void ReadACustomEditorWithoutAPlaceholderAsVsCode()
    {
        SettingsFile.From(new VantageConfig(null, null, null, new EditorSetting("Custom", @"C:\Tools\subl.exe", ["-n"])), Home).Editor.ShouldBe(EditorChoice.VsCode);
    }

    [Fact]
    public void RoundTripThroughTheFileRecord()
    {
        Settings settings = Settings.Resolve([new FolderPath(@"C:\repos"), new FolderPath(@"D:\work")], new FolderPath(@"E:\trees"), EditorChoice.Custom(@"C:\Tools\subl.exe", ["-n", "{folder}"]), WorktreeBase.Head, Home);

        VantageConfig config = SettingsFile.ToConfig(settings);

        config.ShouldBe(new VantageConfig([@"C:\repos", @"D:\work"], WorktreeBase.Head, @"E:\trees", new EditorSetting("Custom", @"C:\Tools\subl.exe", ["-n", "{folder}"])));
        SettingsFile.From(config, Home).ShouldBe(settings);
    }

    [Fact]
    public void WriteAPresetWithoutACommand()
    {
        SettingsFile.ToConfig(SettingsFile.From(config: null, Home)).Editor.ShouldBe(new EditorSetting("VsCode", Executable: null, Arguments: null));
    }
}
