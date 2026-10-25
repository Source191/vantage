namespace Source191.Vantage.Application;

/// <summary>
/// One key of a view: what the header shows (<paramref name="Hint"/>, null for a key only the help lists), the sentence
/// the help and the hint's tooltip share (<paramref name="Line"/>) and the header column its hint sits in.
/// </summary>
public sealed record KeyHelp(string Key, string? Hint, string Line, int Column)
{
    /// <summary>The key as the header draws it, such as "&lt;enter&gt;" or "&lt;R&gt;".</summary>
    public string HeaderKey => $"<{Key}>";

    /// <summary>The key as the help writes it: a named key is capitalised ("Enter"), a character is left as typed.</summary>
    public string HelpKey => Key.Length > 1 ? char.ToUpperInvariant(Key[0]) + Key[1..] : Key;
}

/// <summary>What one table column shows; a meaning of several lines is drawn with its continuation lines indented.</summary>
public sealed record ColumnHelp(string Name, string Meaning);