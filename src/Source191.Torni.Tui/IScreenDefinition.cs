using Source191.Torni.Application;
using Source191.Torni.Application.Views;
using Terminal.Gui.Drawing;

namespace Source191.Torni.Tui;

/// <summary>
/// What one table view adds to the shared screen: which tab it is, how it refreshes, its layout text, its columns and colours,
/// its reducer and, when it needs them, an input observer, a row tooltip and a detail pane (each defaults to none).
/// The rest of a tab (lifetime, keys, routing effects, background work, spinners, redraw) is
/// <see cref="TableBody{TState, TRow}"/>'s and written once, with the column sizing, tooltips and detail pane
/// in <see cref="ColumnSizing{TRow}"/>, <see cref="TableToolTips{TRow}"/> and <see cref="DetailReader{TRow}"/>.
/// </summary>
internal interface IScreenDefinition<TState, TRow>
    where TState : IListViewState<TRow>
{
    /// <summary>Which tab this is; its title is the tab's, set once.</summary>
    ConsoleTab Tab { get; }

    /// <summary>How the tab is kept fresh on a timer.</summary>
    TabRefresh Refresh { get; }

    /// <summary>Shown with a spinner over the empty table until the first load arrives.</summary>
    string WaitingText { get; }

    /// <summary>The words before the banner's failure message, such as "listing failed".</summary>
    string LoadFailurePrefix { get; }

    string HelpText { get; }

    /// <summary>The table's columns, in cell order: each its help (the name that heads it, the tooltip and the help), its cell, its fit and its paint. Exactly one is elastic.</summary>
    IReadOnlyList<TableColumn<TRow>> Columns { get; }

    Color RowColour(TRow row);

    /// <summary>The counts beside the tab's title.</summary>
    IReadOnlyList<TextPart> Counts(TState state);

    /// <summary>The key bar for the selected row and the listed rows; Update shows when a release is known.</summary>
    KeyBar Bar(TState state, bool updateKnown);

    TState WithMessage(TState state, StatusMessage message);

    ViewUpdate<TState> Reduce(TState state, ViewInput input);

    /// <summary>Sees every input before the state is replaced, for the notifier.</summary>
    void OnInput(TState before, ViewInput input)
    {
    }

    /// <summary>What a row is, shown as a tooltip while the pointer rests on it, or empty text for a view whose rows need none.</summary>
    string RowTip(TRow row) => string.Empty;

    /// <summary>Whether the tab has a pane under its table and how it reads the selected row; asked once, when the tab is built.</summary>
    TableDetail<TRow> Detail => new TableDetail<TRow>.None();
}