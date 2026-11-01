namespace Source191.Vantage.Ports;

/// <summary>
/// A captured child program did not finish within <see cref="Limit"/> and was ended with its process tree.
/// <see cref="FileName"/> is the program's bare file name, never its folder.
/// </summary>
public sealed class ProcessTimedOutException(string fileName, TimeSpan limit)
    : Exception($"{fileName} did not finish within {Seconds(limit)} and was ended")
{
    public string FileName { get; } = fileName;

    public TimeSpan Limit { get; } = limit;

    private static string Seconds(TimeSpan limit) => limit == TimeSpan.FromSeconds(1) ? "1 second" : $"{limit.TotalSeconds:0.##} seconds";
}