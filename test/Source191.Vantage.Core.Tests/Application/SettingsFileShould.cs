using Source191.Vantage.Application;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class SettingsFileShould
{
    private static readonly FolderPath Home = FolderPath.From(@"C:\Users\me");

    [Fact]
    public void ResolveAMissingConfigToTheDefaults()
    {
        Settings settings = SettingsFile.From(VantageConfig.Missing, Home);

        settings.RepositoryRoots.ShouldBe([FolderPath.From(@"C:\Users\me\source\repos")]);
        settings.WorktreeRoot.ShouldBe(FolderPath.From(@"C:\Users\me\source\worktrees"));
        settings.Editor.ShouldBe(EditorChoice.VsCode);
        settings.DefaultBase.ShouldBe(WorktreeBase.RemoteDefault);
        settings.DefaultPlace.ShouldBe(LaunchMode.NewWorktree);
    }

    [Fact]
    public void RefuseANullConfig()
    {
        Should.Throw<ArgumentNullException>(() => SettingsFile.From(null!, Home)).ParamName.ShouldBe("config");
    }

    [Fact]
    public void CheckForUpdatesWhenTheFileSaysNothing()
    {
        SettingsFile.From(VantageConfig.Missing, Home).CheckForUpdates.ShouldBeTrue();
        SettingsFile.From(new VantageConfig([@"D:\work"], WorktreeBase.Head, WorktreeRoot: null, Editor: null), Home).CheckForUpdates.ShouldBeTrue();
    }

    [Fact]
    public void ReadAndWriteTheUpdateCheckTurnedOff()
    {
        Settings settings = SettingsFile.From(new VantageConfig([@"D:\work"], WorktreeBase.Head, WorktreeRoot: null, Editor: null, CheckForUpdates: false), Home);

        settings.CheckForUpdates.ShouldBeFalse();
        SettingsFile.ToConfig(settings).CheckForUpdates.ShouldBe(false);
        SettingsFile.ToConfig(SettingsFile.From(VantageConfig.Missing, Home)).CheckForUpdates.ShouldBe(true);
    }

    [Fact]
    public void ResolveAFileFromAnEarlierVersion()
    {
        Settings settings = SettingsFile.From(new VantageConfig([@"D:\work"], WorktreeBase.Head, WorktreeRoot: null, Editor: null), Home);

        settings.RepositoryRoots.ShouldBe([FolderPath.From(@"D:\work")]);
        settings.DefaultBase.ShouldBe(WorktreeBase.Head);
        settings.WorktreeRoot.ShouldBe(FolderPath.From(@"D:\worktrees"));
        settings.Editor.ShouldBe(EditorChoice.VsCode);
        settings.DefaultPlace.ShouldBe(LaunchMode.NewWorktree);
    }

    [Fact]
    public void ReadAndWriteTheDefaultPlace()
    {
        Settings settings = SettingsFile.From(new VantageConfig([@"D:\work"], DefaultBase: null, WorktreeRoot: null, Editor: null, DefaultPlace: LaunchMode.CurrentBranch), Home);

        settings.DefaultPlace.ShouldBe(LaunchMode.CurrentBranch);
        SettingsFile.ToConfig(settings).DefaultPlace.ShouldBe(LaunchMode.CurrentBranch);
        SettingsFile.ToConfig(SettingsFile.From(VantageConfig.Missing, Home)).DefaultPlace.ShouldBe(LaunchMode.NewWorktree);
    }

    [Fact]
    public void SkipRootsThatAreNotFullPaths()
    {
        Settings settings = SettingsFile.From(new VantageConfig(["repos", @"D:\work"], DefaultBase: null, WorktreeRoot: "trees", Editor: null), Home);

        settings.RepositoryRoots.ShouldBe([FolderPath.From(@"D:\work")]);
        settings.WorktreeRoot.ShouldBe(FolderPath.From(@"D:\worktrees"));
    }

    [Fact]
    public void ReadTheEditorPresetByName()
    {
        SettingsFile.From(new VantageConfig(null, null, null, new EditorSetting("rider", null, null)), Home).Editor.ShouldBe(EditorChoice.Of(EditorPreset.Rider));
    }

    [Fact]
    public void ReadAnOldCursorPresetAsVsCode()
    {
        Settings settings = SettingsFile.From(new VantageConfig([@"D:\work"], null, null, new EditorSetting("Cursor", null, null)), Home);

        settings.Editor.ShouldBe(EditorChoice.VsCode);
        settings.RepositoryRoots.ShouldBe([FolderPath.From(@"D:\work")]);
    }

    [Fact]
    public void ReadAVisualStudioPreset()
    {
        SettingsFile.From(new VantageConfig(null, null, null, new EditorSetting("VisualStudio", null, null)), Home).Editor.ShouldBe(EditorChoice.Of(EditorPreset.VisualStudio));
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
    public void KeepACustomEditorWhoseExecutableIsMissing()
    {
        EditorChoice editor = SettingsFile.From(new VantageConfig(null, null, null, new EditorSetting("Custom", @"E:\Tools\subl.exe", ["{folder}"])), Home).Editor;

        editor.Preset.ShouldBe(EditorPreset.Custom, "drive E: may simply not be attached; f reports it");
        editor.Executable.ShouldBe(@"E:\Tools\subl.exe");
    }

    [Fact]
    public void RoundTripThroughTheFileRecord()
    {
        Settings settings = Settings.Resolve([FolderPath.From(@"C:\repos"), FolderPath.From(@"D:\work")], new WorktreeRootChoice.At(FolderPath.From(@"E:\trees")), EditorChoice.Custom(@"C:\Tools\subl.exe", ["-n", "{folder}"]), WorktreeBase.Head, Home, defaultPlace: LaunchMode.CurrentBranch);

        VantageConfig config = SettingsFile.ToConfig(settings);

        config.ShouldBe(new VantageConfig([@"C:\repos", @"D:\work"], WorktreeBase.Head, @"E:\trees", new EditorSetting("Custom", @"C:\Tools\subl.exe", ["-n", "{folder}"]), CheckForUpdates: true, DefaultPlace: LaunchMode.CurrentBranch));
        SettingsFile.From(config, Home).ShouldBe(settings);
    }

    [Fact]
    public void WriteAPresetWithoutACommand()
    {
        SettingsFile.ToConfig(SettingsFile.From(VantageConfig.Missing, Home)).Editor.ShouldBe(new EditorSetting("VsCode", Executable: null, Arguments: null));
    }
}