using Source191.Vantage.Application.Views;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;

namespace Source191.Vantage.Tui;

/// <summary>
/// What one table view adds to the shared screen: its layout text, its columns and colours, its reducer, and how its
/// own effects are carried out. Everything else (lifetime, refresh, keys, background work, spinners, redraw) is
/// <see cref="TableScreen{TState, TRow}"/>'s and written once.
/// </summary>
internal interface IScreenDefinition<TState, TRow>
    where TState : IListViewState<TRow>
{
    TimeSpan RefreshEvery { get; }

    /// <summary>The footer crumb, with its surrounding spaces, such as " &lt;sessions&gt; ".</summary>
    string Crumb { get; }

    /// <summary>Shown with a spinner over the empty table until the first load arrives.</summary>
    string WaitingText { get; }

    /// <summary>The words before the banner's failure message, such as "listing failed".</summary>
    string LoadFailurePrefix { get; }

    /// <summary>The five header row names, newline separated; the first three are machine, Claude and roots.</summary>
    string HeaderNames { get; }

    string Keys { get; }

    string KeyNames { get; }

    string Keys2 { get; }

    string KeyNames2 { get; }

    string HelpText { get; }

    IReadOnlyList<string> ColumnNames { get; }

    string Cell(TRow row, int column, DateTimeOffset now);

    Color RowColour(TRow row);

    /// <summary>The fourth and fifth header lines, newline separated: counts drawn from the state.</summary>
    string ContextLines(TState state);

    string FooterText(TState state);

    TState WithFooter(TState state, string footer);

    (TState State, IReadOnlyList<ViewEffect> Effects) Reduce(TState state, ViewInput input);

    /// <summary>Sees every input before the state is replaced, for the notifier.</summary>
    void OnInput(TState before, ViewInput input);

    /// <summary>Effects that stop this screen so the loop can run them or switch screens.</summary>
    bool LeavesScreen(ViewEffect effect);

    /// <summary>Carries out an in-screen effect; help is handled by the screen itself.</summary>
    void Handle(IEffectContext<TState> context, ViewEffect effect);
}

/// <summary>What a definition may do while handling an effect, on the UI thread.</summary>
internal interface IEffectContext<TState>
{
    IApplication App { get; }

    TState State { get; set; }

    /// <summary>Runs the action off the UI thread with a busy spinner; its text becomes the footer and a refresh follows.</summary>
    void RunInBackground(string working, Func<CancellationToken, Task<string>> action);

    /// <summary>As above, with a continuation on the UI thread that turns the result into the footer (and may ask a question first).</summary>
    void RunInBackground<T>(string working, Func<CancellationToken, Task<T>> action, Func<T, string> onUiThread);

    /// <summary>Feeds a follow-up input (a dialog's answer) back through the reducer.</summary>
    void Reduce(ViewInput input);
}