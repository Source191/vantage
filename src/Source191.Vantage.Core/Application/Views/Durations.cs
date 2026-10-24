namespace Source191.Vantage.Application.Views;

/// <summary>How the tables show an elapsed time: minutes under an hour, hours and minutes under a day, then days and hours.</summary>
public static class Durations
{
    public static string Age(TimeSpan elapsed)
    {
        if (elapsed < TimeSpan.Zero)
        {
            elapsed = TimeSpan.Zero;
        }

        return elapsed.TotalHours switch
        {
            < 1 => $"{(int)elapsed.TotalMinutes}m",
            < 24 => $"{(int)elapsed.TotalHours}h{elapsed.Minutes:00}m",
            _ => $"{(int)elapsed.TotalDays}d{elapsed.Hours}h",
        };
    }
}