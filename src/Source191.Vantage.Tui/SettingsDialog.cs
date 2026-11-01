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
/// the events that pushing raises are ignored. Nothing is saved here; the screen dispatches the settings it answers.
/// The Open folder with dialog also looks for the editor presets off the UI thread as it opens and marks those it cannot
/// find once the look answers; a dialog closed by then drops the answer.
/// </summary>
internal static class SettingsDialog
{
    /// <summary>The effect the dialog ended on: <see cref="SettingsEffect.Save"/> with the settings, or <see cref="SettingsEffect.Close"/>.</summary>
    public static SettingsEffect Show(IApplication app, DiagnosticLog log, Settings current, SettingsScope scope, Func<string, bool> folderExists, Func<string, bool> fileExists, EditorPresets presets)
    {
        SettingsState state = SettingsState.Open(current, scope, folderExists, fileExists);
        SettingsEffect result = new SettingsEffect.Close();
        bool binding = false;
        bool open = true;
        SettingsControls controls = new(state);
        using Dialog dialog = new() { Title = Wording.SettingsDialogTitle(scope), Width = Dim.Percent(90), Height = controls.Height };
        dialog.SetScheme(Palette.Focusable(Palette.White));
        DialogTabTitle.Apply(app, log, dialog, DialogName.Settings);
        dialog.Add(controls.All);
        Button browse = new() { Text = Wording.BrowseButton };

        void Apply(SettingsInput input)
        {
            if (binding)
            {
                return;
            }

            SettingsUpdate update = SettingsView.Reduce(state, input);
            state = update.State;
            if (update.Effect is not SettingsEffect.None)
            {
                result = update.Effect;
                app.RequestStop(dialog);
                return;
            }

            Push();
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

        // Which picker F3 opens is read from the state as the key is pressed: the Open folder with dialog browses only while Custom is chosen.
        void Browse()
        {
            Apply(new SettingsInput.RootsChanged(controls.Roots.Text));
            Apply(new SettingsInput.WorktreeRootChanged(controls.WorktreeRoot.Text));
            Apply(new SettingsInput.ExecutableChanged(controls.Executable.Text));
            switch (state.Browses)
            {
                case SettingsBrowse.OneFolder or SettingsBrowse.SeveralFolders:
                    BrowseFolders();
                    break;
                case SettingsBrowse.OneFile:
                    BrowseFile();
                    break;
                case SettingsBrowse.No:
                    break;
            }
        }

        // The picker answers folders; the reducer adds them as lines and F5 checks them with the typed ones.
        void BrowseFolders()
        {
            using OpenDialog picker = new() { Title = Wording.FolderPickerTitle(scope), OpenMode = OpenMode.Directory, AllowsMultipleSelection = state.Browses == SettingsBrowse.SeveralFolders };
            Run(picker, DialogName.FolderPicker);
            Apply(new SettingsInput.FoldersChosen(picker.Canceled ? [] : picker.FilePaths));
            binding = true;
            controls.ShowFolders(state);
            binding = false;
        }

        // The picker shows folders and programs only; the file chosen replaces the executable, and F5 checks it as if typed.
        void BrowseFile()
        {
            using OpenDialog picker = new() { Title = Wording.FilePickerTitle, OpenMode = OpenMode.File, AllowedTypes = [new AllowedType(Wording.ProgramFiles, ".exe")] };
            Run(picker, DialogName.FilePicker);
            Apply(new SettingsInput.FileChosen(picker is { Canceled: false, FilePaths: [string chosen, ..] } ? chosen : string.Empty));
            binding = true;
            controls.Executable.Text = state.Text.Executable;
            binding = false;
        }

        void Run(OpenDialog picker, DialogName name)
        {
            if (state.BrowseFrom is BrowseStart.At from)
            {
                picker.Path = from.Folder.Value;
            }

            picker.SetScheme(Palette.Focusable(Palette.White));
            DialogTabTitle.Apply(app, log, picker, name);
            app.Run(picker);
        }

        void Push()
        {
            binding = true;
            controls.Show(state);
            browse.Visible = state.Browses != SettingsBrowse.No;
            binding = false;
        }

        // Started off the UI thread and never awaited; the answer is applied on the UI thread only while the dialog is open.
        async Task LookForPresetsAsync()
        {
            IReadOnlySet<EditorPreset> missing = await Attempt.RunAsync(
                log,
                FailureSite.BackgroundTask,
                CodeName.For(typeof(EditorPresets)),
                () => Task.Run(() => presets.MissingAsync(CancellationToken.None)),
                _ => new HashSet<EditorPreset>()).ConfigureAwait(false);
            app.Invoke(() =>
            {
                if (open)
                {
                    Apply(new SettingsInput.PresetsLocated(missing));
                }
            });
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
        dialog.KeyDown += (_, key) => key.Handled = (key == GuiKey.F5 && WizardControls.Handled(Save)) || (key == GuiKey.F3 && state.Browses != SettingsBrowse.No && WizardControls.Handled(Browse));
        if (scope is SettingsScope.Roots or SettingsScope.WorktreeRoot or SettingsScope.Editor)
        {
            dialog.AddButton(browse);
        }

        dialog.AddButton(WizardControls.Button("_Save (F5)", Save));
        dialog.AddButton(WizardControls.Button("_Cancel (Esc)", () => Apply(new SettingsInput.Cancel())));
        dialog.Initialized += (_, _) => Push();
        if (scope is SettingsScope.Editor)
        {
            _ = LookForPresetsAsync();
        }

        app.Run(dialog);
        open = false;

        return result;
    }
}
