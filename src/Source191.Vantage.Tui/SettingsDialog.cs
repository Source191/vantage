using Source191.Vantage.Application;
using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using GuiKey = Terminal.Gui.Input.Key;

namespace Source191.Vantage.Tui;

/// <summary>
/// A setting's dialog: <see cref="SettingsControls"/> for one <see cref="SettingsScope"/> bound to <see cref="SettingsView"/>, which holds every rule. Each
/// control event becomes a <see cref="SettingsInput"/>; after each reduce the state is pushed back to the controls, and
/// the events that pushing raises are ignored. Nothing is saved here; the screen dispatches the settings it returns.
/// </summary>
internal static class SettingsDialog
{
    /// <summary>The settings to save, or null when the dialog was cancelled.</summary>
    public static Settings? Show(IApplication app, DiagnosticLog log, Settings current, SettingsScope scope, Func<string, bool> folderExists)
    {
        SettingsState state = SettingsState.Open(current, scope, folderExists);
        Settings? result = null;
        bool binding = false;
        SettingsControls controls = new(state);
        using Dialog dialog = new() { Title = Wording.SettingsDialogTitle(scope), Width = Dim.Percent(90), Height = controls.Height };
        dialog.SetScheme(Palette.Focusable(Palette.White));
        DialogTabTitle.Apply(app, log, dialog, DialogName.Settings);
        dialog.Add(controls.All);

        void Apply(SettingsInput input)
        {
            if (binding)
            {
                return;
            }

            SettingsUpdate update = SettingsView.Reduce(state, input);
            state = update.State;
            if (update.Effect is SettingsEffect.Save save)
            {
                result = save.Settings;
            }

            if (update.Effect is not SettingsEffect.None)
            {
                app.RequestStop(dialog);
                return;
            }

            Push();
        }

        void Push()
        {
            binding = true;
            controls.Show(state);
            binding = false;
        }

        // The texts follow their change events; reading them again at Save means no missed event can save a stale value.
        void Save()
        {
            Apply(new SettingsInput.RootsChanged(controls.Roots.Text));
            Apply(new SettingsInput.WorktreeRootChanged(controls.WorktreeRoot.Text));
            Apply(new SettingsInput.ExecutableChanged(controls.Executable.Text));
            Apply(new SettingsInput.ArgumentsChanged(controls.Arguments.Text));
            Apply(new SettingsInput.Save());
        }

        controls.WhenFocused(field => Apply(new SettingsInput.FieldFocused(field)));
        controls.Roots.ContentsChanged += (_, _) => Apply(new SettingsInput.RootsChanged(controls.Roots.Text));
        controls.WorktreeRoot.TextChanging += (_, changing) => Apply(new SettingsInput.WorktreeRootChanged(changing.Result ?? string.Empty));
        controls.Editor.ValueChanged += (_, _) => Apply(new SettingsInput.PresetChanged((EditorPreset)(controls.Editor.Value ?? 0)));
        controls.Executable.TextChanging += (_, changing) => Apply(new SettingsInput.ExecutableChanged(changing.Result ?? string.Empty));
        controls.Arguments.TextChanging += (_, changing) => Apply(new SettingsInput.ArgumentsChanged(changing.Result ?? string.Empty));
        controls.BaseRef.ValueChanged += (_, _) => Apply(new SettingsInput.BaseChanged(controls.BaseRef.Value == 1 ? WorktreeBase.Head : WorktreeBase.RemoteDefault));
        controls.Place.ValueChanged += (_, _) => Apply(new SettingsInput.PlaceChanged((LaunchMode)(controls.Place.Value ?? 0)));
        // Enter in a one-line field does nothing by itself: only Save (F5) saves.
        controls.WorktreeRoot.Accepting += (_, accepting) => accepting.Handled = true;
        controls.Executable.Accepting += (_, accepting) => accepting.Handled = true;
        controls.Arguments.Accepting += (_, accepting) => accepting.Handled = true;
        dialog.KeyDown += (_, key) => key.Handled = key == GuiKey.F5 && WizardControls.Handled(Save);
        dialog.AddButton(WizardControls.Button("_Save (F5)", Save));
        dialog.AddButton(WizardControls.Button("_Cancel (Esc)", () => Apply(new SettingsInput.Cancel())));
        dialog.Initialized += (_, _) => Push();

        app.Run(dialog);

        return result;
    }
}