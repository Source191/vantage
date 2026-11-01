namespace Source191.Vantage.Application.Diagnostics;

/// <summary>
/// How often each child program ran, by subcommand and exit code, with the typical and longest runs. Successful runs
/// are counted here rather than logged, because the sessions view's refresh alone would fill the log within minutes.
/// </summary>
public sealed class ProcessCounts
{
    private readonly Lock gate = new();
    private readonly Dictionary<(ChildCommand Command, int ExitCode), DurationSample> samples = [];

    public void Add(ChildCommand command, int exitCode, TimeSpan duration)
    {
        lock (gate)
        {
            if (!samples.TryGetValue((command, exitCode), out DurationSample? sample))
            {
                sample = new DurationSample();
                samples[(command, exitCode)] = sample;
            }

            sample.Add(duration);
        }
    }

    public IReadOnlyList<ProcessCount> Snapshot()
    {
        lock (gate)
        {
            return [.. samples
                .Select(pair => new ProcessCount(pair.Key.Command, pair.Key.ExitCode, pair.Value.Figures()))
                .OrderBy(count => count.Command.ToString(), StringComparer.Ordinal)
                .ThenBy(count => count.ExitCode)];
        }
    }
}