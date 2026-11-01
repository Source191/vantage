namespace Source191.Vantage.Application.Diagnostics;

/// <summary>What a <see cref="DurationSample"/> says: every run counted, the median and 95th percentile of the latest, and the longest of all.</summary>
public sealed record DurationFigures(int Runs, TimeSpan Median, TimeSpan NinetyFifth, TimeSpan Longest)
{
    public static readonly DurationFigures None = new(0, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero);
}