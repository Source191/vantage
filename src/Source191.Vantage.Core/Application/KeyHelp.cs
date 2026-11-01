namespace Source191.Vantage.Application;

/// <summary>
/// One key of a view: the word the key bar shows beside it (<paramref name="Hint"/>, empty for a key only the help lists,
/// built by <see cref="HelpOnly"/>), the sentence the help shows (<paramref name="Line"/>), and the sentence its tooltip
/// shows on the bar (<paramref name="Tip"/>).
/// </summary>
public sealed record KeyHelp(string Key, string Hint, string Line, string Tip)
{
    /// <summary>Whether the key has a word for the key bar; a key only the help lists has none.</summary>
    public bool OnBar => Hint.Length > 0;

    /// <summary>A key only the help lists, such as a tab key: it has no word for the key bar.</summary>
    public static KeyHelp HelpOnly(string key, string line, string tip) => new(key, string.Empty, line, tip);

    /// <summary>The key as the help and the key bar write it: a named key is capitalised ("Enter", "Shift+Left"), a character is left as typed.</summary>
    public string HelpKey => Key.Length > 1 ? string.Join('+', Key.Split('+').Select(part => char.ToUpperInvariant(part[0]) + part[1..])) : Key;
}