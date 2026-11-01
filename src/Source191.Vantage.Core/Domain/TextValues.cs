namespace Source191.Vantage.Domain;

public static class TextValues
{
    /// <summary>The texts that parse, in order; the rest are skipped, as a listing skips a row it cannot read.</summary>
    public static IEnumerable<T> Parsed<T>(this IEnumerable<string?> texts)
        where T : class, ITextValue<T>
    {
        foreach (string? text in texts)
        {
            if (T.TryParse(text, out T? value))
            {
                yield return value;
            }
        }
    }
}