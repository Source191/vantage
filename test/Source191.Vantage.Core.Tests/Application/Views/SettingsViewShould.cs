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

    private static readonly Settings TwoRoots = TestSettings.For([Root, Work], @"D:\worktrees", WorktreeBase.Head);

    private static SettingsState Opened(SettingsScope scope) => SettingsState.Open(TestSettings.For(Root), scope, Exists);

    public static TheoryData<SettingsScope, SettingsField> Scopes => new()
    {
        { new SettingsScope.Roots(), SettingsField.Roots },
        { new SettingsScope.WorktreeRoot(), SettingsField.WorktreeRoot },
        { new SettingsScope.Editor(), SettingsField.Editor },
        { new SettingsScope.Base(), SettingsField.Base },
    };

    [Theory]
    [MemberData(nameof(Scopes))]
    public void OpenOnTheOneValueWithItsField(SettingsScope scope, SettingsField field)
    {
        SettingsState opened = SettingsState.Open(TwoRoots, scope, Exists);

        opened.Scope.ShouldBe(scope);
        opened.Focus.ShouldBe(field);
        opened.Problem.ShouldBeNull();
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
    };

    [Theory]
    [MemberData(nameof(Problems))]
    public void RefuseToSaveWithTheProblemAndItsField(SettingsScope scope, SettingsInput[] inputs, SettingsProblem problem, SettingsField field)
    {
        SettingsState state = inputs.Aggregate(Opened(scope), Reduce);

        SettingsUpdate saved = SettingsView.Reduce(state, new SettingsInput.Save());

        saved.Effect.ShouldBe(new SettingsEffect.None());
        saved.State.Problem.ShouldBe(problem);
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
        SettingsState state = Reduce(SettingsState.Open(TwoRoots, new SettingsScope.WorktreeRoot(), Exists), new SettingsInput.WorktreeRootChanged(@"E:\trees"));

        SettingsView.Reduce(state, new SettingsInput.Save()).Effect.ShouldBe(new SettingsEffect.Save(TestSettings.For([Root, Work], @"E:\trees", WorktreeBase.Head)));
    }

    [Fact]
    public void SaveTheBase()
    {
        SettingsState state = Reduce(Opened(new SettingsScope.Base()), new SettingsInput.BaseChanged(WorktreeBase.Head));

        SettingsView.Reduce(state, new SettingsInput.Save()).Effect.ShouldBeOfType<SettingsEffect.Save>().Settings.DefaultBase.ShouldBe(WorktreeBase.Head);
    }

    [Fact]
    public void ClearTheProblemOnceTheValueIsValid()
    {
        SettingsState refused = SettingsView.Reduce(Reduce(Opened(new SettingsScope.Roots()), new SettingsInput.RootsChanged("repos")), new SettingsInput.Save()).State;

        SettingsUpdate saved = SettingsView.Reduce(Reduce(refused, new SettingsInput.RootsChanged(Root)), new SettingsInput.Save());

        saved.State.Problem.ShouldBeNull();
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
        ConfigRow row = ConfigRows.For(TwoRoots, facts: null).Single(candidate => candidate.Key == key);

        SettingsScope.For(row).ShouldBe(new SettingsScope.Roots());
    }

    [Fact]
    public void ScopeEachSettingRow()
    {
        IReadOnlyList<ConfigRow> rows = ConfigRows.For(TwoRoots, facts: null);

        SettingsScope.For(rows.OfType<ConfigRow.WorktreeRoot>().Single()).ShouldBe(new SettingsScope.WorktreeRoot());
        SettingsScope.For(rows.OfType<ConfigRow.OpenFolderWith>().Single()).ShouldBe(new SettingsScope.Editor());
        SettingsScope.For(rows.OfType<ConfigRow.DefaultBase>().Single()).ShouldBe(new SettingsScope.Base());
    }

    private static SettingsState Reduce(SettingsState state, SettingsInput input)
    {
        SettingsUpdate update = SettingsView.Reduce(state, input);
        update.Effect.ShouldBe(new SettingsEffect.None());

        return update.State;
    }
}