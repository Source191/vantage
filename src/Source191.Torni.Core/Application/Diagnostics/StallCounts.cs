namespace Source191.Torni.Application.Diagnostics;

/// <summary>How late the UI thread ran its heartbeat, counted for every beat more than <see cref="Heartbeat.Stall"/> late.</summary>
public sealed class StallCounts
{
    private readonly DurationSample sample = new();

    public void Add(TimeSpan lateness) => sample.Add(lateness);

    public DurationFigures Figures() => sample.Figures();
}