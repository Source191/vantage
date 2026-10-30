namespace Source191.Vantage.Domain;

/// <summary>
/// A background session's screen rebuilt from <c>claude logs</c>, which answers the whole screen history as one stream:
/// every row padded with spaces to the session's own width, with no line breaks, and the width not stated.
/// </summary>
public static class SessionScreen
{
    /// <summary>The last <paramref name="rows"/> rows of the session's output, without Claude's own input box and status line; none when the width cannot be found.</summary>
    public static IReadOnlyList<string> Bottom(string logs, int rows) => [];
}
