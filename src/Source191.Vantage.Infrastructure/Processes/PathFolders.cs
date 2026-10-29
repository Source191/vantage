namespace Source191.Vantage.Adapters.Processes;

/// <summary>The folders of this process's PATH, in order, for the locators that search it.</summary>
public static class PathFolders
{
    public static IReadOnlyList<string> ForCurrentUser() =>
        (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}