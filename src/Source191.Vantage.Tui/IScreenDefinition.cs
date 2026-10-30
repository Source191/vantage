using Source191.Vantage.Application;
using Source191.Vantage.Application.Diagnostics;
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

    /// <summary>The six header row names, newline separated; the first four are Vantage, Claude, the roots and the worktree root.</summary>
    string HeaderNames { get; }

    /// <summary>The keys the view answers to; the header draws a hint for each that has one and shows its line as a tooltip.</summary>
    IReadOnlyList<KeyHelp> Keys { get; }

    string HelpText { get; }

    /// <summary>The table's columns, in cell order: their names head the table and their meanings are the tooltips and the help.</summary>
    IReadOnlyList<ColumnHelp> Columns { get; }

    /// <summary>The column that takes the width the others leave (WHERE, WORKTREE), or null when every column has its natural width.</summary>
    int? ElasticColumn { get; }

    /// <summary>A cell's text; <paramref name="width"/> is the cells the elastic column has, and int.MaxValue for every other column.</summary>
    string Cell(TRow row, int column, DateTimeOffset now, int width);

    /// <summary>A colour for one cell that differs from its row's (the TAB swatch), or null for the row's colour.</summary>
    Color? CellColour(TRow row, int column);

    Color RowColour(TRow row);

    /// <summary>The fifth and sixth header lines, newline separated: counts drawn from the state.</summary>
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

    /// <summary>The diagnostic log, which every dialog records itself in by name when it opens.</summary>
    DiagnosticLog Diagnostics { get; }

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