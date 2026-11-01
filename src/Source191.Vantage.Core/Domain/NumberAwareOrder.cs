namespace Source191.Vantage.Domain;

/// <summary>
/// Orders text with its runs of ASCII digits compared as numbers and every other character compared without case,
/// so <c>2025.10</c> follows <c>2025.9</c>. It reads no culture data: the shipped tool runs in invariant globalisation
/// mode, where <c>CompareOptions.NumericOrdering</c> is ignored. Text that ties (<c>7</c> against <c>007</c>) puts the
/// shorter first, then falls back to an ordinal comparison, so only the same text answers zero.
/// </summary>
public static class NumberAwareOrder
{
    public static int Compare(string first, string second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        int byRuns = Compare(first.AsSpan(), second.AsSpan());
        int byLength = byRuns == 0 ? first.Length.CompareTo(second.Length) : byRuns;

        return byLength == 0 ? string.CompareOrdinal(first, second) : byLength;
    }

    private static int Compare(ReadOnlySpan<char> first, ReadOnlySpan<char> second)
    {
        if (first.IsEmpty || second.IsEmpty)
        {
            return first.Length.CompareTo(second.Length);
        }

        int firstRun = RunLength(first);
        int secondRun = RunLength(second);
        int compared = CompareRuns(first[..firstRun], second[..secondRun]);

        return compared == 0 ? Compare(first[firstRun..], second[secondRun..]) : compared;
    }

    /// <summary>A run is all the leading digits, or one character that is not a digit.</summary>
    private static int RunLength(ReadOnlySpan<char> text)
    {
        int digits = text.IndexOfAnyExceptInRange('0', '9');
        int leadingDigits = digits < 0 ? text.Length : digits;

        return leadingDigits == 0 ? 1 : leadingDigits;
    }

    private static int CompareRuns(ReadOnlySpan<char> first, ReadOnlySpan<char> second) =>
        char.IsAsciiDigit(first[0]) && char.IsAsciiDigit(second[0])
            ? CompareNumbers(first.TrimStart('0'), second.TrimStart('0'))
            : char.ToUpperInvariant(first[0]).CompareTo(char.ToUpperInvariant(second[0]));

    private static int CompareNumbers(ReadOnlySpan<char> first, ReadOnlySpan<char> second)
    {
        int byLength = first.Length.CompareTo(second.Length);

        return byLength == 0 ? first.SequenceCompareTo(second) : byLength;
    }
}
