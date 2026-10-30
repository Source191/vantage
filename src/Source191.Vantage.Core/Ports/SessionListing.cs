using Source191.Vantage.Domain;

namespace Source191.Vantage.Ports;

/// <summary>What the CLI's listing holds: the sessions, and apart from them the automated runs.</summary>
public sealed record SessionListing(IReadOnlyList<Session> Sessions, IReadOnlyList<AutomatedRun> AutomatedRuns)
{
    public static SessionListing Empty { get; } = new([], []);
}
