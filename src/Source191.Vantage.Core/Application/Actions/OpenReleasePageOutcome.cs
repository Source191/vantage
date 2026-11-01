using Source191.Vantage.Application.Effects;

namespace Source191.Vantage.Application.Actions;

/// <summary>What the Release page button did.</summary>
public abstract record OpenReleasePageOutcome : Outcome
{
    /// <summary>The release's page was handed to the default browser.</summary>
    public sealed record Opened(string Version) : OpenReleasePageOutcome;

    /// <summary>Windows could not open the page; the reason is the desktop's own sentence.</summary>
    public sealed record NotOpened(string Version, string Reason) : OpenReleasePageOutcome;
}