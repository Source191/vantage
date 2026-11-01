using Source191.Vantage.Application;
using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Application.Launch;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using System.Collections.ObjectModel;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using GuiKey = Terminal.Gui.Input.Key;

namespace Source191.Vantage.Tui;

/// <summary>
/// The new-session dialog: <see cref="WizardControls"/> bound to <see cref="WizardView"/>, which holds every rule. Each
/// control event becomes a <see cref="WizardInput"/>; after each reduce the state is pushed back to the controls, and the
/// events that pushing raises are ignored. Nothing is started here; the screen runs the plan so the dialog closes at once.
/// </summary>
internal static class LaunchWizardDialog
{
    /// <summary>The effect the wizard ended on: <see cref="WizardEffect.Launch"/> with the plan, or <see cref="WizardEffect.Close"/>.</summary>
    public static WizardEffect Show(IApplication app, DiagnosticLog log, RepositoryCatalogue catalogue, IRecentRepositories recentRepositories, IFolders folders, Settings settings, FolderPath? fixedDirectory = null)
    {
        // Read once, on a pool thread: Terminal.Gui installs a synchronisation context while it runs, so awaiting
        // on the UI thread and then blocking for the result would deadlock (it did, after every attach-and-return).
        IReadOnlyList<string> recent = Task.Run(recentRepositories.ReadRecentRepositoriesAsync).GetAwaiter().GetResult();
        WizardState state = WizardState.Open(catalogue, recent, folders, settings, fixedDirectory is null ? new WizardOrigin.Anywhere() : WizardOrigin.In(fixedDirectory));
        WizardEffect result = new WizardEffect.Close();
        bool binding = false;
        IReadOnlyList<string> listed = [];
        WizardControls controls = new(state);
        // One 76-cell column, as tall as the screen allows so the repository list gets the rows; a fixed folder's form is
        // the directory, the name, a two-line preview and the prompt, so its dialog is sized to them.
        using Dialog dialog = state.ShowsChooser
            ? new() { Title = Wording.WizardTitle(state), Width = 80, Height = Dim.Percent(90) }
            : new() { Title = Wording.WizardTitle(state), Width = 84, Height = 23 };
        dialog.SetScheme(Palette.Focusable(Palette.White));
        DialogTabTitle.Apply(app, log, dialog, DialogName.NewSession);
        dialog.Add(controls.All);

        void Apply(WizardInput input)
        {
            if (binding)
            {
                return;
            }

            WizardUpdate update = WizardView.Reduce(state, input);
            state = update.State;
            if (update.Effect is not WizardEffect.None)
            {
                result = update.Effect;
                app.RequestStop(dialog);
                return;
            }

            Push();
        }

        void Push()
        {
            binding = true;
            IReadOnlyList<string> names = [.. state.Choices.Select(choice => choice.Name)];
            if (!names.SequenceEqual(listed))
            {
                listed = names;
                controls.Repositories.SetSource(new ObservableCollection<string>(names));
            }

            controls.Repositories.SelectedItem = state.SelectedIndex;
            controls.Chosen.Text = Wording.WizardChosen(state);
            controls.Preview.Text = state.Preview;
            controls.Problem.Text = Wording.Describe(state.Problem);
            controls.Fields[state.Focus].SetFocus();
            binding = false;
        }

        // The texts follow their change events; reading them again at Start, as the dialog always did, means no missed event can launch a stale name.
        void Start()
        {
            Apply(new WizardInput.NameChanged(controls.Name.Text));
            Apply(new WizardInput.PromptChanged(controls.Prompt.Text));
            Apply(new WizardInput.Start());
        }

        controls.WhenFocused(field => Apply(new WizardInput.FieldFocused(field)));
        controls.Filter.TextChanging += (_, changing) => Apply(new WizardInput.FilterChanged(changing.Result ?? string.Empty));
        controls.Repositories.ValueChanged += (_, _) => Apply(new WizardInput.ChoiceSelected(controls.Repositories.SelectedItem ?? -1));
        controls.Name.TextChanging += (_, changing) => Apply(new WizardInput.NameChanged(changing.Result ?? string.Empty));
        controls.Mode.ValueChanged += (_, _) => Apply(new WizardInput.ModeChanged(controls.Mode.Value == 1 ? LaunchMode.NewWorktree : LaunchMode.CurrentBranch));
        controls.BaseRef.ValueChanged += (_, _) => Apply(new WizardInput.BaseChanged(controls.BaseRef.Value == 1 ? WorktreeBase.Head : WorktreeBase.RemoteDefault));
        controls.Prompt.ContentsChanged += (_, _) => Apply(new WizardInput.PromptChanged(controls.Prompt.Text));
        // Arrow and page keys typed in the filter drive the list; a letter or Backspace typed in the list is typing in the filter.
        controls.Filter.KeyDown += (_, key) => key.Handled = controls.ListDelta(key) is int delta && WizardControls.Handled(() => Apply(new WizardInput.ListMoved(delta)));
        controls.Repositories.KeyDown += (_, key) => key.Handled = (key == GuiKey.Backspace || (!key.IsAlt && !key.IsCtrl && key.AsRune.Value >= ' ')) && controls.Filter.SetFocus() && controls.Filter.NewKeyDownEvent(key);
        controls.Filter.Accepting += (_, accepting) => accepting.Handled = WizardControls.Handled(() => Apply(new WizardInput.ConfirmRepository()));
        controls.Repositories.Accepting += (_, accepting) => accepting.Handled = WizardControls.Handled(() => Apply(new WizardInput.ConfirmRepository()));
        // Enter in the name moves on, or for a fixed folder starts the session in a tab, so the texts are read again first as Start does.
        controls.Name.Accepting += (_, accepting) => accepting.Handled = WizardControls.Handled(() =>
        {
            Apply(new WizardInput.NameChanged(controls.Name.Text));
            Apply(new WizardInput.PromptChanged(controls.Prompt.Text));
            Apply(new WizardInput.NextField());
        });
        // Enter on a selector's option would also choose that option; it only moves on, and the arrows change the choice.
        // The focused option sees the key before the selector does, so the options are where it is caught.
        controls.WhenEnterInSelectors(() => Apply(new WizardInput.NextField()));
        // An Enter no control took would otherwise close the dialog with nothing started.
        dialog.Accepting += (_, accepting) => accepting.Handled = true;
        dialog.KeyDown += (_, key) => key.Handled = key == GuiKey.F5 && WizardControls.Handled(Start);
        // No default button: Enter never starts anything by itself. F5 and the Start button do, and both open the session's tab.
        dialog.AddButton(WizardControls.Button("_Start (F5)", Start));
        dialog.AddButton(WizardControls.Button("_Cancel (Esc)", () => Apply(new WizardInput.Cancel())));
        dialog.Initialized += (_, _) => Push();

        app.Run(dialog);

        return result;
    }
}