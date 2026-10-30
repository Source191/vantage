namespace Source191.Vantage.Domain;

/// <summary>
/// Where a row runs, as one value: the repository, then › and the folder's part after the repository (the slug of a
/// worktree named <c>repository-slug</c>, the path inside the repository, or the whole folder label), then the branch
/// in brackets when it differs from that part. Outside a repository the folder's label stands alone. <see cref="Fit"/>
/// elides the repository's middle first and the place's end second; the branch is never cut.
/// </summary>
public sealed record WhereLabel(string? Repository, string Place, string? Branch)
{
    private const string Ellipsis = "…";
    private const string Separator = " › ";

    public static WhereLabel For(string repositoryName, string folderLabel, string? branch, bool inRepository)
    {
        ArgumentNullException.ThrowIfNull(repositoryName);
        ArgumentNullException.ThrowIfNull(folderLabel);
        if (!inRepository)
        {
            return new WhereLabel(null, folderLabel, null);
        }

        string place = PlaceOf(repositoryName, folderLabel);
        string? shownBranch = branch is null || string.Equals(branch, place, StringComparison.Ordinal) ? null : branch;

        return new WhereLabel(repositoryName, place, shownBranch);
    }

    public string Text => Fit(int.MaxValue);

    /// <summary>The label within <paramref name="width"/> cells: whole when it fits, else the repository elided in its middle, else the place cut at its end.</summary>
    public string Fit(int width)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(width);
        string branch = Branch is null ? string.Empty : $" ({Branch})";
        if (Repository is null)
        {
            return CutEnd(Place, width);
        }

        string tail = (Place.Length == 0 ? string.Empty : Separator + Place) + branch;
        if (Repository.Length + tail.Length <= width)
        {
            return Repository + tail;
        }

        int room = width - tail.Length;
        if (room >= 3)
        {
            return ElideMiddle(Repository, room) + tail;
        }

        int placeRoom = width - Ellipsis.Length - Separator.Length - branch.Length;

        return Ellipsis + Separator + CutEnd(Place, Math.Max(placeRoom, 1)) + branch;
    }

    /// <summary>The label's part after the repository: nothing for the main checkout, the slug or inner path when the label starts with the repository, else the whole label.</summary>
    private static string PlaceOf(string repository, string label)
    {
        if (string.Equals(label, repository, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        string[] prefixes = [repository + "-", repository + "\\"];
        string? prefix = prefixes.FirstOrDefault(candidate => label.StartsWith(candidate, StringComparison.OrdinalIgnoreCase));

        return prefix is null ? label : label[prefix.Length..];
    }

    private static string ElideMiddle(string text, int width)
    {
        if (text.Length <= width)
        {
            return text;
        }

        int kept = width - Ellipsis.Length;
        int head = (kept + 1) / 2;

        return text[..head] + Ellipsis + text[^(kept - head)..];
    }

    private static string CutEnd(string text, int width) => text.Length <= width ? text : text[..Math.Max(width - Ellipsis.Length, 0)] + Ellipsis;
}