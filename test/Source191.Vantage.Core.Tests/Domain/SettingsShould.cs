using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

public sealed class SettingsShould
{
    private static readonly FolderPath Home = new(@"C:\Users\me");
    private const string Root = @"C:\Users\me\source\repos";

    private static readonly SettingsText Typed = new(
        Root,
        @"C:\Users\me\source\worktrees",
        EditorPreset.VsCode,
        Executable: string.Empty,
        Arguments: string.Empty,
        WorktreeBase.RemoteDefault);

    [Fact]
    public void ResolveAMissingFileToEveryDefault()
    {
        Settings settings = Settings.Resolve(roots: null, worktreeRoot: null, EditorChoice.VsCode, defaultBase: null, Home);

        settings.RepositoryRoots.ShouldBe([new FolderPath(Root)]);
        settings.WorktreeRoot.ShouldBe(new FolderPath(@"C:\Users\me\source\worktrees"));
        settings.Editor.ShouldBe(EditorChoice.VsCode);
        settings.DefaultBase.ShouldBe(WorktreeBase.RemoteDefault);
    }

    [Fact]
    public void ResolveAFileFromAnEarlierVersion()
    {
        Settings settings = Settings.Resolve([new FolderPath(@"D:\work")], worktreeRoot: null, EditorChoice.VsCode, WorktreeBase.Head, Home);

        settings.RepositoryRoots.ShouldBe([new FolderPath(@"D:\work")]);
        settings.WorktreeRoot.ShouldBe(new FolderPath(@"D:\worktrees"));
        settings.Editor.ShouldBe(EditorChoice.VsCode);
        settings.DefaultBase.ShouldBe(WorktreeBase.Head);
    }

    [Fact]
    public void ResolveAnEmptyRootListToTheDefaultRoot()
    {
        Settings settings = Settings.Resolve([], worktreeRoot: null, EditorChoice.VsCode, defaultBase: null, Home);

        settings.RepositoryRoots.ShouldBe([new FolderPath(Root)]);
    }

    [Fact]
    public void PutTheWorktreeRootInsideARootWithNoParent()
    {
        Settings settings = Settings.Resolve([new FolderPath(@"D:\")], worktreeRoot: null, EditorChoice.VsCode, defaultBase: null, Home);

        settings.WorktreeRoot.ShouldBe(new FolderPath(@"D:\worktrees"));
    }

    [Fact]
    public void KeepAWorktreeRootThatIsGiven()
    {
        Settings settings = Settings.Resolve([new FolderPath(Root)], new FolderPath(@"E:\trees"), EditorChoice.VsCode, defaultBase: null, Home);

        settings.WorktreeRoot.ShouldBe(new FolderPath(@"E:\trees"));
    }

    [Fact]
    public void CompareByValue()
    {
        Settings first = Settings.Resolve([new FolderPath(Root)], worktreeRoot: null, EditorChoice.Custom("subl.exe", ["{folder}"]), defaultBase: null, Home);
        Settings second = Settings.Resolve([new FolderPath(Root)], worktreeRoot: null, EditorChoice.Custom("subl.exe", ["{folder}"]), defaultBase: null, Home);

        first.ShouldBe(second);
    }

    [Fact]
    public void AcceptTypedSettings()
    {
        SettingsText typed = Typed with { Roots = $"{Root}\r\n\r\n  D:\\work  \n", Base = WorktreeBase.Head };

        Settings settings = Settings.Validate(typed, Exists(Root, @"D:\work")).ShouldBeOfType<SettingsValidation.Valid>().Settings;

        settings.RepositoryRoots.ShouldBe([new FolderPath(Root), new FolderPath(@"D:\work")]);
        settings.WorktreeRoot.ShouldBe(new FolderPath(@"C:\Users\me\source\worktrees"));
        settings.DefaultBase.ShouldBe(WorktreeBase.Head);
    }

    [Fact]
    public void RefuseNoRoots()
    {
        Refusal(Typed with { Roots = " \n " }).ShouldBe(new SettingsProblem.NoRoots());
    }

    [Fact]
    public void RefuseARelativeRoot()
    {
        Refusal(Typed with { Roots = "repos" }).ShouldBe(new SettingsProblem.RootNotFullPath("repos"));
    }

    [Fact]
    public void RefuseAMissingRoot()
    {
        Refusal(Typed with { Roots = $"{Root}\nD:\\nowhere" }).ShouldBe(new SettingsProblem.RootMissing(new FolderPath(@"D:\nowhere")));
    }

    [Fact]
    public void RefuseARelativeWorktreeRoot()
    {
        Refusal(Typed with { WorktreeRoot = "worktrees" }).ShouldBe(new SettingsProblem.WorktreeRootNotFullPath());
    }

    [Fact]
    public void AcceptAWorktreeRootThatDoesNotExistYet()
    {
        Settings.Validate(Typed with { WorktreeRoot = @"D:\worktrees" }, Exists(Root))
            .ShouldBeOfType<SettingsValidation.Valid>().Settings.WorktreeRoot.ShouldBe(new FolderPath(@"D:\worktrees"));
    }

    [Fact]
    public void RefuseACustomEditorWithNoExecutable()
    {
        Refusal(Typed with { Preset = EditorPreset.Custom, Executable = "  ", Arguments = "{folder}" }).ShouldBe(new SettingsProblem.NoExecutable());
    }

    [Theory]
    [InlineData(@"C:\Tools\edit.cmd")]
    [InlineData(@"C:\Tools\EDIT.BAT ")]
    public void RefuseABatchFileEditor(string executable)
    {
        Refusal(Typed with { Preset = EditorPreset.Custom, Executable = executable, Arguments = "{folder}" }).ShouldBe(new SettingsProblem.BatchFileExecutable());
    }

    [Fact]
    public void RefuseACustomEditorWithNoPlaceholder()
    {
        Refusal(Typed with { Preset = EditorPreset.Custom, Executable = @"C:\Tools\subl.exe", Arguments = "-n" }).ShouldBe(new SettingsProblem.NoFolderPlaceholder());
    }

    [Fact]
    public void AcceptACustomEditorWithItsArguments()
    {
        SettingsText typed = Typed with { Preset = EditorPreset.Custom, Executable = @" C:\Tools\subl.exe ", Arguments = "-n {folder}" };

        Settings.Validate(typed, Exists(Root)).ShouldBeOfType<SettingsValidation.Valid>()
            .Settings.Editor.ShouldBe(EditorChoice.Custom(@"C:\Tools\subl.exe", ["-n", "{folder}"]));
    }

    [Fact]
    public void IgnoreTheCustomFieldsForAPreset()
    {
        SettingsText typed = Typed with { Preset = EditorPreset.Rider, Executable = @"C:\Tools\edit.cmd", Arguments = "-n" };

        Settings.Validate(typed, Exists(Root)).ShouldBeOfType<SettingsValidation.Valid>().Settings.Editor.ShouldBe(EditorChoice.Of(EditorPreset.Rider));
    }

    [Fact]
    public void GiveBackTheTextItWasTypedFrom()
    {
        Settings settings = Settings.Resolve([new FolderPath(Root), new FolderPath(@"D:\work")], new FolderPath(@"E:\trees"), EditorChoice.Custom("subl.exe", ["-n", "{folder}"]), WorktreeBase.Head, Home);

        settings.ToText().ShouldBe(new SettingsText($"{Root}\nD:\\work", @"E:\trees", EditorPreset.Custom, "subl.exe", "-n {folder}", WorktreeBase.Head));
    }

    private static SettingsProblem Refusal(SettingsText typed) => Settings.Validate(typed, Exists(Root)).ShouldBeOfType<SettingsValidation.Refused>().Problem;

    private static Func<string, bool> Exists(params string[] folders) => path => folders.Contains(path, StringComparer.OrdinalIgnoreCase);
}