namespace Source191.Vantage.Application.Views;

/// <summary>
/// Sets a status line message out in rows of a given width under the settings row: broken at spaces, a word wider than
/// a row broken at its end, and at most <see cref="MostRows"/> rows, the last ending with an ellipsis when more was left.
/// </summary>
public static class StatusLayout
{
    public const int MostRows = 3;

    private const string Ellipsis = "…";

    /// <summary>How long a success stays before it clears by itself.</summary>
    public static readonly TimeSpan SuccessShownFor = TimeSpan.FromSeconds(10);

    /// <summary>Whether a message of this tone clears by itself, after <see cref="SuccessShownFor"/>: a success does, everything else stays until it is cleared.</summary>
    public static bool ClearsBySelf(StatusTone tone) => tone switch
    {
        StatusTone.Success => true,
        StatusTone.Notice => false,
        StatusTone.Failure => false,
        StatusTone.Working => false,
        StatusTone.Hint => false,
    };

    /// <summary>Whether the message has a close mark: not while an action runs, and not for no message.</summary>
    public static bool Closes(StatusMessage message, bool running) => !running && message is not StatusMessage.Blank;

    public static IReadOnlyList<string> Lines(string words, int width)
    {
        ArgumentNullException.ThrowIfNull(words);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);

        List<string> rows = [];
        string current = string.Empty;
        foreach (string word in words.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            current = Place(rows, current, word, width);
        }

        if (current.Length > 0)
        {
            rows.Add(current);
        }

        return rows.Count <= MostRows ? rows : [.. rows.Take(MostRows - 1), Shortened(rows[MostRows - 1], width)];
    }

    /// <summary>Adds the word to the row being built, or closes that row and starts the next with it; answers the row being built.</summary>
    private static string Place(List<string> rows, string current, string word, int width)
    {
        string joined = current.Length == 0 ? word : $"{current} {word}";
        if (joined.Length <= width)
        {
            return joined;
        }

        if (current.Length > 0)
        {
            rows.Add(current);
        }

        string rest = word;
        while (rest.Length > width)
        {
            rows.Add(rest[..width]);
            rest = rest[width..];
        }

        return rest;
    }

    /// <summary>The last row shown when more was left: an ellipsis after it, or in place of its last cell when it is full.</summary>
    private static string Shortened(string row, int width) => row.Length < width ? row + Ellipsis : row[..(width - 1)] + Ellipsis;
}