using Source191.Vantage.Domain;
using System.Diagnostics;

namespace Source191.Vantage.Application.Views;

/// <summary>
/// The settings dialog's rules as a pure function of state and input; the dialog only binds controls to it. Every
/// change is held as typed; Save checks the whole form with <see cref="Settings.Validate"/> and either yields the
/// settings to save or shows the problem with focus in the field it is about.
/// </summary>
public static class SettingsView
{
    public static SettingsUpdate Reduce(SettingsState state, SettingsInput input)
    {
        return input switch
        {
            SettingsInput.RootsChanged changed => Changed(state with { Text = state.Text with { Roots = changed.Text } }),
            SettingsInput.WorktreeRootChanged changed => Changed(state with { Text = state.Text with { WorktreeRoot = changed.Text } }),
            SettingsInput.PresetChanged changed => Changed(state with { Text = state.Text with { Preset = changed.Preset } }),
            SettingsInput.ExecutableChanged changed => Changed(state with { Text = state.Text with { Executable = changed.Text } }),
            SettingsInput.ArgumentsChanged changed => Changed(state with { Text = state.Text with { Arguments = changed.Text } }),
            SettingsInput.BaseChanged changed => Changed(state with { Text = state.Text with { Base = changed.Base } }),
            SettingsInput.CheckForUpdatesChanged changed => Changed(state with { Text = state.Text with { CheckForUpdates = changed.Checked } }),
            SettingsInput.FieldFocused focused => Changed(state with { Focus = focused.Field }),
            SettingsInput.Save => Saved(state),
            SettingsInput.Cancel => new SettingsUpdate(state, new SettingsEffect.Close()),
            _ => Changed(state),
        };
    }

    private static SettingsUpdate Saved(SettingsState state)
    {
        return Settings.Validate(state.Text, state.FolderExists) switch
        {
            SettingsValidation.Valid valid => new SettingsUpdate(state with { Problem = null }, new SettingsEffect.Save(valid.Settings)),
            SettingsValidation.Refused refused => Changed(state with { Problem = refused.Problem, Focus = FieldOf(refused.Problem) }),
            _ => throw new UnreachableException(),
        };
    }

    /// <summary>The field a problem is about, so focus goes where the fix is typed.</summary>
    private static SettingsField FieldOf(SettingsProblem problem)
    {
        return problem switch
        {
            SettingsProblem.NoRoots or SettingsProblem.RootNotFullPath or SettingsProblem.RootMissing => SettingsField.Roots,
            SettingsProblem.WorktreeRootNotFullPath => SettingsField.WorktreeRoot,
            SettingsProblem.NoExecutable or SettingsProblem.BatchFileExecutable => SettingsField.Executable,
            SettingsProblem.NoFolderPlaceholder => SettingsField.Arguments,
            _ => throw new UnreachableException(),
        };
    }

    private static SettingsUpdate Changed(SettingsState state) => new SettingsUpdate(state, new SettingsEffect.None());
}