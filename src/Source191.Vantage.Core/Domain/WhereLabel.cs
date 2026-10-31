namespace Source191.Vantage.Domain;

/// <summary>
/// Where a row runs, as one value. <see cref="Inside"/> a repository it is the repository, then › and the folder's part
/// after the repository (the slug of a worktree named <c>repository-slug</c>, the path inside the repository, or the
/// whole folder label), eliding the repository's middle first and the place's end second. <see cref="Outside"/> one the
/// folder's label stands alone, cut at its end. The branch is the detail pane's, never the label's.
/// </summary>
public abstract record WhereLabel
{
    private const string Ellipsis = "…";
    private const string Separator = " › ";

    public static WhereLabel For(string repositoryName, string folderLabel, bool inRepository)
    {
        ArgumentNullException.ThrowIfNull(repositoryName);
        ArgumentNullException.ThrowIfNull(folderLabel);

        return inRepository ? new Inside(repositoryName, PlaceOf(repositoryName, folderLabel)) : new Outside(folderLabel);
    }

    public string Text => Fit(int.MaxValue);

    /// <summary>The label within <paramref name="width"/> cells, whole when it fits.</summary>
    public abstract string Fit(int width);

    /// <summary>A folder outside any repository, shown by its label.</summary>
    public sealed record Outside(string Place) : WhereLabel
    {
        public override string Fit(int width)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(width);

            return CutEnd(Place, width);
        }
    }

    /// <summary>A folder in a repository; an empty place is the main checkout, or a cell that names the repository alone.</summary>
    public sealed record Inside(string Repository, string Place) : WhereLabel
    {
        /// <summary>Whole when it fits, else the repository elided in its middle, else the place cut at its end.</summary>
        public override string Fit(int width)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(width);
            string tail = Place.Length == 0 ? string.Empty : Separator + Place;
            if (Repository.Length + tail.Length <= width)
            {
                return Repository + tail;
            }

            int room = width - tail.Length;
            if (room >= 3)
            {
                return ElideMiddle(Repository, room) + tail;
            }

            int placeRoom = width - Ellipsis.Length - Separator.Length;

            return Ellipsis + Separator + CutEnd(Place, Math.Max(placeRoom, 1));
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
    }

    /// <summary>The label's part after the repository: nothing for the main checkout, the slug or inner path when the label starts with the repository, else the whole label.</summary>
    private static string PlaceOf(string repository, string label)
    {
        if (string.Equals(label, repository, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        string[] prefixes = [repository + "-", repository + "\\"];

        return prefixes
            .Where(prefix => label.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(prefix => label[prefix.Length..])
            .DefaultIfEmpty(label)
            .First();
    }


    private static string CutEnd(string text, int width) => text.Length <= width ? text : text[..Math.Max(width - Ellipsis.Length, 0)] + Ellipsis;
}