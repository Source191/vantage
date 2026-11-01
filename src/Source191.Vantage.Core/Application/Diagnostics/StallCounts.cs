namespace Source191.Vantage.Application.Diagnostics;

/// <summary>How late the UI thread ran its heartbeat, counted for every beat more than 100 ms late.</summary>
public sealed class StallCounts
{
    private readonly DurationSample sample = new();

    public void Add(TimeSpan lateness) => sample.Add(lateness);

    public DurationFigures Figures() => sample.Figures();
}
