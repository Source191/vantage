using Source191.Vantage.Application.Views;
using Terminal.Gui.Views;
namespace Source191.Vantage.Tui;

/// <summary>Presents the worktree rows to Terminal.Gui's TableView.</summary>
internal sealed class WorktreeTableSource(IReadOnlyList<WorktreeRow> rows, DateTimeOffset now) : ITableSource
{
    private static readonly string[] Names = ["REPOSITORY", "FOLDER", "BRANCH", "DIRTY", "UNPUSHED", "LAST COMMIT", "SESSION", "HEALTH"];

    public int Rows => rows.Count;

    public int Columns => Names.Length;

    public string[] ColumnNames => Names;

    public object this[int row, int col]
    {
        get
        {
            WorktreeRow item = rows[row];

            return col switch
            {
                0 => item.RepositoryName,
                1 => item.FolderName,
                2 => item.Branch ?? "-",
                3 => Count(item.DirtyFiles),
                4 => Count(item.UnpushedCommits),
                5 => item.LastCommitAt is null ? "-" : SessionTableSource.Age(now - item.LastCommitAt.Value),
                6 => item.SessionName ?? string.Empty,
                // The kind only: the reason goes in the footer for the selected row, so a long one is never cut off.
                _ => item.Health.Kind.ToString().ToLowerInvariant(),
            };
        }
    }

    public WorktreeRow RowAt(int index)
    {
        return rows[index];
    }

    private static string Count(int number)
    {
        return number == 0 ? string.Empty : number.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}