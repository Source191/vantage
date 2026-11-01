using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

public sealed class SettingsShould
{
    private static readonly FolderPath Home = FolderPath.From(@"C:\Users\me");
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
        Settings settings = Settings.Resolve(roots: [], new WorktreeRootChoice.BesideFirstRoot(), EditorChoice.VsCode, Settings.BaseWhenUnset, Home);

        settings.RepositoryRoots.ShouldBe([FolderPath.From(Root)]);
        settings.WorktreeRoot.ShouldBe(FolderPath.From(@"C:\Users\me\source\worktrees"));
        settings.Editor.ShouldBe(EditorChoice.VsCode);
        settings.DefaultBase.ShouldBe(WorktreeBase.RemoteDefault);
        settings.DefaultPlace.ShouldBe(LaunchMode.NewWorktree);
    }

    [Fact]
    public void NameTheDefaultsForWhatAFileLeavesOut()
    {
        Settings.BaseWhenUnset.ShouldBe(WorktreeBase.RemoteDefault);
        Settings.ChecksForUpdatesWhenUnset.ShouldBeTrue();
        Settings.PlaceWhenUnset.ShouldBe(LaunchMode.NewWorktree);
    }

    [Fact]
    public void ResolveAFileFromAnEarlierVersion()
    {
        Settings settings = Settings.Resolve([FolderPath.From(@"D:\work")], new WorktreeRootChoice.BesideFirstRoot(), EditorChoice.VsCode, WorktreeBase.Head, Home);

        settings.RepositoryRoots.ShouldBe([FolderPath.From(@"D:\work")]);
        settings.WorktreeRoot.ShouldBe(FolderPath.From(@"D:\worktrees"));
        settings.Editor.ShouldBe(EditorChoice.VsCode);
        settings.DefaultBase.ShouldBe(WorktreeBase.Head);
        settings.DefaultPlace.ShouldBe(LaunchMode.NewWorktree);
    }

    [Fact]
    public void KeepADefaultPlaceThatIsGiven()
    {
        Settings settings = Settings.Resolve(roots: [], new WorktreeRootChoice.BesideFirstRoot(), EditorChoice.VsCode, WorktreeBase.RemoteDefault, Home, defaultPlace: LaunchMode.CurrentBranch);

        settings.DefaultPlace.ShouldBe(LaunchMode.CurrentBranch);
        settings.ShouldNotBe(Settings.Resolve(roots: [], new WorktreeRootChoice.BesideFirstRoot(), EditorChoice.VsCode, WorktreeBase.RemoteDefault, Home));
    }

    [Fact]
    public void ResolveAnEmptyRootListToTheDefaultRoot()
    {
        Settings settings = Settings.Resolve([], new WorktreeRootChoice.BesideFirstRoot(), EditorChoice.VsCode, WorktreeBase.RemoteDefault, Home);

        settings.RepositoryRoots.ShouldBe([FolderPath.From(Root)]);
    }

    [Fact]
    public void PutTheWorktreeRootInsideARootWithNoParent()
    {
        Settings settings = Settings.Resolve([FolderPath.From(@"D:\")], new WorktreeRootChoice.BesideFirstRoot(), EditorChoice.VsCode, WorktreeBase.RemoteDefault, Home);

        settings.WorktreeRoot.ShouldBe(FolderPath.From(@"D:\worktrees"));
    }

    [Fact]
    public void KeepAWorktreeRootThatIsGiven()
    {
        Settings settings = Settings.Resolve([FolderPath.From(Root)], new WorktreeRootChoice.At(FolderPath.From(@"E:\trees")), EditorChoice.VsCode, WorktreeBase.RemoteDefault, Home);

        settings.WorktreeRoot.ShouldBe(FolderPath.From(@"E:\trees"));
    }

    [Fact]
    public void CompareByValue()
    {
        Settings first = Settings.Resolve([FolderPath.From(Root)], new WorktreeRootChoice.BesideFirstRoot(), EditorChoice.Custom(@"C:\Tools\subl.exe", ["{folder}"]), WorktreeBase.RemoteDefault, Home);
        Settings second = Settings.Resolve([FolderPath.From(Root)], new WorktreeRootChoice.BesideFirstRoot(), EditorChoice.Custom(@"C:\Tools\subl.exe", ["{folder}"]), WorktreeBase.RemoteDefault, Home);

        first.ShouldBe(second);
    }

    [Fact]
    public void AcceptTypedSettings()
    {
        SettingsText typed = Typed with { Roots = $"{Root}\r\n\r\n  D:\\work  \n", Base = WorktreeBase.Head, Place = LaunchMode.CurrentBranch };

        Settings settings = Settings.Validate(typed, Exists(Root, @"D:\work")).ShouldBeOfType<SettingsValidation.Valid>().Settings;

        settings.RepositoryRoots.ShouldBe([FolderPath.From(Root), FolderPath.From(@"D:\work")]);
        settings.WorktreeRoot.ShouldBe(FolderPath.From(@"C:\Users\me\source\worktrees"));
        settings.DefaultBase.ShouldBe(WorktreeBase.Head);
        settings.DefaultPlace.ShouldBe(LaunchMode.CurrentBranch);
    }

    [Fact]
    public void KeepTheDefaultPlaceForACustomEditor()
    {
        SettingsText typed = Typed with { Preset = EditorPreset.Custom, Executable = @"C:\Tools\subl.exe", Arguments = "{folder}", Place = LaunchMode.CurrentBranch };

        Settings.Validate(typed, Exists(Root)).ShouldBeOfType<SettingsValidation.Valid>().Settings.DefaultPlace.ShouldBe(LaunchMode.CurrentBranch);
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
        Refusal(Typed with { Roots = $"{Root}\nD:\\nowhere" }).ShouldBe(new SettingsProblem.RootMissing(FolderPath.From(@"D:\nowhere")));
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
            .ShouldBeOfType<SettingsValidation.Valid>().Settings.WorktreeRoot.ShouldBe(FolderPath.From(@"D:\worktrees"));
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

    [Theory]
    [InlineData("subl")]
    [InlineData("subl.exe")]
    [InlineData(@"Tools\subl.exe")]
    public void RefuseACustomEditorThatIsNotAFullPath(string executable)
    {
        Refusal(Typed with { Preset = EditorPreset.Custom, Executable = executable, Arguments = "{folder}" }).ShouldBe(new SettingsProblem.ExecutableNotFullPath());
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
    public void RefuseARootListedTwice()
    {
        SettingsText typed = Typed with { Roots = $"{Root}\n" + @"C:\Users\ME\source\repos\" };

        Settings.Validate(typed, Exists(Root)).ShouldBeOfType<SettingsValidation.Refused>().Problem.ShouldBe(new SettingsProblem.RootAlreadyListed(FolderPath.From(Root)));
    }

    [Fact]
    public void TurnTheUpdateCheckOffAndOn()
    {
        Settings settings = Settings.Resolve(roots: [], new WorktreeRootChoice.BesideFirstRoot(), EditorChoice.VsCode, WorktreeBase.RemoteDefault, Home);

        settings.WithUpdateCheckToggled().CheckForUpdates.ShouldBeFalse();
        settings.WithUpdateCheckToggled().WithUpdateCheckToggled().ShouldBe(settings);
        Settings.Resolve(roots: [], new WorktreeRootChoice.BesideFirstRoot(), EditorChoice.VsCode, WorktreeBase.RemoteDefault, Home, defaultPlace: LaunchMode.CurrentBranch).WithUpdateCheckToggled().DefaultPlace.ShouldBe(LaunchMode.CurrentBranch);
    }

    [Fact]
    public void GiveBackTheTextItWasTypedFrom()
    {
        Settings settings = Settings.Resolve([FolderPath.From(Root), FolderPath.From(@"D:\work")], new WorktreeRootChoice.At(FolderPath.From(@"E:\trees")), EditorChoice.Custom(@"C:\Tools\subl.exe", ["-n", "{folder}"]), WorktreeBase.Head, Home, defaultPlace: LaunchMode.CurrentBranch);

        settings.ToText().ShouldBe(new SettingsText($"{Root}\nD:\\work", @"E:\trees", EditorPreset.Custom, @"C:\Tools\subl.exe", "-n {folder}", WorktreeBase.Head, Place: LaunchMode.CurrentBranch));
    }

    [Fact]
    public void CheckForUpdatesUnlessTurnedOff()
    {
        Settings.Resolve(roots: [], new WorktreeRootChoice.BesideFirstRoot(), EditorChoice.VsCode, WorktreeBase.RemoteDefault, Home).CheckForUpdates.ShouldBeTrue();
        Settings off = Settings.Resolve(roots: [], new WorktreeRootChoice.BesideFirstRoot(), EditorChoice.VsCode, WorktreeBase.RemoteDefault, Home, checkForUpdates: false);

        off.CheckForUpdates.ShouldBeFalse();
        off.ToText().CheckForUpdates.ShouldBeFalse();
        off.ShouldNotBe(Settings.Resolve(roots: [], new WorktreeRootChoice.BesideFirstRoot(), EditorChoice.VsCode, WorktreeBase.RemoteDefault, Home));
    }

    private static SettingsProblem Refusal(SettingsText typed) => Settings.Validate(typed, Exists(Root)).ShouldBeOfType<SettingsValidation.Refused>().Problem;

    private static Func<string, bool> Exists(params string[] folders) => path => folders.Contains(path, StringComparer.OrdinalIgnoreCase);
}