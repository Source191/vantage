namespace Source191.Vantage.Application.Views;

public sealed record RowsLoaded(IReadOnlyList<SessionRow> Rows) : ViewInput
{
    /// <summary>The automated runs the same listing held, which are not rows.</summary>
    public AutomatedRuns AutomatedRuns { get; init; } = AutomatedRuns.None;
}