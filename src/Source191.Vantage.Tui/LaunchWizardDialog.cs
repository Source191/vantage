using Source191.Vantage.Application;
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
    public static WizardResult? Show(IApplication app, RepositoryCatalogue catalogue, IRecentRepositories recentRepositories, IFolders folders, WorktreeBase defaultBase, FolderPath? fixedDirectory = null)
    {
        // Read once, on a pool thread: Terminal.Gui installs a synchronisation context while it runs, so awaiting
        // on the UI thread and then blocking for the result would deadlock (it did, after every attach-and-return).
        IReadOnlyList<string> recent = Task.Run(recentRepositories.ReadRecentRepositoriesAsync).GetAwaiter().GetResult();
        WizardState state = WizardState.Open(catalogue, recent, folders, defaultBase, fixedDirectory);
        WizardResult? result = null;
        bool binding = false;
        IReadOnlyList<string> listed = [];
        WizardControls controls = new(state);
        using Dialog dialog = new() { Title = Wording.WizardTitle(state), Width = Dim.Percent(90), Height = Dim.Percent(90) };
        dialog.SetScheme(Palette.Focusable(Palette.White));
        dialog.Add(controls.All);

        void Apply(WizardInput input)
        {
            if (binding)
            {
                return;
            }

            WizardUpdate update = WizardView.Reduce(state, input);
            state = update.State;
            result = update.Effect is WizardEffect.Launch launch ? new WizardResult(launch.Plan, launch.OpenInTab) : null;
            if (update.Effect is not null)
            {
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
            controls.Problem.Text = state.Problem is null ? string.Empty : Wording.Describe(state.Problem);
            controls.Fields[state.Focus].SetFocus();
            binding = false;
        }

        foreach ((WizardField field, View view) in controls.Fields)
        {
            view.HasFocusChanged += (_, _) =>
            {
                if (view.HasFocus)
                {
                    Apply(new WizardInput.FieldFocused(field));
                }
            };
        }

        controls.Filter.TextChanging += (_, changing) => Apply(new WizardInput.FilterChanged(changing.Result ?? string.Empty));
        controls.Repositories.ValueChanged += (_, _) => Apply(new WizardInput.ChoiceSelected(controls.Repositories.SelectedItem ?? -1));
        controls.Name.TextChanging += (_, changing) => Apply(new WizardInput.NameChanged(changing.Result ?? string.Empty));
        controls.Description.TextChanging += (_, changing) => Apply(new WizardInput.DescriptionChanged(changing.Result ?? string.Empty));
        controls.Mode.ValueChanged += (_, _) => Apply(new WizardInput.ModeChanged(controls.Mode.Value == 1 ? LaunchMode.NewWorktree : LaunchMode.CurrentBranch));
        controls.BaseRef.ValueChanged += (_, _) => Apply(new WizardInput.BaseChanged(controls.BaseRef.Value == 1 ? WorktreeBase.Head : WorktreeBase.RemoteDefault));
        controls.Prompt.ContentsChanged += (_, _) => Apply(new WizardInput.PromptChanged(controls.Prompt.Text));
        // Arrow and page keys typed in the filter drive the list; a letter or Backspace typed in the list is typing in the filter.
        controls.Filter.KeyDown += (_, key) => key.Handled = controls.ListDelta(key) is int delta && Handled(() => Apply(new WizardInput.ListMoved(delta)));
        controls.Repositories.KeyDown += (_, key) => key.Handled = (key == GuiKey.Backspace || (!key.IsAlt && !key.IsCtrl && key.AsRune.Value >= ' ')) && controls.Filter.SetFocus() && controls.Filter.NewKeyDownEvent(key);
        controls.Filter.Accepting += (_, accepting) => accepting.Handled = Handled(() => Apply(new WizardInput.ConfirmRepository()));
        controls.Repositories.Accepting += (_, accepting) => accepting.Handled = Handled(() => Apply(new WizardInput.ConfirmRepository()));
        controls.Name.Accepting += (_, accepting) => accepting.Handled = Handled(() => Apply(new WizardInput.NextField()));
        controls.Description.Accepting += (_, accepting) => accepting.Handled = Handled(() => Apply(new WizardInput.NextField()));
        dialog.KeyDown += (_, key) => key.Handled = (key == GuiKey.F5 || key == GuiKey.F6) && Handled(() => Apply(new WizardInput.Start(OpenInTab: key == GuiKey.F6)));
        // No default button: Enter never starts anything by itself. The function keys and the buttons do.
        dialog.AddButton(WizardControls.Button("_Start (F5)", () => Apply(new WizardInput.Start(OpenInTab: false))));
        dialog.AddButton(WizardControls.Button("Start and open in _tab (F6)", () => Apply(new WizardInput.Start(OpenInTab: true))));
        dialog.AddButton(WizardControls.Button("_Cancel (Esc)", () => Apply(new WizardInput.Cancel())));
        dialog.Initialized += (_, _) => Push();

        app.Run(dialog);

        return result;
    }

    internal static bool Handled(Action action)
    {
        action();

        return true;
    }
}