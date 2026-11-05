using Source191.Torni.Application.Detail;

namespace Source191.Torni.Tui;

/// <summary>Whether a tab has a detail pane under its table: none (the Config tab), or a pane that reads the selected row from its source.</summary>
internal abstract record TableDetail<TRow>
{
    /// <summary>No pane: the table fills the tab.</summary>
    public sealed record None : TableDetail<TRow>;

    public sealed record Pane(IDetailSource<TRow> Source) : TableDetail<TRow>;
}