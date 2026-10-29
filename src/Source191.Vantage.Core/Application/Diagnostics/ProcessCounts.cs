namespace Source191.Vantage.Application.Diagnostics;

/// <summary>
/// How often each child program ran, by subcommand and exit code, with the longest run. Successful runs are counted
/// here rather than logged, because the sessions view's refresh alone would fill the log within minutes.
/// </summary>
public sealed class ProcessCounts
{
    private readonly Lock gate = new();
    private readonly Dictionary<(ChildCommand Command, int ExitCode), ProcessTally> tallies = [];

    public void Add(ChildCommand command, int exitCode, TimeSpan duration)
    {
        lock (gate)
        {
            ProcessTally tally = tallies.GetValueOrDefault((command, exitCode), ProcessTally.None);
            tallies[(command, exitCode)] = tally.With(duration);
        }
    }

    public IReadOnlyList<ProcessCount> Snapshot()
    {
        lock (gate)
        {
            return [.. tallies
                .Select(pair => new ProcessCount(pair.Key.Command, pair.Key.ExitCode, pair.Value.Runs, pair.Value.Longest))
                .OrderBy(count => count.Command.ToString(), StringComparer.Ordinal)
                .ThenBy(count => count.ExitCode)];
        }
    }

    private sealed record ProcessTally(int Runs, TimeSpan Longest)
    {
        public static readonly ProcessTally None = new(0, TimeSpan.Zero);

        public ProcessTally With(TimeSpan duration) => new(Runs + 1, duration > Longest ? duration : Longest);
    }
}

public sealed record ProcessCount(ChildCommand Command, int ExitCode, int Runs, TimeSpan Longest);
