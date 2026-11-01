namespace Source191.Vantage.HeadlessTerminal;

/// <summary>How a drive runs: whether to pretend to be Windows Terminal and answer its queries, the XTVERSION delay, and the pseudo-console's size.</summary>
public sealed record DriverOptions(bool PretendWindowsTerminal, bool AnswerQueries, int XtversionDelayMs, int Columns = 120, int Rows = 40)
{
    public static DriverOptions FromEnvironment()
    {
        return new DriverOptions(
            PretendWindowsTerminal: Environment.GetEnvironmentVariable("HARNESS_PLAIN") is null,
            AnswerQueries: Environment.GetEnvironmentVariable("HARNESS_RESPOND") is not null,
            XtversionDelayMs: int.TryParse(Environment.GetEnvironmentVariable("HARNESS_DCS_DELAY_MS"), out int delay) ? delay : 0,
            Columns: int.TryParse(Environment.GetEnvironmentVariable("HARNESS_COLUMNS"), out int columns) ? columns : 120,
            Rows: int.TryParse(Environment.GetEnvironmentVariable("HARNESS_ROWS"), out int rows) ? rows : 40);
    }

    public IReadOnlyDictionary<string, string> ChildEnvironment()
    {
        Dictionary<string, string> environment = new(StringComparer.OrdinalIgnoreCase);
        foreach (System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            environment[(string)entry.Key] = (string?)entry.Value ?? string.Empty;
        }

        environment["VANTAGE_NO_UPDATE_CHECK"] = "1";

        if (PretendWindowsTerminal)
        {
            environment["TERM_PROGRAM"] = "WindowsTerminal";
            environment["WT_SESSION"] = "harness";
        }
        else
        {
            environment.Remove("TERM_PROGRAM");
            environment.Remove("WT_SESSION");
        }

        return environment;
    }
}