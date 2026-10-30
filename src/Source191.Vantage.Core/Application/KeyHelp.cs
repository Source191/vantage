namespace Source191.Vantage.Application;

/// <summary>
/// One key of a view: the word the key bar shows beside it (<paramref name="Hint"/>, null for a key only the help lists),
/// the sentence the help shows (<paramref name="Line"/>), and the sentence its tooltip shows on the bar (<paramref name="Tip"/>).
/// </summary>
public sealed record KeyHelp(string Key, string? Hint, string Line, string Tip)
{
    /// <summary>The key as the help and the key bar write it: a named key is capitalised ("Enter"), a character is left as typed.</summary>
    public string HelpKey => Key.Length > 1 ? char.ToUpperInvariant(Key[0]) + Key[1..] : Key;
}

/// <summary>What one table column shows; a meaning of several lines is drawn with its continuation lines indented.</summary>
public sealed record ColumnHelp(string Name, string Meaning);
