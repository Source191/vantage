using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;

namespace Source191.Vantage.Tui;

/// <summary>
/// What one table view adds to the shared screen: its layout text, its columns and colours, and its reducer.
/// Everything else (lifetime, refresh, keys, routing effects, background work, spinners, redraw) is
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

    TState WithFooter(TState state, Footer footer);

    (TState State, IReadOnlyList<ViewEffect> Effects) Reduce(TState state, ViewInput input);

    /// <summary>Sees every input before the state is replaced, for the notifier.</summary>
    void OnInput(TState before, ViewInput input);
}

/// <summary>What a dialog flow may do on the UI thread of the screen that raised it.</summary>
internal interface IEffectContext
{
    IApplication App { get; }

    /// <summary>The screen's key help, for the help dialog.</summary>
    string HelpText { get; }

    void ShowFooter(Footer footer);

    /// <summary>Feeds a follow-up input (a dialog's answer) back through the reducer.</summary>
    void Reduce(ViewInput input);

    /// <summary>Runs the effect's handler off the UI thread with a busy spinner; its answer becomes the footer.</summary>
    void Dispatch(ViewEffect effect);

    /// <summary>As above, with a continuation on the UI thread that turns the handler's answer into the footer.</summary>
    void Dispatch(ViewEffect effect, Func<Outcome, Footer> onUiThread);

    /// <summary>Runs work that is not a dispatched effect (its outcome needs a question) with a continuation on the UI thread.</summary>
    void RunInBackground<T>(Footer working, Func<CancellationToken, Task<T>> action, Func<T, Footer> onUiThread);
}