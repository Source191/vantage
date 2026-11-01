namespace Source191.Vantage.Adapters.Processes;

/// <summary>The folders of this process's PATH, in order, for the locators that search it.</summary>
public static class PathFolders
{
    public static IReadOnlyList<string> ForCurrentUser() => From(Environment.GetEnvironmentVariable("PATH"));

    /// <summary>The folders of a PATH value, in order; an unset one has none.</summary>
    public static IReadOnlyList<string> From(string? path) => (path ?? string.Empty).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}