using Source191.Vantage.Application.Launch;
using System.Diagnostics;

namespace Source191.Vantage.Application.Views;

/// <summary>
/// The new-session wizard's rules as a pure function of state and input; the dialog only binds controls to it.
/// Keyboard model: focus starts in the repository filter; typing narrows the list, the arrow keys move into the list,
/// and Enter confirms the repository and moves to the name. Enter in the other one-line fields moves on; only Start
/// (F5, or F6 to open a tab too) launches, so a stray Enter never starts a half-filled session.
/// </summary>
public static class WizardView
{
    public static WizardUpdate Reduce(WizardState state, WizardInput input)
    {
        return input switch
        {
            WizardInput.FilterChanged changed => Changed(Filtered(state, changed.Text)),
            WizardInput.ListMoved moved => Changed(Moved(state, moved.Delta)),
            WizardInput.ChoiceSelected selected => Changed(Selected(state, selected.Index)),
            WizardInput.NameChanged changed => Changed(state with { Name = changed.Text }),
            WizardInput.ModeChanged changed => Changed(state with { Mode = changed.Mode }),
            WizardInput.BaseChanged changed => Changed(state with { Base = changed.Base }),
            WizardInput.PromptChanged changed => Changed(state with { Prompt = changed.Text }),
            WizardInput.FieldFocused focused => Changed(state with { Focus = focused.Field }),
            WizardInput.ConfirmRepository => Changed(Confirmed(state)),
            WizardInput.NextField => Changed(state with { Focus = Next(state) }),
            WizardInput.Start start => Started(state, start.OpenInTab),
            WizardInput.Cancel => new WizardUpdate(state, new WizardEffect.Close()),
            _ => Changed(state),
        };
    }

    /// <summary>Typing in the filter (or a letter typed in the list) puts focus in the filter and selects the first match; the same text again only puts focus in the filter and keeps the selection.</summary>
    private static WizardState Filtered(WizardState state, string text)
    {
        if (text == state.Filter)
        {
            return state with { Focus = WizardField.RepositoryFilter };
        }

        WizardState filtered = state with { Filter = text, Focus = WizardField.RepositoryFilter };

        return filtered with { SelectedIndex = filtered.Choices.Count > 0 ? 0 : null };
    }

    /// <summary>An arrow or page key in the filter moves focus into the list, so its highlight shows, and moves the selection within bounds.</summary>
    private static WizardState Moved(WizardState state, int delta)
    {
        int count = state.Choices.Count;
        int? index = state.SelectedIndex is int current && count > 0 ? Math.Clamp(current + delta, 0, count - 1) : state.SelectedIndex;

        return state with { SelectedIndex = index, Focus = WizardField.RepositoryList };
    }

    private static WizardState Selected(WizardState state, int index) => index >= 0 && index < state.Choices.Count ? state with { SelectedIndex = index } : state;

    private static WizardState Confirmed(WizardState state)
    {
        return state.Chosen is null
            ? state with { Problem = new WizardProblem.NoRepository(), Focus = WizardField.RepositoryFilter }
            : state with { Problem = new WizardProblem.None(), Focus = WizardField.Name };
    }

    /// <summary>Enter moves through the form in its order: the name to the Where choice when it shows, Where to the base, the base to the prompt; the prompt keeps it.</summary>
    private static WizardField Next(WizardState state)
    {
        return state.Focus switch
        {
            WizardField.Name => state.ShowsChooser ? WizardField.Mode : WizardField.Name,
            WizardField.RepositoryFilter => WizardField.RepositoryFilter,
            WizardField.RepositoryList => WizardField.RepositoryList,
            WizardField.Mode => WizardField.Base,
            WizardField.Base => WizardField.Prompt,
            WizardField.Prompt => WizardField.Prompt,
        };
    }

    private static WizardUpdate Started(WizardState state, bool openInTab)
    {
        if (state.Chosen is not RepositoryChoice repository)
        {
            return Changed(state with { Problem = new WizardProblem.NoRepository(), Focus = WizardField.RepositoryFilter });
        }

        LaunchRequest request = new(repository, state.Name, state.Mode, state.Base, state.Prompt);

        return LaunchPlanBuilder.Build(request, state.WorktreeRoot, state.Folders.DirectoryExists) switch
        {
            LaunchPlanResult.Planned planned => new WizardUpdate(state, new WizardEffect.Launch(planned.Plan, openInTab)),
            LaunchPlanResult.Refused refused => Changed(state with { Problem = new WizardProblem.Refused(refused.Problem), Focus = WizardField.Name }),
            _ => throw new UnreachableException(),
        };
    }

    private static WizardUpdate Changed(WizardState state) => new WizardUpdate(state, new WizardEffect.None());
}