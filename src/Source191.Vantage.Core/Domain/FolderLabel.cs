namespace Source191.Vantage.Domain;

/// <summary>
/// How a folder is written in a table cell: relative to the deepest root that strictly contains it, else relative to
/// the home folder as <c>~\...</c>, else in full. A root itself is not "under" itself, so it falls through to home.
/// </summary>
public static class FolderLabel
{
    public static string For(FolderPath folder, IReadOnlyList<FolderPath> roots, FolderPath home)
    {
        FolderPath? root = roots
            .Where(candidate => Holds(candidate, folder))
            .OrderByDescending(candidate => candidate.Value.Length)
            .FirstOrDefault();

        return (root, folder) switch
        {
            (not null, _) => Path.GetRelativePath(root.Value, folder.Value),
            _ when folder == home => "~",
            _ when Holds(home, folder) => $@"~\{Path.GetRelativePath(home.Value, folder.Value)}",
            _ => folder.Value,
        };
    }

    private static bool Holds(FolderPath root, FolderPath folder) => folder.IsUnder(root) && folder != root;
}
