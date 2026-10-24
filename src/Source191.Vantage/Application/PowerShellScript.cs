namespace Source191.Vantage.Application;

/// <summary>
/// Builds the PowerShell that runs inside a tab Vantage opens. Every value is single-quoted, so user text such as a
/// session name is never interpolated into script source; the adapter encodes the finished script for wt.exe.
/// </summary>
public static class PowerShellScript
{
    public static string Quote(string text)
    {
        return $"'{text.Replace("'", "''", StringComparison.Ordinal)}'";
    }

    public static string Invoke(string executable, IReadOnlyList<string> arguments)
    {
        IEnumerable<string> quoted = new[] { executable }.Concat(arguments).Select(Quote);

        return $"& {string.Join(' ', quoted)}";
    }
}