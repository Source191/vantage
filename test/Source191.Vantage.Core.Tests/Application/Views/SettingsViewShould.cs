using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application.Views;

/// <summary>A setting's dialog: one value of the saved settings, what it shows, how it follows its controls, and when it saves.</summary>
public sealed class SettingsViewShould
{
    private const string Root = @"C:\Users\me\source\repos";
    private const string Work = @"D:\work";

    private static readonly Func<string, bool> Exists = path => path is Root or Work;

    private static readonly Func<string, bool> Installed = path => path is @"C:\Tools\subl.exe";

    private static readonly Settings TwoRoots = TestSettings.For([Root, Work], @"D:\worktrees", WorktreeBase.Head);

    private static SettingsState Opened(SettingsScope scope) => SettingsState.Open(TestSettings.For(Root), scope, Exists, Installed);

    public static TheoryData<SettingsScope, SettingsField> Scopes => new()
    {
        { new SettingsScope.Roots(), SettingsField.Roots },
        { new SettingsScope.WorktreeRoot(), SettingsField.WorktreeRoot },
        { new SettingsScope.Editor(), SettingsField.Editor },
        { new SettingsScope.Base(), SettingsField.Base },
        { new SettingsScope.Place(), SettingsField.Place },
    };

    [Theory]
    [MemberData(nameof(Scopes))]
    public void OpenOnTheOneValueWithItsField(SettingsScope scope, SettingsField field)
    {
        SettingsState opened = SettingsState.Open(TwoRoots, scope, Exists, Installed);

        opened.Scope.ShouldBe(scope);
        opened.Focus.ShouldBe(field);
        opened.Problem.ShouldBe(new SettingsProblemLine.None());
        opened.Text.Roots.ShouldBe($"{Root}\n{Work}");
    }

    [Fact]
    public void FollowEachControlChange()
    {
        SettingsState state = Opened(new SettingsScope.Editor());

        state = Reduce(state, new SettingsInput.PresetChanged(EditorPreset.Custom));
        state = Reduce(state, new SettingsInput.ExecutableChanged(@"C:\Tools\subl.exe"));
        state = Reduce(state, new SettingsInput.ArgumentsChanged("-n {folder}"));
        state = Reduce(state, new SettingsInput.FieldFocused(SettingsField.Arguments));

        state.Text.Preset.ShouldBe(EditorPreset.Custom);
        state.Text.Executable.ShouldBe(@"C:\Tools\subl.exe");
        state.Text.Arguments.ShouldBe("-n {folder}");
        state.Focus.ShouldBe(SettingsField.Arguments);
    }

    public static TheoryData<SettingsScope, SettingsInput[], SettingsProblem, SettingsField> Problems => new()
    {
        { new SettingsScope.Roots(), [new SettingsInput.RootsChanged(" ")], new SettingsProblem.NoRoots(), SettingsField.Roots },
        { new SettingsScope.Roots(), [new SettingsInput.RootsChanged("repos")], new SettingsProblem.RootNotFullPath("repos"), SettingsField.Roots },
        { new SettingsScope.Roots(), [new SettingsInput.RootsChanged($"{Root}\n" + @"D:\nowhere")], new SettingsProblem.RootMissing(FolderPath.From(@"D:\nowhere")), SettingsField.Roots },
        { new SettingsScope.Roots(), [new SettingsInput.RootsChanged($"{Root}\n{Root}")], new SettingsProblem.RootAlreadyListed(FolderPath.From(Root)), SettingsField.Roots },
        { new SettingsScope.WorktreeRoot(), [new SettingsInput.WorktreeRootChanged("worktrees")], new SettingsProblem.WorktreeRootNotFullPath(), SettingsField.WorktreeRoot },
        { new SettingsScope.Editor(), [new SettingsInput.PresetChanged(EditorPreset.Custom), new SettingsInput.ArgumentsChanged("{folder}")], new SettingsProblem.NoExecutable(), SettingsField.Executable },
        { new SettingsScope.Editor(), [new SettingsInput.PresetChanged(EditorPreset.Custom), new SettingsInput.ExecutableChanged("subl"), new SettingsInput.ArgumentsChanged("{folder}")], new SettingsProblem.ExecutableNotFullPath(), SettingsField.Executable },
        { new SettingsScope.Editor(), [new SettingsInput.PresetChanged(EditorPreset.Custom), new SettingsInput.ExecutableChanged(@"C:\Tools\edit.cmd"), new SettingsInput.ArgumentsChanged("{folder}")], new SettingsProblem.BatchFileExecutable(), SettingsField.Executable },
        { new SettingsScope.Editor(), [new SettingsInput.PresetChanged(EditorPreset.Custom), new SettingsInput.ExecutableChanged(@"C:\Tools\subl.exe"), new SettingsInput.ArgumentsChanged("-n")], new SettingsProblem.NoFolderPlaceholder(), SettingsField.Arguments },
        { new SettingsScope.Editor(), [new SettingsInput.PresetChanged(EditorPreset.Custom), new SettingsInput.ExecutableChanged(@"C:\Tools\gone.exe"), new SettingsInput.ArgumentsChanged("-n {folder}")], new SettingsProblem.ExecutableMissing(@"C:\Tools\gone.exe"), SettingsField.Executable },
    };

    [Theory]
    [MemberData(nameof(Problems))]
    public void RefuseToSaveWithTheProblemAndItsField(SettingsScope scope, SettingsInput[] inputs, SettingsProblem problem, SettingsField field)
    {
        SettingsState state = inputs.Aggregate(Opened(scope), Reduce);

        SettingsUpdate saved = SettingsView.Reduce(state, new SettingsInput.Save());

        saved.Effect.ShouldBe(new SettingsEffect.None());
        saved.State.Problem.ShouldBe(new SettingsProblemLine.Shows(problem));
        saved.State.Focus.ShouldBe(field);
    }

    [Fact]
    public void SaveARootAddedOnANewLine()
    {
        SettingsState state = Reduce(Opened(new SettingsScope.Roots()), new SettingsInput.RootsChanged($"{Root}\n{Work}"));

        SettingsView.Reduce(state, new SettingsInput.Save()).Effect.ShouldBe(new SettingsEffect.Save(TestSettings.For(Root, Work)));
    }

    [Fact]
    public void SaveTheWorktreeRootKeepingEverythingElse()
    {
        SettingsState state = Reduce(SettingsState.Open(TwoRoots, new SettingsScope.WorktreeRoot(), Exists, Installed), new SettingsInput.WorktreeRootChanged(@"E:\trees"));

        SettingsView.Reduce(state, new SettingsInput.Save()).Effect.ShouldBe(new SettingsEffect.Save(TestSettings.For([Root, Work], @"E:\trees", WorktreeBase.Head)));
    }

    [Fact]
    public void SaveTheBase()
    {
        SettingsState state = Reduce(Opened(new SettingsScope.Base()), new SettingsInput.BaseChanged(WorktreeBase.Head));

        SettingsView.Reduce(state, new SettingsInput.Save()).Effect.ShouldBeOfType<SettingsEffect.Save>().Settings.DefaultBase.ShouldBe(WorktreeBase.Head);
    }

    [Fact]
    public void SaveThePlace()
    {
        SettingsState state = Reduce(Opened(new SettingsScope.Place()), new SettingsInput.PlaceChanged(LaunchMode.CurrentBranch));

        state.Text.Place.ShouldBe(LaunchMode.CurrentBranch);
        SettingsView.Reduce(state, new SettingsInput.Save()).Effect.ShouldBeOfType<SettingsEffect.Save>().Settings.DefaultPlace.ShouldBe(LaunchMode.CurrentBranch);
    }

    [Fact]
    public void AddEachFolderChosenAsALineAfterTheRootsInTheOrderChosen()
    {
        SettingsState state = Reduce(Opened(new SettingsScope.Roots()), new SettingsInput.FoldersChosen([Work, @"E:\personal"]));

        state.Text.Roots.ShouldBe($"{Root}\n{Work}\nE:\\personal");
        state.Problem.ShouldBe(new SettingsProblemLine.None());
        state.Focus.ShouldBe(SettingsField.Roots);
    }

    [Fact]
    public void AddAChosenFolderAfterATrailingNewLineWithoutABlankLine()
    {
        SettingsState typed = Reduce(Opened(new SettingsScope.Roots()), new SettingsInput.RootsChanged($"{Root}\r\n\r\n"));

        Reduce(typed, new SettingsInput.FoldersChosen([Work])).Text.Roots.ShouldBe($"{Root}\n{Work}");
        Reduce(Reduce(typed, new SettingsInput.RootsChanged(" ")), new SettingsInput.FoldersChosen([Work])).Text.Roots.ShouldBe(Work, "an empty box takes the folder as its first line");
    }

    [Fact]
    public void ChangeNothingWhenThePickerChoosesNothing()
    {
        SettingsState opened = Opened(new SettingsScope.Roots());

        Reduce(opened, new SettingsInput.FoldersChosen([])).ShouldBe(opened);
    }

    [Fact]
    public void RefuseABrowsedFolderThatIsAlreadyARootOnlyAtSave()
    {
        SettingsState chosen = Reduce(Opened(new SettingsScope.Roots()), new SettingsInput.FoldersChosen([Root]));

        SettingsState refused = Reduce(chosen, new SettingsInput.Save());

        refused.Problem.ShouldBe(new SettingsProblemLine.Shows(new SettingsProblem.RootAlreadyListed(FolderPath.From(Root))));
        refused.Focus.ShouldBe(SettingsField.Roots);
    }

    [Fact]
    public void BrowseFromTheFolderHoldingTheFirstRoot()
    {
        Opened(new SettingsScope.Roots()).BrowseFrom.ShouldBe(new BrowseStart.At(FolderPath.From(@"C:\Users\me\source")));
        Reduce(Opened(new SettingsScope.Roots()), new SettingsInput.RootsChanged("relative\nD:\\")).BrowseFrom.ShouldBe(new BrowseStart.At(FolderPath.From(@"D:\")), "a root with no parent is its own start");
        Reduce(Opened(new SettingsScope.Roots()), new SettingsInput.RootsChanged(" ")).BrowseFrom.ShouldBe(new BrowseStart.NoFolderTyped());
    }

    [Fact]
    public void ReplaceTheWorktreeRootWithTheFolderChosen()
    {
        SettingsState opened = Opened(new SettingsScope.WorktreeRoot());

        SettingsState chosen = Reduce(opened, new SettingsInput.FoldersChosen([@"D:\trees"]));

        chosen.Text.WorktreeRoot.ShouldBe(@"D:\trees");
        chosen.Text.Roots.ShouldBe(opened.Text.Roots, "the roots are another dialog's");
        Reduce(opened, new SettingsInput.FoldersChosen([])).ShouldBe(opened);
        SettingsView.Reduce(chosen, new SettingsInput.Save()).Effect.ShouldBeOfType<SettingsEffect.Save>().Settings.WorktreeRoot.ShouldBe(FolderPath.From(@"D:\trees"));
    }

    [Fact]
    public void BrowseForTheWorktreeRootFromTheFolderHoldingIt()
    {
        SettingsState opened = Opened(new SettingsScope.WorktreeRoot());

        opened.BrowseFrom.ShouldBe(new BrowseStart.At(FolderPath.From(@"C:\Users\me\source")));
        Reduce(opened, new SettingsInput.WorktreeRootChanged(@"E:\")).BrowseFrom.ShouldBe(new BrowseStart.At(FolderPath.From(@"E:\")), "a worktree root with no parent is its own start");
        Reduce(opened, new SettingsInput.WorktreeRootChanged("trees")).BrowseFrom.ShouldBe(new BrowseStart.NoFolderTyped());
    }

    [Fact]
    public void BrowseOnlyForTheTwoFolderSettings()
    {
        Opened(new SettingsScope.Roots()).Browses.ShouldBe(SettingsBrowse.SeveralFolders);
        Opened(new SettingsScope.WorktreeRoot()).Browses.ShouldBe(SettingsBrowse.OneFolder);
        Opened(new SettingsScope.Editor()).Browses.ShouldBe(SettingsBrowse.No);
        Opened(new SettingsScope.Base()).Browses.ShouldBe(SettingsBrowse.No);
        Opened(new SettingsScope.Place()).Browses.ShouldBe(SettingsBrowse.No);
    }

    [Fact]
    public void BrowseForAFileOnlyWhileCustomIsChosen()
    {
        SettingsState opened = Opened(new SettingsScope.Editor());

        opened.Browses.ShouldBe(SettingsBrowse.No);
        Reduce(opened, new SettingsInput.PresetChanged(EditorPreset.Custom)).Browses.ShouldBe(SettingsBrowse.OneFile);
        Reduce(Reduce(opened, new SettingsInput.PresetChanged(EditorPreset.Custom)), new SettingsInput.PresetChanged(EditorPreset.Rider)).Browses.ShouldBe(SettingsBrowse.No);
    }

    [Fact]
    public void ReplaceTheExecutableWithTheFileChosen()
    {
        SettingsState custom = Reduce(Reduce(Opened(new SettingsScope.Editor()), new SettingsInput.PresetChanged(EditorPreset.Custom)), new SettingsInput.ExecutableChanged(@"C:\Tools\old.exe"));

        SettingsState chosen = Reduce(custom, new SettingsInput.FileChosen(@"C:\Tools\subl.exe"));

        chosen.Text.Executable.ShouldBe(@"C:\Tools\subl.exe");
        Reduce(custom, new SettingsInput.FileChosen(string.Empty)).ShouldBe(custom, "a cancelled picker changes nothing");
        SettingsView.Reduce(Reduce(chosen, new SettingsInput.ArgumentsChanged("-n {folder}")), new SettingsInput.Save()).Effect.ShouldBeOfType<SettingsEffect.Save>()
            .Settings.Editor.ShouldBe(EditorChoice.Custom(@"C:\Tools\subl.exe", ["-n", "{folder}"]), "nothing is saved until F5, which checks the file like a typed one");
    }

    [Fact]
    public void BrowseForTheExecutableFromTheFolderHoldingIt()
    {
        SettingsState custom = Reduce(Opened(new SettingsScope.Editor()), new SettingsInput.PresetChanged(EditorPreset.Custom));

        Reduce(custom, new SettingsInput.ExecutableChanged(@"C:\Tools\subl.exe")).BrowseFrom.ShouldBe(new BrowseStart.At(FolderPath.From(@"C:\Tools")));
        Reduce(custom, new SettingsInput.ExecutableChanged("subl.exe")).BrowseFrom.ShouldBe(new BrowseStart.NoFolderTyped());
        custom.BrowseFrom.ShouldBe(new BrowseStart.NoFolderTyped(), "nothing typed yet");
    }

    [Fact]
    public void ClearTheProblemOnceTheValueIsValid()
    {
        SettingsState refused = SettingsView.Reduce(Reduce(Opened(new SettingsScope.Roots()), new SettingsInput.RootsChanged("repos")), new SettingsInput.Save()).State;

        SettingsUpdate saved = SettingsView.Reduce(Reduce(refused, new SettingsInput.RootsChanged(Root)), new SettingsInput.Save());

        saved.State.Problem.ShouldBe(new SettingsProblemLine.None());
        saved.Effect.ShouldBeOfType<SettingsEffect.Save>();
    }

    [Fact]
    public void CancelYieldsClose()
    {
        SettingsView.Reduce(Reduce(Opened(new SettingsScope.WorktreeRoot()), new SettingsInput.WorktreeRootChanged(@"D:\worktrees")), new SettingsInput.Cancel()).Effect.ShouldBe(new SettingsEffect.Close());
    }

    [Fact]
    public void ShowTheCustomEditorFieldsOnlyForCustom()
    {
        SettingsState opened = Opened(new SettingsScope.Editor());

        opened.ShowsCustomEditor.ShouldBeFalse();
        Reduce(opened, new SettingsInput.PresetChanged(EditorPreset.Custom)).ShowsCustomEditor.ShouldBeTrue();
        Reduce(opened, new SettingsInput.PresetChanged(EditorPreset.Explorer)).ShowsCustomEditor.ShouldBeFalse();
    }

    [Fact]
    public void MarkThePresetsNotFound()
    {
        SettingsState opened = Opened(new SettingsScope.Editor());

        SettingsState located = Reduce(opened, new SettingsInput.PresetsLocated(new HashSet<EditorPreset> { EditorPreset.Rider }));

        opened.Presets.ShouldBe(new PresetSearch.NotYetLooked());
        located.Presets.ShouldBeOfType<PresetSearch.Looked>().Missing.ShouldBe([EditorPreset.Rider]);
        located.Text.ShouldBe(opened.Text, "looking changes no choice");
    }

    [Fact]
    public void SaveAPresetMarkedNotFound()
    {
        SettingsState located = Reduce(Opened(new SettingsScope.Editor()), new SettingsInput.PresetsLocated(new HashSet<EditorPreset> { EditorPreset.Rider }));

        SettingsUpdate saved = SettingsView.Reduce(Reduce(located, new SettingsInput.PresetChanged(EditorPreset.Rider)), new SettingsInput.Save());

        saved.Effect.ShouldBeOfType<SettingsEffect.Save>().Settings.Editor.ShouldBe(EditorChoice.Of(EditorPreset.Rider));
    }

    [Fact]
    public void BeIdempotentForARepeatedInput()
    {
        SettingsInput input = new SettingsInput.RootsChanged($"{Root}\n{Work}");
        SettingsState once = Reduce(Opened(new SettingsScope.Roots()), input);

        Reduce(once, input).ShouldBe(once);
    }

    [Theory]
    [InlineData("root:1")]
    [InlineData("root:0")]
    public void ScopeEveryRootRowToAllTheRoots(string key)
    {
        ConfigRow row = ConfigRows.For(TwoRoots, SetupFacts.Reading).Single(candidate => candidate.Key == key);

        SettingsScope.For(row).ShouldBe(new SettingsScope.Roots());
    }

    [Fact]
    public void ScopeEachSettingRow()
    {
        IReadOnlyList<ConfigRow> rows = ConfigRows.For(TwoRoots, SetupFacts.Reading);

        SettingsScope.For(rows.OfType<ConfigRow.WorktreeRoot>().Single()).ShouldBe(new SettingsScope.WorktreeRoot());
        SettingsScope.For(rows.OfType<ConfigRow.OpenFolderWith>().Single()).ShouldBe(new SettingsScope.Editor());
        SettingsScope.For(rows.OfType<ConfigRow.DefaultBase>().Single()).ShouldBe(new SettingsScope.Base());
        SettingsScope.For(rows.OfType<ConfigRow.DefaultPlace>().Single()).ShouldBe(new SettingsScope.Place());
    }

    private static SettingsState Reduce(SettingsState state, SettingsInput input)
    {
        SettingsUpdate update = SettingsView.Reduce(state, input);
        update.Effect.ShouldBe(new SettingsEffect.None());

        return update.State;
    }
}