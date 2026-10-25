using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application.Views;

/// <summary>The settings dialog's rules: what it shows, how it follows its controls, and when it saves.</summary>
public sealed class SettingsViewShould
{
    private const string Root = @"C:\Users\me\source\repos";
    private const string Work = @"D:\work";

    private static readonly Func<string, bool> Exists = path => path is Root or Work;

    private static SettingsState Opened => SettingsState.Open(TestSettings.For(Root), Exists);

    [Fact]
    public void OpenWithTheCurrentSettings()
    {
        SettingsState opened = SettingsState.Open(TestSettings.For([Root, Work], @"D:\worktrees", WorktreeBase.Head), Exists);

        opened.Text.ShouldBe(new SettingsText($"{Root}\n{Work}", @"D:\worktrees", EditorPreset.VsCode, string.Empty, string.Empty, WorktreeBase.Head));
        opened.Problem.ShouldBeNull();
        opened.Focus.ShouldBe(SettingsField.Roots);
    }

    [Fact]
    public void FollowEachControlChange()
    {
        SettingsState state = Opened;

        state = Reduce(state, new SettingsInput.RootsChanged($"{Root}\n{Work}"));
        state = Reduce(state, new SettingsInput.WorktreeRootChanged(@"D:\worktrees"));
        state = Reduce(state, new SettingsInput.PresetChanged(EditorPreset.Custom));
        state = Reduce(state, new SettingsInput.ExecutableChanged(@"C:\Tools\subl.exe"));
        state = Reduce(state, new SettingsInput.ArgumentsChanged("-n {folder}"));
        state = Reduce(state, new SettingsInput.BaseChanged(WorktreeBase.Head));
        state = Reduce(state, new SettingsInput.FieldFocused(SettingsField.Base));

        state.Text.ShouldBe(new SettingsText($"{Root}\n{Work}", @"D:\worktrees", EditorPreset.Custom, @"C:\Tools\subl.exe", "-n {folder}", WorktreeBase.Head));
        state.Focus.ShouldBe(SettingsField.Base);
    }

    [Fact]
    public void TurnTheUpdateCheckOffAndSaveIt()
    {
        SettingsState state = Reduce(Opened, new SettingsInput.CheckForUpdatesChanged(false));

        state.Text.CheckForUpdates.ShouldBeFalse();
        SettingsUpdate saved = SettingsView.Reduce(state, new SettingsInput.Save());
        saved.Effect.ShouldBeOfType<SettingsEffect.Save>().Settings.CheckForUpdates.ShouldBeFalse();
    }

    public static TheoryData<SettingsInput[], SettingsProblem, SettingsField> Problems => new()
    {
        { [new SettingsInput.RootsChanged(" ")], new SettingsProblem.NoRoots(), SettingsField.Roots },
        { [new SettingsInput.RootsChanged("repos")], new SettingsProblem.RootNotFullPath("repos"), SettingsField.Roots },
        { [new SettingsInput.RootsChanged(@"D:\nowhere")], new SettingsProblem.RootMissing(new FolderPath(@"D:\nowhere")), SettingsField.Roots },
        { [new SettingsInput.WorktreeRootChanged("worktrees")], new SettingsProblem.WorktreeRootNotFullPath(), SettingsField.WorktreeRoot },
        { [new SettingsInput.PresetChanged(EditorPreset.Custom), new SettingsInput.ArgumentsChanged("{folder}")], new SettingsProblem.NoExecutable(), SettingsField.Executable },
        { [new SettingsInput.PresetChanged(EditorPreset.Custom), new SettingsInput.ExecutableChanged(@"C:\Tools\edit.cmd"), new SettingsInput.ArgumentsChanged("{folder}")], new SettingsProblem.BatchFileExecutable(), SettingsField.Executable },
        { [new SettingsInput.PresetChanged(EditorPreset.Custom), new SettingsInput.ExecutableChanged(@"C:\Tools\subl.exe"), new SettingsInput.ArgumentsChanged("-n")], new SettingsProblem.NoFolderPlaceholder(), SettingsField.Arguments },
    };

    [Theory]
    [MemberData(nameof(Problems))]
    public void RefuseToSaveWithTheProblemAndItsField(SettingsInput[] inputs, SettingsProblem problem, SettingsField field)
    {
        SettingsState state = inputs.Aggregate(Opened, Reduce);

        SettingsUpdate saved = SettingsView.Reduce(state, new SettingsInput.Save());

        saved.Effect.ShouldBe(new SettingsEffect.None());
        saved.State.Problem.ShouldBe(problem);
        saved.State.Focus.ShouldBe(field);
    }

    [Fact]
    public void SaveYieldsSaveSettings()
    {
        SettingsState state = Reduce(Reduce(Opened, new SettingsInput.RootsChanged($"{Root}\n{Work}")), new SettingsInput.WorktreeRootChanged(@"D:\worktrees"));

        SettingsUpdate saved = SettingsView.Reduce(state, new SettingsInput.Save());

        saved.Effect.ShouldBe(new SettingsEffect.Save(TestSettings.For([Root, Work], @"D:\worktrees")));
    }

    [Fact]
    public void ClearTheProblemOnceTheSettingsAreValid()
    {
        SettingsState refused = SettingsView.Reduce(Reduce(Opened, new SettingsInput.RootsChanged("repos")), new SettingsInput.Save()).State;

        SettingsUpdate saved = SettingsView.Reduce(Reduce(refused, new SettingsInput.RootsChanged(Root)), new SettingsInput.Save());

        saved.State.Problem.ShouldBeNull();
        saved.Effect.ShouldBeOfType<SettingsEffect.Save>();
    }

    [Fact]
    public void CancelYieldsClose()
    {
        SettingsView.Reduce(Reduce(Opened, new SettingsInput.WorktreeRootChanged(@"D:\worktrees")), new SettingsInput.Cancel()).Effect.ShouldBe(new SettingsEffect.Close());
    }

    [Fact]
    public void ShowTheCustomEditorFieldsOnlyForCustom()
    {
        Opened.ShowsCustomEditor.ShouldBeFalse();
        Reduce(Opened, new SettingsInput.PresetChanged(EditorPreset.Custom)).ShowsCustomEditor.ShouldBeTrue();
        Reduce(Opened, new SettingsInput.PresetChanged(EditorPreset.Explorer)).ShowsCustomEditor.ShouldBeFalse();
    }

    [Fact]
    public void BeIdempotentForARepeatedInput()
    {
        SettingsInput input = new SettingsInput.RootsChanged($"{Root}\n{Work}");
        SettingsState once = Reduce(Opened, input);

        Reduce(once, input).ShouldBe(once);
    }

    private static SettingsState Reduce(SettingsState state, SettingsInput input)
    {
        SettingsUpdate update = SettingsView.Reduce(state, input);
        update.Effect.ShouldBe(new SettingsEffect.None());

        return update.State;
    }
}