using Source191.Vantage.Application;
using Source191.Vantage.Application.Views;
using Terminal.Gui.ViewBase;
using GuiKey = Terminal.Gui.Input.Key;

namespace Source191.Vantage.Tui;

/// <summary>What the console asks of a tab, whatever its rows: its view, its words for the frame, its keys and its refresh.</summary>
internal interface ITabBody
{
    View View { get; }

    TimeSpan RefreshEvery { get; }

    string Title { get; }

    IReadOnlyList<TextPart> Counts { get; }

    /// <summary>The table's title for the frame's line, or the failure banner in red when the last load failed.</summary>
    TextPart FrameTitle { get; }

    KeyBar Bar(bool updateKnown);

    /// <summary>What the console's status line says while this tab is shown: its last message, or nothing.</summary>
    StatusMessage Status { get; }

    /// <summary>Whether an action this tab started is still running, which the status line shows with its spinner.</summary>
    bool Running { get; }

    void OnKey(GuiKey key);

    void Press(Key key);

    /// <summary>An input for the tab's reducer that is not a key, such as the status line's close mark or a success's time running out.</summary>
    void Reduce(ViewInput input);

    void TriggerLoad();

    void Redraw();

    /// <summary>
    /// Tells the tab whether it is the one shown. A hidden tab keeps its spinners still: a spinner redraws its own tab alone,
    /// title and frame included, and a hidden tab drawn alone lands on top of the shown one and closes its edge.
    /// </summary>
    void Shown(bool shown);
}