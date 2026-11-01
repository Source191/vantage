namespace Source191.Vantage.Adapters.Processes;

/// <summary>
/// The folders of this process's PATH, in order, for the locators that search it. Only full paths are kept: a relative
/// entry such as <c>.</c> would be searched against the folder Vantage was started from, which a cloned repository can
/// plant a program in.
/// </summary>
public static class PathFolders
{
    public static IReadOnlyList<string> ForCurrentUser() => From(Environment.GetEnvironmentVariable("PATH"));

    /// <summary>The full-path folders of a PATH value, in order; an unset one has none.</summary>
    public static IReadOnlyList<string> From(string? path) => Searchable((path ?? string.Empty).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    /// <summary>The one filter every PATH search applies, whoever built the list.</summary>
    public static IReadOnlyList<string> Searchable(IEnumerable<string> folders) => [.. folders.Where(Path.IsPathFullyQualified)];
}