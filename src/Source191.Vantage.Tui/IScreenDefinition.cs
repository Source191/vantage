using Source191.Vantage.Application;
using Source191.Vantage.Application.Detail;
using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;

namespace Source191.Vantage.Tui;

/// <summary>
/// What one table view adds to the shared screen: its layout text, its columns and colours, and its reducer.
/// Everything else (lifetime, refresh, keys, routing effects, background work, spinners, redraw) is
/// <see cref="TableBody{TState, TRow}"/>'s and written once.
/// </summary>
internal interface IScreenDefinition<TState, TRow>
    where TState : IListViewState<TRow>
{
    TimeSpan RefreshEvery { get; }

    /// <summary>Shown with a spinner over the empty table until the first load arrives.</summary>
    string WaitingText { get; }

    /// <summary>The words before the banner's failure message, such as "listing failed".</summary>
    string LoadFailurePrefix { get; }

    string HelpText { get; }

    /// <summary>The table's columns, in cell order: their names head the table and their meanings are the tooltips and the help.</summary>
    IReadOnlyList<ColumnHelp> Columns { get; }

    /// <summary>The column that takes the width the others leave (REPOSITORY, WORKTREE), or null when every column has its natural width.</summary>
    int? ElasticColumn { get; }

    /// <summary>A cell's text; <paramref name="width"/> is the cells the elastic column has, and int.MaxValue for every other column.</summary>
    string Cell(TRow row, int column, DateTimeOffset now, int width);

    /// <summary>One cell's own paint (the TAB swatch), or null for the row's colour.</summary>
    CellPaint? CellPaint(TRow row, int column);

    Color RowColour(TRow row);

    /// <summary>The tab's title, such as "Sessions (6)".</summary>
    string TabTitle(TState state);

    /// <summary>The counts beside the tab's title.</summary>
    IReadOnlyList<TextPart> Counts(TState state);

    /// <summary>The key bar for the selected row and the listed rows; Update shows when a release is known.</summary>
    KeyBar Bar(TState state, bool updateKnown);

    TState WithMessage(TState state, StatusMessage message);

    (TState State, IReadOnlyList<ViewEffect> Effects) Reduce(TState state, ViewInput input);

    /// <summary>Sees every input before the state is replaced, for the notifier.</summary>
    void OnInput(TState before, ViewInput input);

    /// <summary>What a row is, shown as a tooltip while the pointer rests on it, or null for a view whose rows need none.</summary>
    string? RowTip(TRow row);

    /// <summary>How the pane under the table reads the selected row, or null for a view with no pane.</summary>
    IDetailSource<TRow>? Detail { get; }
}

/// <summary>What a dialog flow may do on the UI thread of the screen that raised it.</summary>
internal interface IEffectContext
{
    IApplication App { get; }

    /// <summary>The diagnostic log, which every dialog records itself in by name when it opens.</summary>
    DiagnosticLog Diagnostics { get; }

    /// <summary>The screen's key help, for the help dialog.</summary>
    string HelpText { get; }

    void ShowMessage(StatusMessage message);

    /// <summary>Feeds a follow-up input (a dialog's answer) back through the reducer.</summary>
    void Reduce(ViewInput input);

    /// <summary>Runs the effect's handler off the UI thread with a busy spinner; its answer becomes the footer.</summary>
    void Dispatch(ViewEffect effect);

    /// <summary>As above, with a continuation on the UI thread that turns the handler's answer into the footer.</summary>
    void Dispatch(ViewEffect effect, Func<Outcome, StatusMessage> onUiThread);

    /// <summary>Runs work that is not a dispatched effect (its outcome needs a question) with a continuation on the UI thread.</summary>
    void RunInBackground<T>(StatusMessage working, Func<CancellationToken, Task<T>> action, Func<T, StatusMessage> onUiThread);
}