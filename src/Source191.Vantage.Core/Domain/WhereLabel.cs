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

    /// <summary>A character, the ellipsis and a character.</summary>
    private const int LeastElided = 3;

    /// <summary>The ellipsis, the separator and a character of the place.</summary>
    private const int LeastPlaced = 5;

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

            if (Place.Length == 0 || width < LeastPlaced)
            {
                return ElideMiddle(Repository, width);
            }

            int room = width - tail.Length;
            if (room >= LeastElided)
            {
                return ElideMiddle(Repository, room) + tail;
            }

            return Ellipsis + Separator + CutEnd(Place, width - Ellipsis.Length - Separator.Length);
        }

        private static string ElideMiddle(string text, int width)
        {
            if (text.Length <= width)
            {
                return text;
            }

            if (width < LeastElided)
            {
                return CutEnd(text, width);
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


    private static string CutEnd(string text, int width) => (text.Length <= width, width) switch
    {
        (true, _) => text,
        (_, 0) => string.Empty,
        _ => text[..(width - Ellipsis.Length)] + Ellipsis,
    };
}