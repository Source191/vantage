namespace Source191.Vantage.Application;

/// <summary>What the pane shows for one row: its title, its facts in rows of up to three columns, and the lines beneath them (a worktree's commits or changed files).</summary>
public sealed record DetailContent(string Title, IReadOnlyList<IReadOnlyList<DetailLine>> Facts, IReadOnlyList<DetailLine> Below)
{
    /// <summary>An empty pane: no title and no lines, shown before a row is selected and when the selection clears.</summary>
    public static readonly DetailContent Empty = new(string.Empty, [], []);
}